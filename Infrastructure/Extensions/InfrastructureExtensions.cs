using Application.Interfaces;
using Application.Options;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

/// <summary>
/// Регистрация зависимостей инфраструктурного слоя.
/// </summary>
public static class InfrastructureServiceRegistration
{
    /// <summary>
    /// Регистрирует DbContext и репозитории инфраструктурного слоя.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="connectionString">Строка подключения к базе данных.</param>
    /// <param name="configuration">Конфигурация приложения для привязки параметров JWT.</param>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string? connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        services.Configure<JwtSettings>(options => configuration.GetSection("Jwt").Bind(options));
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}