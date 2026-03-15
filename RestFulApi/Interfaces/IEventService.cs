using RestFulApi.DTOs;
using RestFulApi.Models;

namespace RestFulApi.Interfaces;

/// <summary>
/// Определяет контракт сервиса для работы с событиями.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Возвращает список событий с учетом фильтрации и пагинации.
    /// </summary>
    /// <param name="title">Фильтр по части названия события.</param>
    /// <param name="from">Минимальная дата начала события.</param>
    /// <param name="to">Максимальная дата окончания события.</param>
    /// <param name="page">Номер страницы, начиная с 1.</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <returns>Результат пагинации со списком найденных событий.</returns>
    Task<PaginatedResult<Event>> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <returns>Найденное событие.</returns>
    Task<Event> GetById(Guid id);

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    /// <param name="item">Данные создаваемого события.</param>
    /// <returns>Созданное событие.</returns>
    Task<Event> Create(EventDto item);

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    /// <param name="id">Идентификатор обновляемого события.</param>
    /// <param name="item">Новые данные события.</param>
    /// <returns>Обновленное событие.</returns>
    Task<Event> Update(Guid id, EventDto item);

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого события.</param>
    /// <returns>Задача, представляющая завершение операции удаления.</returns>
    Task Delete(Guid id);
}