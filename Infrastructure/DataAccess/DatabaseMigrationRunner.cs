using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess;

/// <summary>
/// Утилита для выполнения миграций базы данных.
/// </summary>
public static class DatabaseMigrationRunner
{
    /// <summary>
    /// Применяет миграции, если база данных является реляционной.
    /// </summary>
    /// <param name="db">Контекст базы данных.</param>
    public static void MigrateIfRelational(AppDbContext db)
        => MigrateIfRelational(db.Database.IsRelational(), db.Database.Migrate);

    /// <summary>
    /// Внутренний метод для применения миграций с передачей функции миграции.
    /// </summary>
    /// <param name="isRelational">Флаг, указывающий, является ли база данных реляционной.</param>
    /// <param name="migrate">Действие, выполняющее миграцию.</param>
    internal static void MigrateIfRelational(bool isRelational, Action migrate)
    {
        if (isRelational)
            migrate();
    }
}
