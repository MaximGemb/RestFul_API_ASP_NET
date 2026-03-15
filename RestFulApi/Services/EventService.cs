using RestFulApi.DTOs;
using RestFulApi.Exceptions;
using RestFulApi.Interfaces;
using RestFulApi.Models;

namespace RestFulApi.Services;

/// <summary>
/// Сервис для работы с событиями в памяти.
/// </summary>
public class EventService : IEventService
{
    private readonly List<Event> _events = [];

    /// <summary>
    /// Возвращает список событий с учетом фильтрации и пагинации.
    /// </summary>
    /// <param name="title">Фильтр по части названия события.</param>
    /// <param name="from">Минимальная дата начала события.</param>
    /// <param name="to">Максимальная дата окончания события.</param>
    /// <param name="page">Номер страницы, начиная с 1.</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <returns>Результат пагинации со списком найденных событий.</returns>
    public Task<PaginatedResult<Event>> GetAll(string? title = null, DateTime? from = null, DateTime? to = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _events.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.EndAt <= to.Value);

        var filteredList = query.ToList();

        return Task.FromResult(GetEventsWithPagination(filteredList, page, pageSize));
    }

    /// <summary>
    /// Выполняет пагинацию коллекции событий.
    /// </summary>
    /// <param name="filteredEvents">Исходная коллекция событий.</param>
    /// <param name="pageNumber">Номер запрашиваемой страницы (начиная с 1).</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <returns>Объект <see cref="PaginatedResult{Event}"/> с данными о странице и метаданными.</returns>
    private static PaginatedResult<Event> GetEventsWithPagination(
        List<Event> filteredEvents,
        int pageNumber,
        int pageSize)
    {
        var totalCount = filteredEvents.Count;

        var items = filteredEvents
            .OrderByDescending(c => c.StartAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Items = items,
            CurrentPageNumber = pageNumber,
            CurrentPageItemsCount = items.Count
        };
    }

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <returns>Найденное событие.</returns>
    // ReSharper disable once HeapView.ClosureAllocation
    public Task<Event> GetById(Guid id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id)
                 ?? throw new NotFoundException(id, $"Can't get event with id {id}. Event not found ");

        return Task.FromResult(ev);
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    /// <param name="item">Данные создаваемого события.</param>
    /// <returns>Созданное событие.</returns>
    public Task<Event> Create(EventDto item)
    {
        var newEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = item.Title,
            Description = item.Description,
            StartAt = item.StartAt,
            EndAt = item.EndAt
        };
        _events.Add(newEvent);
        return Task.FromResult(newEvent);
    }

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    /// <param name="id">Идентификатор обновляемого события.</param>
    /// <param name="item">Новые данные события.</param>
    /// <returns>Обновленное событие.</returns>
    // ReSharper disable once HeapView.ClosureAllocation
    public Task<Event> Update(Guid id, EventDto item)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id)
                 ?? throw new NotFoundException(id, $"Can't update event with id {id}. Event not found ");

        ev.Title = item.Title;
        ev.Description = item.Description;
        ev.StartAt = item.StartAt;
        ev.EndAt = item.EndAt;

        return Task.FromResult(ev);
    }

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого события.</param>
    /// <returns>Задача, представляющая завершение операции удаления.</returns>
    // ReSharper disable once HeapView.ClosureAllocation
    public Task Delete(Guid id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id)
                 ?? throw new NotFoundException(id, $"Can't delete event with id {id}. Event not found ");

        _events.Remove(ev);
        return Task.CompletedTask;
    }
}