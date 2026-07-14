using System.Text.Json;
using EventService.Application.Common;
using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;
using AppEventService = EventService.Application.Services.EventService;

namespace EventService.Application.Tests;

/// <summary>
/// Unit-тесты для <see cref="AppEventService"/>, проверяющие стратегию Cache-Aside:
/// попадание/промах кеша при чтении и обновление/инвалидацию кеша при мутирующих операциях.
/// Репозиторий и кеш подменяются заглушками (Moq).
/// </summary>
public class EventServiceTests
{
    private readonly Mock<IEventRepository> _repositoryMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly AppEventService _sut;

    public EventServiceTests()
    {
        _sut = new AppEventService(_repositoryMock.Object, _cacheMock.Object);
    }

    private static Event CreateEvent(int totalSeats = 100) => Event.Create(
        "Test event",
        DateTime.UtcNow.AddDays(1),
        DateTime.UtcNow.AddDays(2),
        totalSeats,
        "Description");

    private static EventInfo ToInfo(Event @event) => new()
    {
        Id = @event.Id,
        Title = @event.Title,
        StartAt = @event.StartAt!.Value,
        EndAt = @event.EndAt!.Value,
        TotalSeats = @event.TotalSeats,
        AvailableSeats = @event.AvailableSeats,
        Description = @event.Description
    };

    // ---------- Сценарий 1: при попадании в кеш репозиторий не вызывается ----------

    [Fact]
    public async Task GetEventByIdAsync_CacheHit_DoesNotCallRepository()
    {
        var @event = CreateEvent();
        var cachedInfo = ToInfo(@event);
        _cacheMock
            .Setup(c => c.GetAsync(CacheKeys.Event(@event.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedInfo));

        var result = await _sut.GetEventByIdAsync(@event.Id);

        result.Should().BeEquivalentTo(cachedInfo);
        _repositoryMock.Verify(
            r => r.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _cacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTopEventsAsync_CacheHit_DoesNotCallRepository()
    {
        var @event = CreateEvent();
        var cachedInfos = new List<EventInfo> { ToInfo(@event) };
        _cacheMock
            .Setup(c => c.GetAsync(CacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedInfos));

        var result = await _sut.GetTopEventsAsync();

        result.Should().BeEquivalentTo(cachedInfos);
        _repositoryMock.Verify(
            r => r.GetTopByPopularityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------- Сценарий 2: при промахе данные берутся из репозитория и сохраняются в кеш ----------

    [Fact]
    public async Task GetEventByIdAsync_CacheMiss_FetchesFromRepositoryAndStoresInCache()
    {
        var @event = CreateEvent();
        _cacheMock
            .Setup(c => c.GetAsync(CacheKeys.Event(@event.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _repositoryMock
            .Setup(r => r.FindByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var result = await _sut.GetEventByIdAsync(@event.Id);

        result.Should().BeEquivalentTo(ToInfo(@event));
        _repositoryMock.Verify(r => r.FindByIdAsync(@event.Id, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(
            c => c.SetAsync(
                CacheKeys.Event(@event.Id),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTopEventsAsync_CacheMiss_FetchesFromRepositoryAndStoresInCache()
    {
        var events = new List<Event> { CreateEvent(), CreateEvent() };
        _cacheMock
            .Setup(c => c.GetAsync(CacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _repositoryMock
            .Setup(r => r.GetTopByPopularityAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var result = await _sut.GetTopEventsAsync();

        result.Should().BeEquivalentTo(events.Select(ToInfo));
        _repositoryMock.Verify(r => r.GetTopByPopularityAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(
            c => c.SetAsync(CacheKeys.TopEvents, It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---------- Сценарий 3: мутирующие операции обновляют/инвалидируют кеш согласно стратегии ----------

    [Fact]
    public async Task CreateEventAsync_InvalidatesTopEventsCache()
    {
        var item = new CreateEvent
        {
            Title = "New event",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 50
        };

        await _sut.CreateEventAsync(item);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.TopEvents, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEventAsync_InvalidatesEventCache()
    {
        var @event = CreateEvent();
        _repositoryMock
            .Setup(r => r.FindByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);
        var update = new UpdateEvent
        {
            Title = "Updated title",
            StartAt = DateTime.UtcNow.AddDays(3),
            EndAt = DateTime.UtcNow.AddDays(4)
        };

        await _sut.UpdateEventAsync(@event.Id, update);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.Event(@event.Id), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.TopEvents, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEventAsync_InvalidatesEventAndTopEventsCache()
    {
        var @event = CreateEvent();
        _repositoryMock
            .Setup(r => r.FindByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        await _sut.DeleteEventAsync(@event.Id);

        _repositoryMock.Verify(r => r.Remove(@event), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.Event(@event.Id), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.TopEvents, It.IsAny<CancellationToken>()), Times.Once);
    }
}
