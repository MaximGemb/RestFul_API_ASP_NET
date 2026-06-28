namespace Domain.Entities;

/// <summary>
/// Представляет пользователя системы.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Конструктор по умолчанию для Entity Framework.
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private User()
    {
        Login = null!;
        PasswordHash = null!;
    }

    /// <summary>
    /// Инициализирует новый экземпляр пользователя с заданными параметрами.
    /// </summary>
    /// <param name="id">Уникальный идентификатор пользователя.</param>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="passwordHash">Хеш пароля пользователя.</param>
    /// <param name="role">Роль пользователя.</param>
    private User(Guid id, string login, string passwordHash, Roles role)
    {
        Id = id;
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }

    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public Guid Id { get; private set; }

    /// <summary>
    /// Логин пользователя.
    /// </summary>
    public string Login { get; private set; }

    /// <summary>
    /// Хеш пароля пользователя.
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Роль пользователя в системе.
    /// </summary>
    public Roles Role { get; private set; }

    /// <summary>
    /// Создает нового пользователя.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="passwordHash">Хеш пароля пользователя.</param>
    /// <param name="role">Роль пользователя.</param>
    /// <returns>Новый экземпляр <see cref="User"/>.</returns>
    public static User Create(string login, string passwordHash, Roles role)
    {
        return new User(Guid.NewGuid(), login, passwordHash, role);
    }
}