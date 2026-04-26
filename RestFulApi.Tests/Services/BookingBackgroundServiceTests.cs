using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RestFulApi.DataAccess;
using RestFulApi.Models;
using RestFulApi.Services;
using Xunit;

namespace RestFulApi.Tests.Services;

public class BookingBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldProcessPendingBooking_AndConfirmIt()
    {
        var provider = BuildProvider();
        await SeedPendingBookingAsync(provider, withExistingEvent: true);
        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        _ = await Record.ExceptionAsync(() => service.ExposeExecuteAsync(cts.Token));

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await context.Bookings.SingleAsync(TestContext.Current.CancellationToken);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectPendingBooking_WhenEventNotFound()
    {
        var provider = BuildProvider();
        await SeedPendingBookingAsync(provider, withExistingEvent: false);
        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        _ = await Record.ExceptionAsync(() => service.ExposeExecuteAsync(cts.Token));

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await context.Bookings.SingleAsync(TestContext.Current.CancellationToken);

        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DelayProcessingAsync_ShouldThrowOperationCanceled_WhenTokenCanceled()
    {
        var provider = BuildProvider();
        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExposeBaseDelayProcessingAsync(cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepConfirmedBookingUnchanged_WhenNoPendingBookings()
    {
        var provider = BuildProvider();
        await SeedProcessedBookingAsync(provider);
        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        _ = await Record.ExceptionAsync(() => service.ExposeExecuteAsync(cts.Token));

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await context.Bookings.SingleAsync(TestContext.Current.CancellationToken);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowOperationCanceled_WhenCanceledDuringPollingDelay()
    {
        var provider = BuildProvider();
        await SeedPendingBookingAsync(provider, withExistingEvent: true);

        using var cts = new CancellationTokenSource();
        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance)
        {
            DelayAction = _ =>
            {
                cts.Cancel();
                return Task.CompletedTask;
            }
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExposeExecuteAsync(cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipBooking_WhenItWasDeletedBeforeProcessing()
    {
        var provider = BuildProvider();
        var bookingId = await SeedPendingBookingAsync(provider, withExistingEvent: true);
        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance)
        {
            DelayAction = async _ =>
            {
                await using var deleteScope = provider.CreateAsyncScope();
                var deleteContext = deleteScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var bookingToDelete = await deleteContext.Bookings.FirstAsync(
                    b => b.Id == bookingId,
                    TestContext.Current.CancellationToken);
                deleteContext.Bookings.Remove(bookingToDelete);
                await deleteContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        _ = await Record.ExceptionAsync(() => service.ExposeExecuteAsync(cts.Token));

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await context.Bookings.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectBookingAndReleaseSeat_WhenProcessingThrows()
    {
        var provider = BuildProvider();
        var eventId = await SeedEventAsync(provider, totalSeats: 5);
        var bookingId = await SeedPendingBookingAsync(provider, withExistingEvent: true, eventIdOverride: eventId);
        await ReserveSeatAsync(provider, eventId);

        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance)
        {
            DelayAction = _ => throw new InvalidOperationException("Simulated failure")
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        _ = await Record.ExceptionAsync(() => service.ExposeExecuteAsync(cts.Token));

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await context.Bookings.SingleAsync(b => b.Id == bookingId, TestContext.Current.CancellationToken);
        var @event = await context.Events.SingleAsync(e => e.Id == eventId, TestContext.Current.CancellationToken);

        booking.Status.Should().Be(BookingStatus.Rejected);
        @event.AvailableSeats.Should().Be(@event.TotalSeats);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLeaveBookingPending_WhenProcessingCanceled()
    {
        var provider = BuildProvider();
        await SeedPendingBookingAsync(provider, withExistingEvent: true);
        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance)
        {
            DelayAction = token => Task.Delay(TimeSpan.FromSeconds(5), token)
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        _ = await Record.ExceptionAsync(() => service.ExposeExecuteAsync(cts.Token));

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await context.Bookings.SingleAsync(TestContext.Current.CancellationToken);

        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleException_WhenPollingScopeCreationFails()
    {
        var provider = BuildProvider();
        var scopeFactory = new FailingOnCallScopeFactory(
            provider.GetRequiredService<IServiceScopeFactory>(),
            failingCallNumber: 1);

        var service = new TestBookingBackgroundService(
            scopeFactory,
            NullLogger<BookingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        _ = await Record.ExceptionAsync(() => service.ExposeExecuteAsync(cts.Token));

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await context.Bookings.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepBookingPending_WhenRecoveryScopeFailsAfterProcessingError()
    {
        var provider = BuildProvider();
        var bookingId = await SeedPendingBookingAsync(provider, withExistingEvent: true);
        var scopeFactory = new FailingOnCallScopeFactory(
            provider.GetRequiredService<IServiceScopeFactory>(),
            failingCallNumber: 2);

        var service = new TestBookingBackgroundService(
            scopeFactory,
            NullLogger<BookingBackgroundService>.Instance)
        {
            DelayAction = _ => throw new InvalidOperationException("Simulated processing failure")
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        _ = await Record.ExceptionAsync(() => service.ExposeExecuteAsync(cts.Token));

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await context.Bookings.SingleAsync(b => b.Id == bookingId, TestContext.Current.CancellationToken);

        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNormally_WhenTokenAlreadyCanceled()
    {
        var provider = BuildProvider();
        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await service.ExposeExecuteAsync(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExitGracefully_WhenOperationCanceledInsideTryBlock()
    {
        var provider = BuildProvider();
        using var cts = new CancellationTokenSource();
        var scopeFactory = new CancelingOnCallScopeFactory(cts);

        var service = new TestBookingBackgroundService(
            scopeFactory,
            NullLogger<BookingBackgroundService>.Instance);

        await service.ExposeExecuteAsync(cts.Token);
    }

    [Fact]
    public async Task DelayPollingAsync_ShouldThrowOperationCanceled_WhenTokenCanceled()
    {
        var provider = BuildProvider();
        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExposeBaseDelayPollingAsync(cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompletePollingLoopIteration_WhenDelayPollingCompletesNormally()
    {
        var provider = BuildProvider();
        using var cts = new CancellationTokenSource();
        var service = new TestBookingBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BookingBackgroundService>.Instance)
        {
            DelayPollingAction = _ =>
            {
                cts.Cancel();
                return Task.CompletedTask;
            }
        };

        await service.ExposeExecuteAsync(cts.Token);
    }

    private static ServiceProvider BuildProvider()
    {
        var dbName = $"BgTests_{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }

    private static async Task<Guid> SeedPendingBookingAsync(
        ServiceProvider provider,
        bool withExistingEvent,
        Guid? eventIdOverride = null)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventId = eventIdOverride ?? Guid.NewGuid();

        if (withExistingEvent && eventIdOverride is null)
        {
            var @event = Event.Create(
                title: "Background event",
                startAt: DateTime.UtcNow.AddDays(1),
                endAt: DateTime.UtcNow.AddDays(2),
                totalSeats: 5,
                description: "desc");

            await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
            eventId = @event.Id;
        }

        var booking = Booking.CreatePending(eventId);
        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return booking.Id;
    }

    private static async Task SeedProcessedBookingAsync(ServiceProvider provider)
    {
        var eventId = await SeedEventAsync(provider, totalSeats: 4);

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = Booking.CreatePending(eventId);
        booking.Confirm();
        await context.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<Guid> SeedEventAsync(ServiceProvider provider, int totalSeats)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var @event = Event.Create(
            title: "Background event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: totalSeats,
            description: "desc");

        await context.Events.AddAsync(@event, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return @event.Id;
    }

    private static async Task ReserveSeatAsync(ServiceProvider provider, Guid eventId)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var @event = await context.Events.SingleAsync(e => e.Id == eventId, TestContext.Current.CancellationToken);
        @event.TryReserveSeats();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed class TestBookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        Microsoft.Extensions.Logging.ILogger<BookingBackgroundService> logger)
        : BookingBackgroundService(scopeFactory, logger)
    {
        public Func<CancellationToken, Task> DelayAction { get; set; } = _ => Task.CompletedTask;

        public Func<CancellationToken, Task> DelayPollingAction { get; set; } =
            token => Task.Delay(TimeSpan.FromSeconds(5), token);

        public Task ExposeExecuteAsync(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);

        public Task ExposeBaseDelayProcessingAsync(CancellationToken stoppingToken) => base.DelayProcessingAsync(stoppingToken);

        public Task ExposeBaseDelayPollingAsync(CancellationToken stoppingToken) => base.DelayPollingAsync(stoppingToken);

        protected override Task DelayProcessingAsync(CancellationToken stoppingToken) => DelayAction(stoppingToken);

        protected override Task DelayPollingAsync(CancellationToken stoppingToken) => DelayPollingAction(stoppingToken);
    }

    private sealed class FailingOnCallScopeFactory(
        IServiceScopeFactory innerScopeFactory,
        int failingCallNumber) : IServiceScopeFactory
    {
        private int _calls;

        public IServiceScope CreateScope()
        {
            var currentCall = Interlocked.Increment(ref _calls);
            if (currentCall == failingCallNumber)
                throw new InvalidOperationException($"Scope factory failure on call {currentCall}");

            return innerScopeFactory.CreateScope();
        }
    }

    private sealed class CancelingOnCallScopeFactory(
        CancellationTokenSource cts) : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        }
    }
}