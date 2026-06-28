namespace BookingService.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при превышении лимита активных бронирований пользователя.
/// </summary>
public class ActiveBookingsLimitExceededException : Exception
{
    /// <summary>
    /// Идентификатор пользователя, превысившего лимит.
    /// </summary>
    public Guid? UserId { get; }

    /// <summary>
    /// Максимально допустимое количество активных бронирований.
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором пользователя и лимитом.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="limit">Лимит активных бронирований.</param>
    public ActiveBookingsLimitExceededException(Guid? userId, int limit)
        : base($"User {userId} has reached the active bookings limit of {limit}.")
    {
        UserId = userId;
        Limit = limit;
    }
}
