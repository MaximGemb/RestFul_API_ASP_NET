using System.Text.Json;
using EventService.Application.Common;
using EventService.Application.DTOs;
using EventService.Application.Interfaces;
using EventService.Domain.Entities;
using EventService.Domain.Exceptions;

namespace EventService.Application.Services;

/// <summary>
/// Сервис для работы с событиями через репозиторий.
/// Реализует паттерн Cache-Aside для чтения данных: сначала проверяется кеш,
/// при отсутствии данных выполняется запрос к базе, после чего результат сохраняется в кеш.
/// </summary>
public class EventService : IEventService
{
    private static readonly TimeSpan EventCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TopEventsCacheTtl = TimeSpan.FromMinutes(2);
    private const int TopEventsCount = 10;

    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cacheService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="EventService"/>.
    /// </summary>
    /// <param name="eventRepository">Репозиторий событий.</param>
    /// <param name="cacheService">Сервис кеширования.</param>
    public EventService(IEventRepository eventRepository, ICacheService cacheService)
    {
        _eventRepository = eventRepository;
        _cacheService = cacheService;
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
        var cacheKey = CacheKeys.Event(id);

        var cached = await _cacheService.GetAsync(cacheKey, ct);
        if (cached is not null)
        {
            var cachedInfo = JsonSerializer.Deserialize<EventInfo>(cached);
            if (cachedInfo is not null)
                return cachedInfo;
        }

        var @event = await _eventRepository.FindByIdAsync(id, ct)
                     ?? throw new NotFoundException(id, $"Can't get event with id {id}. Event not found");

        var info = ToInfo(@event);
        await _cacheService.SetAsync(cacheKey, JsonSerializer.Serialize(info), EventCacheTtl, ct);
        return info;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventInfo>> GetTopEventsAsync(CancellationToken ct = default)
    {
        var cached = await _cacheService.GetAsync(CacheKeys.TopEvents, ct);
        if (cached is not null)
        {
            var cachedInfos = JsonSerializer.Deserialize<List<EventInfo>>(cached);
            if (cachedInfos is not null)
                return cachedInfos;
        }

        var events = await _eventRepository.GetTopByPopularityAsync(TopEventsCount, ct);
        var infos = events.Select(ToInfo).ToList();

        await _cacheService.SetAsync(CacheKeys.TopEvents, JsonSerializer.Serialize(infos), TopEventsCacheTtl, ct);
        return infos;
    }

    /// <inheritdoc />
    public async Task<EventInfo> CreateEventAsync(CreateEvent item, CancellationToken ct = default)
    {
        var @event = Event.Create(item.Title, item.StartAt, item.EndAt, item.TotalSeats, item.Description);

        await _eventRepository.AddAsync(@event, ct);
        await _eventRepository.SaveChangesAsync(ct);
        await _cacheService.RemoveAsync(CacheKeys.TopEvents, ct);
        return ToInfo(@event);
    }

    /// <inheritdoc />
    public async Task<EventInfo> UpdateEventAsync(Guid id, UpdateEvent item, CancellationToken ct = default)
    {
        var @event = await _eventRepository.FindByIdAsync(id, ct)
                     ?? throw new NotFoundException(id, $"Can't update event with id {id}. Event not found");

        @event.Update(item.Title, item.StartAt, item.EndAt, item.Description);

        await _eventRepository.SaveChangesAsync(ct);
        await _cacheService.RemoveAsync(CacheKeys.Event(id), ct);
        return ToInfo(@event);
    }

    /// <inheritdoc />
    public async Task DeleteEventAsync(Guid id, CancellationToken ct = default)
    {
        var @event = await _eventRepository.FindByIdAsync(id, ct)
                     ?? throw new NotFoundException(id, $"Can't delete event with id {id}. Event not found");

        _eventRepository.Remove(@event);
        await _eventRepository.SaveChangesAsync(ct);
        await _cacheService.RemoveAsync(CacheKeys.Event(id), ct);
        await _cacheService.RemoveAsync(CacheKeys.TopEvents, ct);
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
