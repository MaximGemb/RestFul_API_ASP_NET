using RestFulApi.Models;

namespace RestFulApi.Interfaces;

/// <summary>
/// Определяет контракт сервиса для работы с бронированиями.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создает новую бронь для указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Созданное бронирование.</returns>
    Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Возвращает бронь по идентификатору.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Найденное бронирование.</returns>
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);

    /// <summary>
    /// Получает список бронирований со статусом Pending.
    /// </summary>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Список ожидающих обработки бронирований.</returns>
    Task<IEnumerable<Booking>> GetPendingBookingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Обновляет информацию о бронировании.
    /// </summary>
    /// <param name="booking">Обновленное бронирование.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task UpdateBookingAsync(Booking booking, CancellationToken ct = default);
}
