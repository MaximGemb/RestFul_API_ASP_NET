using Domain.Entities;

namespace RestFulApi.Interfaces;

/// <summary>
/// Определяет контракт репозитория для работы с бронированиями.
/// </summary>
internal interface IBookingRepository
{
    /// <summary>
    /// Возвращает бронирование по идентификатору или <c>null</c>, если не найдено.
    /// </summary>
    Task<Booking?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Возвращает список идентификаторов бронирований со статусом <see cref="BookingStatus.Pending"/>.
    /// </summary>
    Task<List<Guid>> GetPendingIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Добавляет новое бронирование в контекст.
    /// </summary>
    Task AddAsync(Booking booking, CancellationToken ct = default);

    /// <summary>
    /// Сохраняет все изменения в базе данных.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
