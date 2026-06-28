namespace BookingService.Infrastructure.Kafka;

/// <summary>
/// Параметры подключения к Kafka-брокеру.
/// Считываются из секции <c>Kafka</c> конфигурации приложения.
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>Адрес (или список адресов) bootstrap-брокеров.</summary>
    public string BootstrapServers { get; set; } = "localhost:9092";
}
