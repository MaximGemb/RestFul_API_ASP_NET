namespace Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при попытке выполнить операцию без необходимых прав.
/// </summary>
public class OperationNotAllowedException : Exception
{
    /// <summary>
    /// Идентификатор пользователя, у которого отсутствуют права.
    /// </summary>
    public Guid? UserId { get; }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    public OperationNotAllowedException(Guid? userId)
        : base($"User {userId} is not allowed to perform this operation.") =>
        UserId = userId;

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором пользователя и сообщением.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="message">Текст сообщения об ошибке.</param>
    public OperationNotAllowedException(Guid? userId, string message) : base(message) =>
        UserId = userId;

    /// <summary>
    /// Инициализирует новый экземпляр исключения с идентификатором пользователя, сообщением и внутренним исключением.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="message">Текст сообщения об ошибке.</param>
    /// <param name="inner">Внутреннее исключение.</param>
    public OperationNotAllowedException(Guid? userId, string message, Exception inner)
        : base(message, inner) =>
        UserId = userId;
}
