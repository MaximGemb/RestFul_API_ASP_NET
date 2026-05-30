using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace RestFulApi.DataAccess.Repositories;

/// <summary>
/// Репозиторий для работы с бронированиями через <see cref="AppDbContext"/>.
/// </summary>
public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<Booking?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);

    /// <inheritdoc />
    public Task<List<Guid>> GetPendingIdsAsync(CancellationToken ct = default) =>
        _context.Bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task AddAsync(Booking booking, CancellationToken ct = default) =>
        await _context.Bookings.AddAsync(booking, ct);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
