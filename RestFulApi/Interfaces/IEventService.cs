using RestFulApi.DTOs;
using RestFulApi.Models;

namespace RestFulApi.Interfaces;

/// <summary>
/// Интерфейс сервиса для работы с событиями
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Получить все события
    /// </summary>
    Task<List<Event>> GetAll();

    /// <summary>
    /// Получить событие по Id
    /// </summary>
    Task<Event?> GetById(Guid id);

    /// <summary>
    /// Создать событие
    /// </summary>
    Task<Event> Create(EventDto item);

    /// <summary>
    /// Обновить событие
    /// </summary>
    Task<Event?> Update(Guid id, EventDto item);

    /// <summary>
    /// Удалить событие
    /// </summary>
    Task<bool> Delete(Guid id);
}