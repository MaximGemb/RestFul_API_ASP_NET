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
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Результат пагинации со списком найденных событий.</returns>
    Task<PaginatedResult<Event>> GetAllAsync(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1,
        int pageSize = 10, CancellationToken ct = default);

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Найденное событие.</returns>
    Task<Event> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    /// <param name="item">Данные создаваемого события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Созданное событие.</returns>
    Task<Event> CreateAsync(EventDto item, CancellationToken ct = default);

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    /// <param name="id">Идентификатор обновляемого события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <param name="item">Новые данные события.</param>
    /// <returns>Обновленное событие.</returns>
    Task<Event> UpdateAsync(Guid id, EventDto item, CancellationToken ct = default);

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Задача, представляющая завершение операции удаления.</returns>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}