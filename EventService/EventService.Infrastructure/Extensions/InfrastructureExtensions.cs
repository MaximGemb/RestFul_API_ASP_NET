using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EventService.Application.Interfaces;
using EventService.Infrastructure.DataAccess;
using EventService.Infrastructure.DataAccess.Repositories;

namespace EventService.Infrastructure.Extensions;

/// <summary>
/// Регистрация зависимостей инфраструктурного слоя.
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Регистрирует DbContext и репозитории инфраструктурного слоя.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="connectionString">Строка подключения к базе данных.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string? connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(opt => opt.UseNpgsql(connectionString));
        services.AddScoped<IEventRepository, EventRepository>();

        return services;
    }
}
