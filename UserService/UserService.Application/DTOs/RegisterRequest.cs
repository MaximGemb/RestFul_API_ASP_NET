using UserService.Domain.Entities;

namespace UserService.Application.DTOs;

/// <summary>
/// Запрос на регистрацию нового пользователя.
/// </summary>
public sealed record RegisterRequest
{
    /// <summary>
    /// Логин нового пользователя.
    /// </summary>
    public required string Login { get; init; }

    /// <summary>
    /// Пароль в открытом виде.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Роль пользователя. По умолчанию — <see cref="Roles.User"/>.
    /// </summary>
    public Roles Role { get; init; } = Roles.User;
}
