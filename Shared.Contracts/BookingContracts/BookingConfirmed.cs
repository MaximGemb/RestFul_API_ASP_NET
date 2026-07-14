namespace Shared.Contracts.BookingContracts;

/// <summary>
/// Контракт события, публикуемого BookingService в топик <see cref="BookingTopics.BOOKING_CONFIRMED"/>
/// после успешного подтверждения брони.
/// </summary>
/// <param name="BookingId">Уникальный идентификатор брони.</param>
/// <param name="EventId">Идентификатор события, к которому относится бронь.</param>
/// <param name="UserId">Идентификатор пользователя, создавшего бронь.</param>
/// <param name="SeatsCount">Количество забронированных мест.</param>
/// <param name="ConfirmedAt">Момент подтверждения брони (UTC).</param>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime ConfirmedAt);
