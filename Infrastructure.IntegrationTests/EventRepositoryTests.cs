using Microsoft.EntityFrameworkCore;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.Repositories;
using Domain.Entities;
using Testcontainers.PostgreSql;
using Xunit;

namespace Infrastructure.IntegrationTests;

public class EventRepositoryTests : IAsyncLifetime
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
    public async Task AddAsync_SavesEventToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);
        var newEvent = Event.Create("Конференция", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 100);

        // Act
        await repository.AddAsync(newEvent);
        await repository.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events.FirstOrDefaultAsync(e => e.Id == newEvent.Id);

        Assert.NotNull(saved);
        Assert.Equal("Конференция", saved.Title);
        Assert.Equal(100, saved.TotalSeats);
    }

    [Fact]
    public async Task FindByIdAsync_ReturnsCorrectEvent()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var expectedEvent = Event.Create("Концерт", DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(6), 50);
        context.Events.Add(expectedEvent);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());
        var result = await repository.FindByIdAsync(expectedEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEvent.Id, result.Id);
        Assert.Equal("Концерт", result.Title);
    }

    [Fact]
    public async Task Remove_RemovesFromDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var eventToRemove = Event.Create("Удаляемое событие", DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(11),
            10);
        context.Events.Add(eventToRemove);
        await context.SaveChangesAsync();

        // Act
        await using var actContext = CreateContext();
        var repository = new EventRepository(actContext);
        var loadedEvent = await actContext.Events.FirstAsync(e => e.Id == eventToRemove.Id);
        repository.Remove(loadedEvent);
        await repository.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var deleted = await verifyContext.Events.FirstOrDefaultAsync(e => e.Id == eventToRemove.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task UpdateEvent_ChangesFieldInDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var arrangeContext = CreateContext();
        var eventToUpdate = Event.Create("Старое название", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 50);
        arrangeContext.Events.Add(eventToUpdate);
        await arrangeContext.SaveChangesAsync();

        // Act
        await using var actContext = CreateContext();
        var repository = new EventRepository(actContext);
        var loadedEvent = await repository.FindByIdAsync(eventToUpdate.Id);
        loadedEvent!.Update("Новое название", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), "Новое описание");
        await repository.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var updated = await verifyContext.Events.FirstAsync(e => e.Id == eventToUpdate.Id);
        Assert.Equal("Новое название", updated.Title);
        Assert.Equal("Новое описание", updated.Description);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersAndPaginatesCorrectly()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        var event1 = Event.Create("C# Meetup", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 50);
        var event2 = Event.Create("ASP.NET Core Conference", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4),
            100);
        var event3 = Event.Create("Python Meetup", DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(6), 20);

        context.Events.AddRange(event1, event2, event3);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());

        // Поиск по title = "meetup", это должно вернуть event1 и event3 (регистронезависимо)
        // Сортировка по убыванию StartAt, так что первым будет event3, потом event1
        var (items, totalCount) = await repository.GetPagedAsync("meetup", null, null, 1, 10);

        // Assert
        Assert.Equal(2, totalCount);
        Assert.Equal(2, items.Count);
        Assert.Equal(event3.Id, items[0].Id);
        Assert.Equal(event1.Id, items[1].Id);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByDateCorrectly()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        var now = DateTime.UtcNow;
        var event1 = Event.Create("Event 1", now.AddDays(1), now.AddDays(2), 10);
        var event2 = Event.Create("Event 2", now.AddDays(5), now.AddDays(6), 10);
        var event3 = Event.Create("Event 3", now.AddDays(10), now.AddDays(11), 10);

        context.Events.AddRange(event1, event2, event3);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());

        var from = now.AddDays(4);
        var to = now.AddDays(7);

        // В этот диапазон должен попасть только event2
        var (items, totalCount) = await repository.GetPagedAsync(null, from, to, 1, 10);

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal(event2.Id, items[0].Id);
    }

    [Fact]
    public async Task GetPagedAsync_PaginatesCorrectly()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        var now = DateTime.UtcNow;
        var event1 = Event.Create("Event 1", now.AddDays(1), now.AddDays(2), 10);
        var event2 = Event.Create("Event 2", now.AddDays(3), now.AddDays(4), 10);
        var event3 = Event.Create("Event 3", now.AddDays(5), now.AddDays(6), 10);

        context.Events.AddRange(event1, event2, event3);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());

        // Сортировка по убыванию StartAt, так что порядок будет: event3, event2, event1
        // Берем страницу 2 размером 1, это должен быть event2 (он будет вторым)
        var (items, totalCount) = await repository.GetPagedAsync(null, null, null, 2, 1);

        // Assert
        Assert.Equal(3, totalCount);
        Assert.Single(items);
        Assert.Equal(event2.Id, items[0].Id);
    }

    [Fact]
    public async Task GetPagedAsync_CombinesAllFiltersAndPaginationCorrectly()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();

        var now = DateTime.UtcNow;
        var event1 = Event.Create("Tech Meetup 1", now.AddDays(1), now.AddDays(2), 10);
        var event2 =
            Event.Create("Tech Meetup 2", now.AddDays(3), now.AddDays(4), 10); // Matches Title, falls in Date range
        var event3 =
            Event.Create("Tech Meetup 3", now.AddDays(5), now.AddDays(6), 10); // Matches Title, falls in Date range
        var event4 =
            Event.Create("Tech Meetup 4", now.AddDays(10), now.AddDays(11), 10); // Matches Title, out of Date range
        var event5 =
            Event.Create("Music Concert", now.AddDays(4), now.AddDays(5),
                10); // Doesn't match Title, falls in Date range

        context.Events.AddRange(event1, event2, event3, event4, event5);
        await context.SaveChangesAsync();

        // Act
        var repository = new EventRepository(CreateContext());

        // title = "tech meetup"
        // from = now.AddDays(2)
        // to = now.AddDays(8)
        // Matches event2 and event3.
        // OrderByDescending(e => e.StartAt) -> event3, then event2.
        // page = 2, pageSize = 1 -> should return event2.

        var (items, totalCount) = await repository.GetPagedAsync(
            title: "tech meetup",
            from: now.AddDays(2),
            to: now.AddDays(8),
            page: 2,
            pageSize: 1);

        // Assert
        Assert.Equal(2, totalCount);
        Assert.Single(items);
        Assert.Equal(event2.Id, items[0].Id);
    }
}