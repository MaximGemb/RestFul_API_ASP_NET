using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;

namespace EventService.Application.Services;

/// <summary>
/// Сервис для работы с событиями через репозиторий.
/// </summary>
public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="EventService"/>.
    /// </summary>
    /// <param name="eventRepository">Репозиторий событий.</param>
    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<EventInfo>> GetAllEventsAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _eventRepository.GetPagedAsync(title, from, to, page, pageSize, ct);

        return new PaginatedResult<EventInfo>
        {
            TotalCount = totalCount,
            Items = items.Select(ToInfo).ToArray(),
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken ct = default)
    {
        var @event = await _eventRepository.FindByIdAsync(id, ct)
                     ?? throw new NotFoundException(id, $"Can't get event with id {id}. Event not found");

        return ToInfo(@event);
    }

    /// <inheritdoc />
    public async Task<EventInfo> CreateEventAsync(CreateEvent item, CancellationToken ct = default)
    {
        var @event = Event.Create(item.Title, item.StartAt, item.EndAt, item.TotalSeats, item.Description);

        await _eventRepository.AddAsync(@event, ct);
        await _eventRepository.SaveChangesAsync(ct);
        return ToInfo(@event);
    }

    /// <inheritdoc />
    public async Task<EventInfo> UpdateEventAsync(Guid id, UpdateEvent item, CancellationToken ct = default)
    {
        var @event = await _eventRepository.FindByIdAsync(id, ct)
                     ?? throw new NotFoundException(id, $"Can't update event with id {id}. Event not found");

        @event.Update(item.Title, item.StartAt, item.EndAt, item.Description);

        await _eventRepository.SaveChangesAsync(ct);
        return ToInfo(@event);
    }

    /// <inheritdoc />
    public async Task DeleteEventAsync(Guid id, CancellationToken ct = default)
    {
        var @event = await _eventRepository.FindByIdAsync(id, ct)
                     ?? throw new NotFoundException(id, $"Can't delete event with id {id}. Event not found");

        _eventRepository.Remove(@event);
        await _eventRepository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Маппинг сущности Event в DTO EventInfo.
    /// </summary>
    internal static EventInfo ToInfo(Event @event) => new()
    {
        Id = @event.Id,
        Title = @event.Title,
        StartAt = @event.StartAt!.Value,
        EndAt = @event.EndAt!.Value,
        TotalSeats = @event.TotalSeats,
        AvailableSeats = @event.AvailableSeats,
        Description = @event.Description
    };
}
