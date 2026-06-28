using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Определяет контракт сервиса для работы с пользователями.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Регистрирует нового пользователя с хешированным паролем.
    /// </summary>
    /// <param name="login">Логин нового пользователя.</param>
    /// <param name="password">Пароль в открытом виде.</param>
    /// <param name="role">Роль нового пользователя.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Идентификатор созданного пользователя.</returns>
    Task<Guid> RegisterAsync(string login, string password, Roles role = Roles.User, CancellationToken ct = default);

    /// <summary>
    /// Выполняет вход: проверяет логин/пароль и возвращает JWT-токен.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="password">Пароль в открытом виде.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Строка с подписанным JWT-токеном.</returns>
    Task<string> LoginAsync(string login, string password, CancellationToken ct = default);
}
