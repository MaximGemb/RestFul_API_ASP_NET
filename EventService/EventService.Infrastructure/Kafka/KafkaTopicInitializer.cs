using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Contracts.BookingContracts;

namespace EventService.Infrastructure.Kafka;

/// <summary>
/// Hosted-сервис, создающий необходимые Kafka-топики при старте приложения.
/// Регистрируется до <see cref="BookingConfirmedConsumer"/>, чтобы топик
/// гарантированно существовал перед первым чтением.
/// Если топик уже существует или создать его не удалось — сервис не падает.
/// </summary>
public sealed class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="KafkaTopicInitializer"/>.
    /// </summary>
    /// <param name="options">Параметры Kafka из конфигурации.</param>
    /// <param name="logger">Логгер.</param>
    public KafkaTopicInitializer(IOptions<KafkaOptions> options, ILogger<KafkaTopicInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var adminConfig = new AdminClientConfig { BootstrapServers = _options.BootstrapServers };
            using var adminClient = new AdminClientBuilder(adminConfig).Build();

            var topicSpec = new TopicSpecification
            {
                Name = BookingTopics.BOOKING_CONFIRMED,
                NumPartitions = 1,
                ReplicationFactor = 1
            };

            await adminClient.CreateTopicsAsync([topicSpec]);
            _logger.LogInformation("Топик {Topic} успешно создан.", BookingTopics.BOOKING_CONFIRMED);
        }
        catch (CreateTopicsException ex)
            when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("Топик {Topic} уже существует, пропускаем создание.", BookingTopics.BOOKING_CONFIRMED);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Не удалось создать топик {Topic}. Подписчик попробует подключиться к существующему топику.",
                BookingTopics.BOOKING_CONFIRMED);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
