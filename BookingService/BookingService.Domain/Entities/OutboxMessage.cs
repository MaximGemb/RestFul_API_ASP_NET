using System.Diagnostics.CodeAnalysis;

namespace BookingService.Domain.Entities;

/// <summary>
/// Запись исходящего сообщения для паттерна Outbox.
/// Сохраняется в той же транзакции, что и изменение бизнес-сущности,
/// и публикуется в брокер сообщений отдельным relay-сервисом.
/// </summary>
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public sealed class OutboxMessage
{
    /// <summary>
    /// Конструктор по умолчанию для Entity Framework.
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private OutboxMessage()
    {
    }

    private OutboxMessage(Guid id, string type, string payload, DateTime createdAt)
    {
        Id = id;
        Type = type;
        Payload = payload;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Уникальный идентификатор записи.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Тип события (имя класса контракта).
    /// </summary>
    public string Type { get; private set; } = default!;

    /// <summary>
    /// Сериализованное тело сообщения (JSON).
    /// </summary>
    public string Payload { get; private set; } = default!;

    /// <summary>
    /// Момент создания записи (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Момент успешной публикации в брокер (UTC).
    /// <c>null</c> — сообщение ещё не отправлено.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Создаёт новую запись Outbox.
    /// </summary>
    /// <param name="type">Тип события.</param>
    /// <param name="payload">Сериализованное тело сообщения.</param>
    public static OutboxMessage Create(string type, string payload) =>
        new(Guid.NewGuid(), type, payload, DateTime.UtcNow);

    /// <summary>
    /// Помечает сообщение как успешно опубликованное.
    /// </summary>
    public void MarkAsProcessed() => ProcessedAt = DateTime.UtcNow;
}
