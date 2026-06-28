namespace UserService.Application.Interfaces;

/// <summary>
/// Компонент хеширования паролей и проверки соответствия.
/// </summary>
public interface IPasswordHasherService
{
    /// <summary>
    /// Вычисляет SHA-256 хеш строки пароля.
    /// </summary>
    /// <param name="password">Исходный пароль.</param>
    /// <returns>Хеш в виде hex-строки верхнего регистра.</returns>
    string Hash(string password);

    /// <summary>
    /// Проверяет соответствие пароля заданному хешу.
    /// </summary>
    /// <param name="password">Проверяемый пароль.</param>
    /// <param name="hash">Ожидаемый хеш.</param>
    /// <returns><c>true</c>, если хеш пароля совпадает с <paramref name="hash"/>.</returns>
    bool Verify(string password, string hash);
}
