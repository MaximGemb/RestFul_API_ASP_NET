namespace Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при попытке забронировать уже начавшееся событие.
/// </summary>
public class EventAlreadyStartedException : Exception
{
    /// <summary>
    /// Идентификатор события, связанного с ошибкой.
    /// </summary>
    public Guid? EventId { get; }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором события.
    /// </summary>
    /// <param name="eventId">Идентификатор уже начавшегося события.</param>
    public EventAlreadyStartedException(Guid? eventId)
        : base($"Cannot book event {eventId}: the event has already started.") =>
        EventId = eventId;

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором события и сообщением.
    /// </summary>
    /// <param name="eventId">Идентификатор уже начавшегося события.</param>
    /// <param name="message">Текст сообщения об ошибке.</param>
    public EventAlreadyStartedException(Guid? eventId, string message) : base(message) =>
        EventId = eventId;

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором события, сообщением и внутренним исключением.
    /// </summary>
    /// <param name="eventId">Идентификатор уже начавшегося события.</param>
    /// <param name="message">Текст сообщения об ошибке.</param>
    /// <param name="inner">Внутреннее исключение.</param>
    public EventAlreadyStartedException(Guid? eventId, string message, Exception inner)
        : base(message, inner) =>
        EventId = eventId;
}
