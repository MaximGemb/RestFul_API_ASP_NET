using BookingService.Application.DTOs;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Контракт сервиса для работы с бронированиями.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создает новую бронь для указанного события от имени пользователя.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="userId">Идентификатор пользователя, создающего бронь.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация о созданном бронировании.</returns>
    Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Отменяет бронь. Администратор может отменить любую бронь; обычный пользователь — только свою.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования.</param>
    /// <param name="userId">Идентификатор пользователя, выполняющего отмену.</param>
    /// <param name="isAdmin">Признак администратора.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация об отменённом бронировании.</returns>
    Task<BookingInfo> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct = default);

    /// <summary>
    /// Возвращает бронь по идентификатору с проверкой прав доступа.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования.</param>
    /// <param name="userId">Идентификатор запрашивающего пользователя.</param>
    /// <param name="isAdmin">Признак администратора.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация о найденном бронировании.</returns>
    Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct = default);
}
