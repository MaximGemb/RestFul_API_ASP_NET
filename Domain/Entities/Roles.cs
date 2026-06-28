namespace Domain.Entities;

/// <summary>
/// Роли пользователей в системе.
/// </summary>
public enum Roles
{
    /// <summary>
    /// Обычный пользователь с ограниченными правами доступа.
    /// </summary>
    User,
    
    /// <summary>
    /// Администратор системы с полными правами доступа.
    /// </summary>
    Admin
    
}