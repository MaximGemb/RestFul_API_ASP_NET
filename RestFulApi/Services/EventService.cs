using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Guid, Event> _events = [];

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
    public Task<PaginatedResult<Event>> GetAllAsync(string? title = null, DateTime? from = null, DateTime? to = null,
        int page = 1,
        int pageSize = 10, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var query = _events.Values.AsEnumerable();

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
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Найденное событие.</returns>
    public Task<Event> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return _events.TryGetValue(id, out var ev)
            ? Task.FromResult(ev)
            : throw new NotFoundException(id, $"Can't get event with id {id}. Event not found");
    }

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    /// <param name="item">Данные создаваемого события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Созданное событие.</returns>
    public Task<Event> CreateAsync(EventDto item, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var newEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = item.Title,
            Description = item.Description,
            StartAt = item.StartAt,
            EndAt = item.EndAt,
            TotalSeats = item.TotalSeats.GetValueOrDefault(),
            AvailableSeats = item.TotalSeats.GetValueOrDefault()
        };

        _events.TryAdd(newEvent.Id, newEvent);
        return Task.FromResult(newEvent);
    }

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    /// <param name="id">Идентификатор обновляемого события.</param>
    /// <param name="item">Новые данные события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Обновленное событие.</returns>
    public Task<Event> UpdateAsync(Guid id, EventDto item, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_events.TryGetValue(id, out var ev))
            throw new NotFoundException(id, $"Can't update event with id {id}. Event not found");

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
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Задача, представляющая завершение операции удаления.</returns>
    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return _events.TryRemove(id, out _)
            ? Task.CompletedTask
            : throw new NotFoundException(id, $"Can't delete event with id {id}. Event not found");
    }
}