using Microsoft.Extensions.DependencyInjection;
using EventService.Application.Interfaces;

namespace EventService.Application.Extensions;

/// <summary>
/// Регистрация зависимостей слоя Application.
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// Регистрирует сервисы слоя Application.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService.Application.Services.EventService>();

        return services;
    }
}
