using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.DataAccess;

/// <summary>
/// Контекст базы данных сервиса пользователей.
/// </summary>
/// <param name="options">Параметры конфигурации контекста базы данных.</param>
public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Набор пользователей в базе данных.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Применяет все конфигурации сущностей из текущей сборки.
    /// </summary>
    /// <param name="modelBuilder">Строитель модели Entity Framework.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
}
