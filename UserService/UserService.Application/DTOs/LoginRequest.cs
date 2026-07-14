namespace UserService.Application.DTOs;

/// <summary>
/// Запрос на аутентификацию пользователя.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// Логин пользователя.
    /// </summary>
    public required string Login { get; init; }

    /// <summary>
    /// Пароль в открытом виде.
    /// </summary>
    public required string Password { get; init; }
}
