using Microsoft.EntityFrameworkCore;
using EventService.Domain.Entities;

namespace EventService.Infrastructure.DataAccess;

/// <summary>
/// Контекст базы данных сервиса событий.
/// </summary>
/// <param name="options">Параметры конфигурации контекста базы данных.</param>
public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Набор событий в базе данных.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>
    /// Применяет все конфигурации сущностей из текущей сборки.
    /// </summary>
    /// <param name="modelBuilder">Строитель модели Entity Framework.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
}
