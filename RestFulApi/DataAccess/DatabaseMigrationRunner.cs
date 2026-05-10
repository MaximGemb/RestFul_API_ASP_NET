using Microsoft.EntityFrameworkCore;

namespace RestFulApi.DataAccess;

/// <summary>
/// 
/// </summary>
public static class DatabaseMigrationRunner
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="db"></param>
    public static void MigrateIfRelational(AppDbContext db)
        => MigrateIfRelational(db.Database.IsRelational(), db.Database.Migrate);

    internal static void MigrateIfRelational(bool isRelational, Action migrate)
    {
        if (isRelational)
            migrate();
    }
}
