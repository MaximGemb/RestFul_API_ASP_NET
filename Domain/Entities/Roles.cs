namespace Domain.Entities;

/// <summary>
/// Роли пользователей в системе.
/// </summary>
public enum Roles
{
    /// <summary>
    /// Администратор системы с полными правами доступа.
    /// </summary>
    Admin,

    /// <summary>
    /// Обычный пользователь с ограниченными правами доступа.
    /// </summary>
    User
}