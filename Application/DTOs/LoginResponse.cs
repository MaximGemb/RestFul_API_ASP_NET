namespace Application.DTOs;

/// <summary>
/// Ответ на успешный вход в систему.
/// </summary>
public sealed record LoginResponse
{
    /// <summary>
    /// Подписанный JWT-токен.
    /// </summary>
    public required string Token { get; init; }
}
