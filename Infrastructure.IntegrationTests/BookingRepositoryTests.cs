using Microsoft.EntityFrameworkCore;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.Repositories;
using Domain.Entities;
using Testcontainers.PostgreSql;
using Xunit;

namespace Infrastructure.IntegrationTests;

public class BookingRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.Migrate();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE");
    }

    [Fact]
    public async Task AddAsync_SavesBookingToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        
        var expectedEvent = Event.Create("Событие для брони", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        context.Events.Add(expectedEvent);
        await context.SaveChangesAsync();

        var repository = new BookingRepository(context);
        var booking = Booking.CreatePending(expectedEvent.Id);

        // Act
        await repository.AddAsync(booking);
        await repository.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
        
        Assert.NotNull(saved);
        Assert.Equal(expectedEvent.Id, saved.EventId);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task FindByIdAsync_ReturnsCorrectBooking()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        
        var testEvent = Event.Create("Тестовое событие", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 5);
        context.Events.Add(testEvent);
        await context.SaveChangesAsync();

        var expectedBooking = Booking.CreatePending(testEvent.Id);
        context.Bookings.Add(expectedBooking);
        await context.SaveChangesAsync();

        // Act
        var repository = new BookingRepository(CreateContext());
        var result = await repository.FindByIdAsync(expectedBooking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedBooking.Id, result.Id);
    }

    [Fact]
    public async Task UpdateBooking_ConfirmChangesStatusInDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var arrangeContext = CreateContext();
        
        var testEvent = Event.Create("Событие для обновления", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        arrangeContext.Events.Add(testEvent);
        await arrangeContext.SaveChangesAsync();

        var bookingToUpdate = Booking.CreatePending(testEvent.Id);
        arrangeContext.Bookings.Add(bookingToUpdate);
        await arrangeContext.SaveChangesAsync();

        // Act
        await using var actContext = CreateContext();
        var repository = new BookingRepository(actContext);
        var loadedBooking = await repository.FindByIdAsync(bookingToUpdate.Id);
        loadedBooking!.Confirm();
        await repository.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var updated = await verifyContext.Bookings.FirstAsync(b => b.Id == bookingToUpdate.Id);
        Assert.Equal(BookingStatus.Confirmed, updated.Status);
        Assert.NotNull(updated.ProcessedAt);
    }

    [Fact]
    public async Task GetPendingIdsAsync_ReturnsOnlyPendingBookings()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        
        var testEvent = Event.Create("Митап", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), 20);
        context.Events.Add(testEvent);
        await context.SaveChangesAsync();

        var pendingBooking1 = Booking.CreatePending(testEvent.Id);
        var pendingBooking2 = Booking.CreatePending(testEvent.Id);
        
        var confirmedBooking = Booking.CreatePending(testEvent.Id);
        confirmedBooking.Confirm();
        
        var rejectedBooking = Booking.CreatePending(testEvent.Id);
        rejectedBooking.Reject();

        context.Bookings.AddRange(pendingBooking1, pendingBooking2, confirmedBooking, rejectedBooking);
        await context.SaveChangesAsync();

        // Act
        var repository = new BookingRepository(CreateContext());
        var pendingIds = await repository.GetPendingIdsAsync();

        // Assert
        Assert.Equal(2, pendingIds.Count);
        Assert.Contains(pendingBooking1.Id, pendingIds);
        Assert.Contains(pendingBooking2.Id, pendingIds);
        Assert.DoesNotContain(confirmedBooking.Id, pendingIds);
        Assert.DoesNotContain(rejectedBooking.Id, pendingIds);
    }

    [Fact]
    public async Task CreateBooking_WithNonExistentEventId_ThrowsDbUpdateException()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new BookingRepository(context);
        
        var invalidEventId = Guid.NewGuid();
        var booking = Booking.CreatePending(invalidEventId);

        // Act
        await repository.AddAsync(booking);
        
        // Assert - Ограничение внешнего ключа не позволит сохранить бронь с несуществующим EventId
        await Assert.ThrowsAsync<DbUpdateException>(() => repository.SaveChangesAsync());
    }

    [Fact]
    public async Task DeleteEvent_CascadeDeletesBookings()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var testEvent = Event.Create("Каскадное удаление", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
        context.Events.Add(testEvent);
        await context.SaveChangesAsync();

        var booking = Booking.CreatePending(testEvent.Id);
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        // Act
        await using var actContext = CreateContext();
        var eventToDelete = await actContext.Events.FirstAsync(e => e.Id == testEvent.Id);
        actContext.Events.Remove(eventToDelete);
        await actContext.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var remainingBookings = await verifyContext.Bookings.Where(b => b.EventId == testEvent.Id).ToListAsync();
        Assert.Empty(remainingBookings);
    }
}
