using Microsoft.EntityFrameworkCore;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.Repositories;
using Domain.Entities;
using Testcontainers.PostgreSql;
using Xunit;

namespace Infrastructure.IntegrationTests;

public class UserRepositoryTests : IAsyncLifetime
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
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE users, events, bookings RESTART IDENTITY CASCADE");
    }

    [Fact]
    public async Task AddAsync_SavesUserToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new UserRepository(context);
        var user = User.Create("alice", "hash_alice", Roles.User);

        // Act
        await repository.AddAsync(user);
        await repository.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

        Assert.NotNull(saved);
        Assert.Equal("alice", saved.Login);
        Assert.Equal("hash_alice", saved.PasswordHash);
        Assert.Equal(Roles.User, saved.Role);
    }

    [Fact]
    public async Task FindByLoginAsync_ReturnsCorrectUser()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var user = User.Create("bob", "hash_bob", Roles.Admin);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var repository = new UserRepository(CreateContext());
        var result = await repository.FindByLoginAsync("bob");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("bob", result.Login);
        Assert.Equal(Roles.Admin, result.Role);
    }

    [Fact]
    public async Task FindByLoginAsync_ReturnsNull_WhenUserNotFound()
    {
        await ResetDatabaseAsync();

        // Act
        var repository = new UserRepository(CreateContext());
        var result = await repository.FindByLoginAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsByLoginAsync_ReturnsTrue_WhenUserExists()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var user = User.Create("charlie", "hash_charlie", Roles.User);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var repository = new UserRepository(CreateContext());
        var exists = await repository.ExistsByLoginAsync("charlie");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByLoginAsync_ReturnsFalse_WhenUserDoesNotExist()
    {
        await ResetDatabaseAsync();

        // Act
        var repository = new UserRepository(CreateContext());
        var exists = await repository.ExistsByLoginAsync("ghost");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateLogin_ThrowsDbUpdateException()
    {
        await ResetDatabaseAsync();

        // Arrange — сохраняем первого пользователя
        await using var firstContext = CreateContext();
        var firstUser = User.Create("duplicate_login", "hash1", Roles.User);
        firstContext.Users.Add(firstUser);
        await firstContext.SaveChangesAsync();

        // Act — пытаемся добавить второго с тем же логином
        await using var secondContext = CreateContext();
        var repository = new UserRepository(secondContext);
        var duplicateUser = User.Create("duplicate_login", "hash2", Roles.Admin);
        await repository.AddAsync(duplicateUser);

        // Assert — уникальный индекс на login не допустит сохранения
        await Assert.ThrowsAsync<DbUpdateException>(() => repository.SaveChangesAsync());
    }
}
