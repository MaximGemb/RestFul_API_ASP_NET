using RestFulApi.DTOs;
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
    public Task<Event?> GetById(Guid id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
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
    public Task<Event?> Update(Guid id, EventDto item)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        if (ev == null)
            return Task.FromResult<Event?>(null);

        ev.Title = item.Title;
        ev.Description = item.Description;
        ev.StartAt = item.StartAt;
        ev.EndAt = item.EndAt;

        return Task.FromResult<Event?>(ev);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    // ReSharper disable once HeapView.ClosureAllocation
    public Task<bool> Delete(Guid id)
    {
        var ev = _events.FirstOrDefault(e => e.Id == id);
        if (ev is null)
            return Task.FromResult(false);
        _events.Remove(ev);
        return Task.FromResult(true);
    }
}