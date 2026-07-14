namespace BookingService.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое, когда в событии не осталось доступных мест.
/// </summary>
public class NoAvailableSeatsException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр исключения с сообщением по умолчанию.
    /// </summary>
    public NoAvailableSeatsException() : base("No available seats for this event.")
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с указанным сообщением.
    /// </summary>
    /// <param name="message">Текст сообщения об ошибке.</param>
    public NoAvailableSeatsException(string message) : base(message)
    {
    }
}
