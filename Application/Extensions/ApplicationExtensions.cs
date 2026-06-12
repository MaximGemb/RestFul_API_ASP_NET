using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

/// <summary>
/// Регистрация зависимостей слоя приложения.
/// </summary>
public static class ApplicationServiceRegistration
{
    /// <summary>
    /// Регистрирует сервисы и фоновые задачи слоя Application.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}
