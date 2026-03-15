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
    /// 
    /// </summary>
    /// <returns></returns>
    public Task<List<Event>> GetAll() =>
        Task.FromResult(_events.ToList());

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    // ReSharper disable once HeapView.ClosureAllocation
    public Task<Event> GetById(Guid id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id)
                 ?? throw new NotFoundException(id, $"Can't get event with id {id}. Event not found ");
        return Task.FromResult(ev);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="item"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    // ReSharper disable once HeapView.ClosureAllocation
    public Task Delete(Guid id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id)
                 ?? throw new NotFoundException(id, $"Can't delete event with id {id}. Event not found ");
        _events.Remove(ev);
        return Task.CompletedTask;
    }
}