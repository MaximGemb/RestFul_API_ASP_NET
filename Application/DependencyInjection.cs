using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

/// <summary>
/// Методы расширения для регистрации сервисов слоя Application в контейнере зависимостей.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует все сервисы слоя Application.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}
