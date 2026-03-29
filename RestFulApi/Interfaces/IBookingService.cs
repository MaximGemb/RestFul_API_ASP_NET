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
}
