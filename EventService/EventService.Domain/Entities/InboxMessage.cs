namespace EventService.Domain.Entities;

/// <summary>
/// Запись входящего сообщения для паттерна Inbox.
/// Хранит идентификатор уже обработанного сообщения из брокера,
/// что обеспечивает идемпотентность обработки при повторной доставке (at-least-once).
/// </summary>
public sealed class InboxMessage
{
    /// <summary>
    /// Конструктор по умолчанию для Entity Framework.
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private InboxMessage()
    {
    }

    private InboxMessage(Guid messageId, string messageType, DateTime processedAt)
    {
        MessageId = messageId;
        MessageType = messageType;
        ProcessedAt = processedAt;
    }

    /// <summary>
    /// Бизнес-идентификатор обработанного сообщения (например, BookingId).
    /// Является первичным ключом — обеспечивает уникальность по смыслу.
    /// </summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// Тип обработанного сообщения (имя класса контракта).
    /// </summary>
    public string MessageType { get; private set; } = default!;

    /// <summary>
    /// Момент обработки сообщения (UTC).
    /// </summary>
    public DateTime ProcessedAt { get; private set; }

    /// <summary>
    /// Создаёт новую запись Inbox для указанного сообщения.
    /// </summary>
    /// <param name="messageId">Бизнес-идентификатор обработанного сообщения.</param>
    /// <param name="messageType">Тип сообщения.</param>
    public static InboxMessage Create(Guid messageId, string messageType) =>
        new(messageId, messageType, DateTime.UtcNow);
}
