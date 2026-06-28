namespace UserService.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при неверных учётных данных пользователя.
/// </summary>
public class InvalidCredentialsException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр исключения с сообщением по умолчанию.
    /// </summary>
    public InvalidCredentialsException()
        : base("Invalid login or password.")
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с указанным сообщением.
    /// </summary>
    /// <param name="message">Текст сообщения об ошибке.</param>
    public InvalidCredentialsException(string message) : base(message)
    {
    }
}
