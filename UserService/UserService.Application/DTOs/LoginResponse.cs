namespace UserService.Application.DTOs;

/// <summary>
/// Ответ на успешную аутентификацию пользователя.
/// </summary>
public sealed record LoginResponse
{
    /// <summary>
    /// Подписанный JWT-токен.
    /// </summary>
    public required string Token { get; init; }
}
