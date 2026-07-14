using Microsoft.EntityFrameworkCore;
using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;

namespace BookingService.Infrastructure.DataAccess.Repositories;

/// <summary>
/// Репозиторий для работы с бронированиями через <see cref="BookingsDbContext"/>.
/// </summary>
public class BookingRepository : IBookingRepository
{
    private readonly BookingsDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public BookingRepository(BookingsDbContext context)
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
    public Task<int> CountActiveByUserAsync(Guid userId, CancellationToken ct = default) =>
        _context.Bookings
            .Where(b => b.UserId == userId &&
                        (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .CountAsync(ct);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
