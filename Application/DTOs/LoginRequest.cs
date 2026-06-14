namespace Application.DTOs;

/// <summary>
/// Запрос на вход в систему.
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
