using UserService.Domain.Entities;

namespace UserService.Application.Interfaces;

/// <summary>
/// Сервис генерации подписанного JWT-токена.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Формирует подписанный JWT-токен по данным пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="role">Роль пользователя.</param>
    /// <returns>Строка с JWT-токеном.</returns>
    string GenerateToken(Guid userId, string login, Roles role);
}
