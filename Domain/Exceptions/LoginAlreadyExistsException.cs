namespace Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при попытке зарегистрировать уже существующий логин.
/// </summary>
public class LoginAlreadyExistsException : Exception
{
    /// <summary>
    /// Логин, который уже занят.
    /// </summary>
    public string Login { get; }

    /// <summary>
    /// Инициализирует новый экземпляр исключения с указанным логином.
    /// </summary>
    /// <param name="login">Занятый логин.</param>
    public LoginAlreadyExistsException(string login)
        : base($"User with login '{login}' already exists.")
    {
        Login = login;
    }
}
