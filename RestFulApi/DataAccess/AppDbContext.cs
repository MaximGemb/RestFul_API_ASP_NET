using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace RestFulApi.DataAccess;

/// <summary>
/// Контекст базы данных приложения. Предоставляет доступ к таблицам событий и бронирований.
/// </summary>
/// <param name="options">Параметры конфигурации контекста базы данных.</param>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Набор событий в базе данных.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();
    /// <summary>
    /// Набор бронирований в базе данных.
    /// </summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>
    /// Применяет все конфигурации сущностей из текущей сборки.
    /// </summary>
    /// <param name="modelBuilder">Строитель модели Entity Framework.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}