using Microsoft.Extensions.DependencyInjection;
using BookingService.Application.Interfaces;
using BookingService.Application.Services;

namespace BookingService.Application.Extensions;

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
        services.AddScoped<IBookingService, BookingService.Application.Services.BookingService>();
        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}
