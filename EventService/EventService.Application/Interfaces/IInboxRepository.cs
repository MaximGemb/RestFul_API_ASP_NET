namespace EventService.Application.Interfaces;

/// <summary>
/// Определяет контракт репозитория для работы с Inbox-сообщениями.
/// </summary>
public interface IInboxRepository
{
    /// <summary>
    /// Возвращает <c>true</c>, если сообщение с указанным идентификатором уже обработано.
    /// </summary>
    /// <param name="messageId">Бизнес-идентификатор сообщения (например, BookingId).</param>
    /// <param name="ct">Токен отмены.</param>
    Task<bool> ExistsAsync(Guid messageId, CancellationToken ct = default);

    /// <summary>
    /// Добавляет новую запись об обработанном сообщении (без сохранения в БД).
    /// Сохранение выполняется через <see cref="IEventRepository.SaveChangesAsync"/>,
    /// совместно использующий тот же DbContext.
    /// </summary>
    /// <param name="messageId">Бизнес-идентификатор обработанного сообщения.</param>
    /// <param name="messageType">Тип сообщения.</param>
    void Add(Guid messageId, string messageType);
}
