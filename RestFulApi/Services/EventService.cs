using RestFulApi.DTOs;
using RestFulApi.Exceptions;
using RestFulApi.Interfaces;
using RestFulApi.Models;

namespace RestFulApi.Services;

/// <summary>
/// Сервис для работы с событиями через репозиторий.
/// </summary>
internal class EventService : IEventService
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

    /// <summary>
    /// Возвращает список событий с учетом фильтрации и пагинации.
    /// </summary>
    /// <param name="title">Фильтр по части названия события.</param>
    /// <param name="from">Минимальная дата начала события.</param>
    /// <param name="to">Максимальная дата окончания события.</param>
    /// <param name="page">Номер страницы, начиная с 1.</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат пагинации со списком найденных событий.</returns>
    public async Task<PaginatedResult<EventInfo>> GetAllEventsAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10, CancellationToken ct = default)
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

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация о найденном событии.</returns>
    public async Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken ct = default)
    {
        var @event = await _eventRepository.FindByIdAsync(id, ct)
                     ?? throw new NotFoundException(id, $"Can't get event with id {id}. Event not found");

        return ToInfo(@event);
    }

    /// <summary>
    /// Возвращает сущность события по идентификатору (для внутреннего использования).
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Сущность события.</returns>
    public async Task<Event> GetEventEntityByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _eventRepository.FindByIdAsync(id, ct)
               ?? throw new NotFoundException(id, $"Can't get event with id {id}. Event not found");
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    /// <param name="item">Данные создаваемого события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация о созданном событии.</returns>
    public async Task<EventInfo> CreateEventAsync(CreateEvent item, CancellationToken ct = default)
    {
        var @event = Event.Create(item.Title, item.StartAt, item.EndAt, item.TotalSeats,
            item.Description);

        await _eventRepository.AddAsync(@event, ct);
        await _eventRepository.SaveChangesAsync(ct);
        return ToInfo(@event);
    }

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    /// <param name="id">Идентификатор обновляемого события.</param>
    /// <param name="item">Новые данные события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация об обновленном событии.</returns>
    public async Task<EventInfo> UpdateEventAsync(Guid id, UpdateEvent item, CancellationToken ct = default)
    {
        var @event = await _eventRepository.FindByIdAsync(id, ct)
                     ?? throw new NotFoundException(id, $"Can't update event with id {id}. Event not found");

        @event.Update(item.Title, item.StartAt, item.EndAt, item.Description);

        await _eventRepository.SaveChangesAsync(ct);
        return ToInfo(@event);
    }

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Задача, представляющая завершение операции удаления.</returns>
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