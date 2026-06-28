using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BookingService.Application.Interfaces;
using BookingService.Infrastructure.DataAccess;
using BookingService.Infrastructure.DataAccess.Repositories;
using BookingService.Infrastructure.Kafka;

namespace BookingService.Infrastructure.Extensions;

/// <summary>
/// Регистрация зависимостей инфраструктурного слоя.
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Регистрирует DbContext, репозитории и Kafka-издателя.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="connectionString">Строка подключения к базе данных.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string? connectionString,
        IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        services.AddDbContext<BookingsDbContext>(opt => opt.UseNpgsql(connectionString));
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }
}
