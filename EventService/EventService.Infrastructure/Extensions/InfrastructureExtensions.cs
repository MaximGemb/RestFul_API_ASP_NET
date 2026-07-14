using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EventService.Application.Interfaces;
using EventService.Infrastructure.DataAccess;
using EventService.Infrastructure.DataAccess.Repositories;
using EventService.Infrastructure.Kafka;
using EventService.Infrastructure.Redis;
using StackExchange.Redis;

namespace EventService.Infrastructure.Extensions;

/// <summary>
/// Регистрация зависимостей инфраструктурного слоя.
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Регистрирует DbContext, репозитории и Kafka-подписчик инфраструктурного слоя.
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
        services.AddScoped<IInboxRepository, InboxRepository>();

        services.Configure<KafkaOptions>(opts => configuration.GetSection("Kafka").Bind(opts));
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingConfirmedConsumer>();

        services.Configure<RedisOptions>(opts => configuration.GetSection("Redis").Bind(opts));
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            var configurationOptions = new ConfigurationOptions
            {
                EndPoints = { redisOptions.EndPoint },
                Password = redisOptions.Password,
                ConnectTimeout = redisOptions.ConnectTimeout,
                SyncTimeout = redisOptions.SyncTimeout,
                AbortOnConnectFail = redisOptions.AbortOnConnectFail
            };

            // ConnectionMultiplexer — тяжёлый потокобезопасный объект,
            // создаётся один раз и переиспользуется как singleton на весь жизненный цикл приложения.
            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        return services;
    }
}
