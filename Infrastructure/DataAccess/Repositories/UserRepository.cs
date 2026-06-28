using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccess.Repositories;

/// <summary>
/// Репозиторий для работы с пользователями через <see cref="AppDbContext"/>.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UserRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public UserRepository(AppDbContext context)
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
