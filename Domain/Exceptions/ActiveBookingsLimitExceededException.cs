namespace Domain.Exceptions;

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

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором пользователя, лимитом и сообщением.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="limit">Лимит активных бронирований.</param>
    /// <param name="message">Текст сообщения об ошибке.</param>
    public ActiveBookingsLimitExceededException(Guid? userId, int limit, string message)
        : base(message)
    {
        UserId = userId;
        Limit = limit;
    }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором пользователя, лимитом, сообщением и внутренним исключением.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="limit">Лимит активных бронирований.</param>
    /// <param name="message">Текст сообщения об ошибке.</param>
    /// <param name="inner">Внутреннее исключение.</param>
    public ActiveBookingsLimitExceededException(Guid? userId, int limit, string message, Exception inner)
        : base(message, inner)
    {
        UserId = userId;
        Limit = limit;
    }
}
