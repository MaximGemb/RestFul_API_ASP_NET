using UserService.Domain.Entities;

namespace UserService.Application.Interfaces;

/// <summary>
/// Определяет контракт репозитория для работы с пользователями.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Возвращает пользователя по логину или <c>null</c>, если не найден.
    /// </summary>
    Task<User?> FindByLoginAsync(string login, CancellationToken ct = default);

    /// <summary>
    /// Проверяет, существует ли пользователь с указанным логином.
    /// </summary>
    Task<bool> ExistsByLoginAsync(string login, CancellationToken ct = default);

    /// <summary>
    /// Добавляет нового пользователя в контекст.
    /// </summary>
    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Сохраняет все изменения в базе данных.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
