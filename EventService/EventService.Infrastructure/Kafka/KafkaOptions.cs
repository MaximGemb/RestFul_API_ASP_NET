namespace EventService.Infrastructure.Kafka;

/// <summary>
/// Параметры подключения к Kafka-брокеру.
/// Считываются из секции <c>Kafka</c> конфигурации приложения.
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>Адрес (или список адресов) bootstrap-брокеров.</summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Имя группы потребителей.
    /// Внутри одной группы каждое сообщение получает только один экземпляр сервиса.
    /// </summary>
    public string ConsumerGroup { get; set; } = "event-service-group";
}
