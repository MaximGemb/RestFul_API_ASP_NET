using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestFulApi.DataAccess;
using RestFulApi.DTOs;
using RestFulApi.Exceptions;
using RestFulApi.Interfaces;
using RestFulApi.Services;
using Xunit;

namespace RestFulApi.Tests.Services;

public class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public EventServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public async Task CreateEventAsync_ShouldCreateEvent_WhenDataIsValid()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var dto = CreateEventDto("Tech Conference", 2, 3);

        var createdEvent = await service.CreateEventAsync(dto, TestContext.Current.CancellationToken);

        createdEvent.Id.Should().NotBe(Guid.Empty);
        createdEvent.Title.Should().Be(dto.Title);
        createdEvent.TotalSeats.Should().Be(dto.TotalSeats);
        createdEvent.AvailableSeats.Should().Be(dto.TotalSeats);
    }

    [Fact]
    public async Task GetAllEventsAsync_ShouldApplyTitleFilterAndPagination()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        await service.CreateEventAsync(CreateEventDto("Backend meetup", 4, 5), TestContext.Current.CancellationToken);
        await service.CreateEventAsync(CreateEventDto("Frontend meetup", 6, 7), TestContext.Current.CancellationToken);
        await service.CreateEventAsync(CreateEventDto("Backend deep dive", 8, 9), TestContext.Current.CancellationToken);

        var result = await service.GetAllEventsAsync(
            title: "backend",
            page: 1,
            pageSize: 1,
            ct: TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(2);
        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Backend deep dive");
    }

    [Fact]
    public async Task GetAllEventsAsync_ShouldApplyDateRangeFilter()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var now = DateTime.UtcNow.AddDays(30);
        await service.CreateEventAsync(new CreateEvent
        {
            Title = "Out of range old",
            StartAt = now.AddDays(-5),
            EndAt = now.AddDays(-4),
            TotalSeats = 20,
            Description = "Old"
        }, TestContext.Current.CancellationToken);

        await service.CreateEventAsync(new CreateEvent
        {
            Title = "In range",
            StartAt = now.AddDays(1),
            EndAt = now.AddDays(2),
            TotalSeats = 20,
            Description = "In range"
        }, TestContext.Current.CancellationToken);

        await service.CreateEventAsync(new CreateEvent
        {
            Title = "Out of range future",
            StartAt = now.AddDays(7),
            EndAt = now.AddDays(8),
            TotalSeats = 20,
            Description = "Future"
        }, TestContext.Current.CancellationToken);

        var result = await service.GetAllEventsAsync(
            from: now,
            to: now.AddDays(3),
            page: 1,
            pageSize: 10,
            ct: TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("In range");
    }

    [Fact]
    public async Task GetEventByIdAsync_ShouldReturnEvent_WhenItExists()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var created = await service.CreateEventAsync(CreateEventDto("Music Fest", 3, 4), TestContext.Current.CancellationToken);

        var result = await service.GetEventByIdAsync(created.Id, TestContext.Current.CancellationToken);

        result.Id.Should().Be(created.Id);
        result.Title.Should().Be("Music Fest");
    }

    [Fact]
    public async Task GetEventEntityByIdAsync_ShouldReturnEntity_WhenItExists()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = (EventService)scope.ServiceProvider.GetRequiredService<IEventService>();
        var created = await service.CreateEventAsync(CreateEventDto("Entity event", 2, 3), TestContext.Current.CancellationToken);

        var entity = await service.GetEventEntityByIdAsync(created.Id, TestContext.Current.CancellationToken);

        entity.Id.Should().Be(created.Id);
        entity.Title.Should().Be("Entity event");
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldUpdateEvent_WhenEventExists()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var created = await service.CreateEventAsync(CreateEventDto("Old Title", 3, 4), TestContext.Current.CancellationToken);

        var now = DateTime.UtcNow;
        var updatedDto = new UpdateEvent
        {
            Title = "New Title",
            StartAt = now.AddDays(5),
            EndAt = now.AddDays(6),
            Description = "Updated description"
        };

        var updatedEvent = await service.UpdateEventAsync(created.Id, updatedDto, TestContext.Current.CancellationToken);

        updatedEvent.Id.Should().Be(created.Id);
        updatedEvent.Title.Should().Be(updatedDto.Title);
        updatedEvent.Description.Should().Be(updatedDto.Description);
        updatedEvent.StartAt.Should().Be(updatedDto.StartAt!.Value);
        updatedEvent.EndAt.Should().Be(updatedDto.EndAt!.Value);
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldRemoveEvent_WhenItExists()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var created = await service.CreateEventAsync(CreateEventDto("Delete me", 3, 4), TestContext.Current.CancellationToken);

        await service.DeleteEventAsync(created.Id, TestContext.Current.CancellationToken);

        var action = () => service.GetEventByIdAsync(created.Id, TestContext.Current.CancellationToken);
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetEventEntityByIdAsync_ShouldThrowNotFound_WhenMissing()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = (EventService)scope.ServiceProvider.GetRequiredService<IEventService>();

        var action = () => service.GetEventEntityByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetEventByIdAsync_ShouldThrowNotFound_WhenMissing()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var action = () => service.GetEventByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldThrowNotFound_WhenMissing()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();
        var missingId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var dto = new UpdateEvent
        {
            Title = "Updated Event",
            StartAt = now.AddDays(1),
            EndAt = now.AddDays(2),
            Description = "Updated description"
        };
        var action = () => service.UpdateEventAsync(missingId, dto, TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldThrowNotFound_WhenMissing()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IEventService>();

        var action = () => service.DeleteEventAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    private static CreateEvent CreateEventDto(string title, int startOffsetDays, int endOffsetDays, int totalSeats = 10)
    {
        var now = DateTime.UtcNow;
        return new CreateEvent
        {
            Title = title,
            Description = "Description",
            StartAt = now.AddDays(startOffsetDays),
            EndAt = now.AddDays(endOffsetDays),
            TotalSeats = totalSeats
        };
    }
}