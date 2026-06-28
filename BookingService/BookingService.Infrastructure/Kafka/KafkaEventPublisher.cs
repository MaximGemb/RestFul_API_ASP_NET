using System.Text.Json;
using BookingService.Application.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Contracts.BookingContracts;

namespace BookingService.Infrastructure.Kafka;

/// <summary>
/// Реализация <see cref="IEventPublisher"/> на базе Apache Kafka.
/// Продюсер — потокобезопасный тяжёлый объект; создаётся один раз и освобождается
/// при остановке приложения.
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    /// <summary>
    /// Инициализирует издателя и создаёт Kafka-продюсера.
    /// </summary>
    /// <param name="options">Параметры Kafka из конфигурации.</param>
    /// <param name="logger">Логгер.</param>
    public KafkaEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    /// <inheritdoc />
    public async Task PublishBookingConfirmedAsync(BookingConfirmed message, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(message);

        var kafkaMessage = new Message<string, string>
        {
            Key = message.EventId.ToString(),
            Value = payload
        };

        var result = await _producer.ProduceAsync(BookingTopics.BOOKING_CONFIRMED, kafkaMessage, ct);

        _logger.LogInformation(
            "Событие BookingConfirmed опубликовано: BookingId={BookingId}, EventId={EventId}, Offset={Offset}",
            message.BookingId, message.EventId, result.Offset);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
