using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestFulApi.DataAccess;
using RestFulApi.DTOs;
using RestFulApi.Exceptions;
using RestFulApi.Interfaces;
using RestFulApi.Models;
using RestFulApi.Services;
using Xunit;

namespace RestFulApi.Tests.Services;

public class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IBookingService, BookingService>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public async Task CreateBookingAsync_ShouldReturnPendingBooking_WhenEventExists()
    {
        var eventId = await SeedEventAsync(totalSeats: 3);

        await using var scope = _serviceProvider.CreateAsyncScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var booking = await bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.Id.Should().NotBe(Guid.Empty);

        await using var verifyScope = _serviceProvider.CreateAsyncScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updatedEvent = await context.Events.SingleAsync(e => e.Id == eventId, TestContext.Current.CancellationToken);
        updatedEvent.AvailableSeats.Should().Be(2);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var action = () => bookingService.CreateBookingAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenSeatsEnded()
    {
        var eventId = await SeedEventAsync(totalSeats: 1);

        await using var scope1 = _serviceProvider.CreateAsyncScope();
        var bookingService1 = scope1.ServiceProvider.GetRequiredService<IBookingService>();
        await bookingService1.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        await using var scope2 = _serviceProvider.CreateAsyncScope();
        var bookingService2 = scope2.ServiceProvider.GetRequiredService<IBookingService>();
        var action = () => bookingService2.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<NoAvailableSeatsException>();
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenBookingExists()
    {
        var eventId = await SeedEventAsync();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var created = await bookingService.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        var result = await bookingService.GetBookingByIdAsync(created.Id, TestContext.Current.CancellationToken);

        result.Id.Should().Be(created.Id);
        result.EventId.Should().Be(eventId);
        result.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowNotFoundException_WhenBookingMissing()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var action = () => bookingService.GetBookingByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<Guid> SeedEventAsync(int totalSeats = 10)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var @event = Event.Create(
            title: "Test event",
            startAt: DateTime.UtcNow.AddDays(2),
            endAt: DateTime.UtcNow.AddDays(3),
            totalSeats: totalSeats,
            description: "desc");

        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return @event.Id;
    }
}
