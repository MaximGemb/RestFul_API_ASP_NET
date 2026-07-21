using Microsoft.EntityFrameworkCore;
using EventService.Application.Interfaces;
using EventService.Domain.Entities;

namespace EventService.Infrastructure.DataAccess.Repositories;

/// <summary>
/// Репозиторий для работы с событиями через <see cref="EventsDbContext"/>.
/// </summary>
public class EventRepository : IEventRepository
{
    private readonly EventsDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="EventRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public EventRepository(EventsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<(List<Event> Items, int TotalCount)> GetPagedAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));

        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public Task<Event?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);

    /// <inheritdoc />
    public Task<List<Event>> GetTopByPopularityAsync(int count, CancellationToken ct = default) =>
        _context.Events
            .OrderByDescending(e => (double)(e.TotalSeats - e.AvailableSeats) / e.TotalSeats)
            .Take(count)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task AddAsync(Event @event, CancellationToken ct = default) =>
        await _context.Events.AddAsync(@event, ct);

    /// <inheritdoc />
    public void Remove(Event @event) =>
        _context.Events.Remove(@event);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
