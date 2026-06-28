namespace EventService.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое, когда в событии не осталось доступных мест.
/// </summary>
public class NoAvailableSeatsException : Exception
{
    /// <summary>
    /// Идентификатор события, связанного с ошибкой.
    /// </summary>
    public Guid? Id { get; }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с сообщением по умолчанию.
    /// </summary>
    public NoAvailableSeatsException() : base(message: "No available seats for this event")
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором сущности и сообщением об ошибке.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="message">Текст сообщения об ошибке.</param>
    public NoAvailableSeatsException(Guid? id, string message) : base(message) =>
        Id = id;
}
