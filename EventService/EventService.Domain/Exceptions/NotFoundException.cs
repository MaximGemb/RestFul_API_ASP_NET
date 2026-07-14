namespace EventService.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое когда запрашиваемый ресурс не найден.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Идентификатор сущности, связанной с ошибкой.
    /// </summary>
    public Guid? Id { get; }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с сообщением по умолчанию.
    /// </summary>
    public NotFoundException() : base(message: "Resource not found.")
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором сущности и сообщением об ошибке.
    /// </summary>
    /// <param name="id">Идентификатор сущности.</param>
    /// <param name="message">Текст сообщения об ошибке.</param>
    public NotFoundException(Guid? id, string message) : base(message) =>
        Id = id;
}
