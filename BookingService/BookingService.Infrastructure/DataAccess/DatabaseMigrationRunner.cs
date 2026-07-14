using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.DataAccess;

/// <summary>
/// Утилита для выполнения миграций базы данных.
/// </summary>
public static class DatabaseMigrationRunner
{
    /// <summary>
    /// Применяет миграции, если база данных является реляционной.
    /// </summary>
    /// <param name="db">Контекст базы данных.</param>
    public static void MigrateIfRelational(BookingsDbContext db)
        => MigrateIfRelational(db.Database.IsRelational(), db.Database.Migrate);

    /// <summary>
    /// Внутренний метод для применения миграций с передачей функции миграции.
    /// </summary>
    internal static void MigrateIfRelational(bool isRelational, Action migrate)
    {
        if (isRelational)
            migrate();
    }
}
