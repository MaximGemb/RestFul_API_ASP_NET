using EventService.Application.Interfaces;
using EventService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.DataAccess.Repositories;

/// <summary>
/// Репозиторий для работы с Inbox-сообщениями через <see cref="EventsDbContext"/>.
/// Использует тот же экземпляр контекста, что и <see cref="EventRepository"/>,
/// что обеспечивает атомарность проверки + обновления состояния события + записи в Inbox
/// в одной транзакции.
/// </summary>
public sealed class InboxRepository : IInboxRepository
{
    private readonly EventsDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="InboxRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public InboxRepository(EventsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid messageId, CancellationToken ct = default) =>
        _context.InboxMessages.AnyAsync(m => m.MessageId == messageId, ct);

    /// <inheritdoc />
    public void Add(Guid messageId, string messageType) =>
        _context.InboxMessages.Add(InboxMessage.Create(messageId, messageType));
}
