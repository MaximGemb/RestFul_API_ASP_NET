using BookingService.Domain.Entities;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Определяет контракт репозитория для работы с Outbox-сообщениями.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Добавляет новое сообщение в Outbox (без сохранения в БД).
    /// Сохранение выполняется через <see cref="IBookingRepository.SaveChangesAsync"/>,
    /// совместно использующий тот же DbContext.
    /// </summary>
    void Add(OutboxMessage message);

    /// <summary>
    /// Возвращает все необработанные сообщения (<see cref="OutboxMessage.ProcessedAt"/> == null),
    /// упорядоченные по времени создания.
    /// </summary>
    Task<List<OutboxMessage>> GetUnprocessedAsync(CancellationToken ct = default);

    /// <summary>
    /// Сохраняет все изменения в базе данных.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
