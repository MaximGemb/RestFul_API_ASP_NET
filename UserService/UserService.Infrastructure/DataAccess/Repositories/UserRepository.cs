using Microsoft.EntityFrameworkCore;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Infrastructure.DataAccess;

namespace UserService.Infrastructure.DataAccess.Repositories;

/// <summary>
/// Репозиторий для работы с пользователями через <see cref="UsersDbContext"/>.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UsersDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UserRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public UserRepository(UsersDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<User?> FindByLoginAsync(string login, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Login == login, ct);

    /// <inheritdoc />
    public Task<bool> ExistsByLoginAsync(string login, CancellationToken ct = default) =>
        _context.Users.AnyAsync(u => u.Login == login, ct);

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _context.Users.AddAsync(user, ct);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
