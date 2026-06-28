using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BookingService.Application.Interfaces;
using BookingService.Infrastructure.DataAccess;
using BookingService.Infrastructure.DataAccess.Repositories;
using BookingService.Infrastructure.HttpClients;

namespace BookingService.Infrastructure.Extensions;

/// <summary>
/// Регистрация зависимостей инфраструктурного слоя.
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Регистрирует DbContext, репозитории и HTTP-клиент для EventService.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="connectionString">Строка подключения к базе данных.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string? connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<BookingsDbContext>(opt => opt.UseNpgsql(connectionString));
        services.AddScoped<IBookingRepository, BookingRepository>();

        var eventServiceBaseUrl = configuration["EventService:BaseUrl"]
                                  ?? throw new InvalidOperationException(
                                      "Не задан URL EventService (EventService:BaseUrl).");

        services.AddHttpClient<IEventServiceClient, EventServiceHttpClient>(client =>
        {
            client.BaseAddress = new Uri(eventServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
