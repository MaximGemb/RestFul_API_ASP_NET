using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestFulApi.DataAccess;
using Xunit;

namespace RestFulApi.Tests;

public class DatabaseMigrationRunnerTests
{
    [Fact]
    public void MigrateIfRelational_WhenRelational_ShouldCallMigrate()
    {
        var migrateCalled = false;

        DatabaseMigrationRunner.MigrateIfRelational(isRelational: true, migrate: () => migrateCalled = true);

        migrateCalled.Should().BeTrue();
    }

    [Fact]
    public void MigrateIfRelational_WhenNotRelational_ShouldNotCallMigrate()
    {
        var migrateCalled = false;

        DatabaseMigrationRunner.MigrateIfRelational(isRelational: false, migrate: () => migrateCalled = true);

        migrateCalled.Should().BeFalse();
    }

    [Fact]
    public void MigrateIfRelational_WithInMemoryDbContext_ShouldNotCallMigrate()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("MigrationRunner_Test_" + Guid.NewGuid()));

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var migrateCalled = false;
        DatabaseMigrationRunner.MigrateIfRelational(db.Database.IsRelational(), () => migrateCalled = true);

        db.Database.IsRelational().Should().BeFalse();
        migrateCalled.Should().BeFalse();
    }
}
