using UserService.Domain.Entities;

namespace UserService.Application.Interfaces;

/// <summary>
/// Контракт сервиса регистрации и аутентификации пользователей.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Регистрирует нового пользователя.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="password">Пароль в открытом виде.</param>
    /// <param name="role">Роль пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Идентификатор созданного пользователя.</returns>
    Task<Guid> RegisterAsync(string login, string password, Roles role = Roles.User, CancellationToken ct = default);

    /// <summary>
    /// Выполняет аутентификацию и возвращает JWT-токен.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="password">Пароль в открытом виде.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Подписанный JWT-токен.</returns>
    Task<string> LoginAsync(string login, string password, CancellationToken ct = default);
}
