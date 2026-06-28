using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.DataAccess.Repositories;

/// <summary>
/// Репозиторий для работы с Outbox-сообщениями через <see cref="BookingsDbContext"/>.
/// Использует тот же экземпляр контекста, что и <see cref="BookingRepository"/>,
/// что обеспечивает атомарность записи брони и Outbox-сообщения в одной транзакции.
/// </summary>
public sealed class OutboxRepository : IOutboxRepository
{
    private readonly BookingsDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="OutboxRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public OutboxRepository(BookingsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public void Add(OutboxMessage message) =>
        _context.OutboxMessages.Add(message);

    /// <inheritdoc />
    public Task<List<OutboxMessage>> GetUnprocessedAsync(CancellationToken ct = default) =>
        _context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
