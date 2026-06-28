using EventService.Application.DTOs;
using EventService.Domain.Entities;

namespace EventService.Application.Interfaces;

/// <summary>
/// Контракт сервиса для работы с событиями.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Возвращает список событий с учетом фильтрации и пагинации.
    /// </summary>
    Task<PaginatedResult<EventInfo>> GetAllEventsAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Возвращает событие по идентификатору.
    /// </summary>
    Task<EventInfo> GetEventByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Возвращает сущность события по идентификатору (для внутреннего использования).
    /// </summary>
    Task<Event> GetEventEntityByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Создает новое событие.
    /// </summary>
    Task<EventInfo> CreateEventAsync(CreateEvent item, CancellationToken ct = default);

    /// <summary>
    /// Обновляет существующее событие.
    /// </summary>
    Task<EventInfo> UpdateEventAsync(Guid id, UpdateEvent item, CancellationToken ct = default);

    /// <summary>
    /// Удаляет событие по идентификатору.
    /// </summary>
    Task DeleteEventAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Резервирует одно место для события. Используется BookingService.
    /// </summary>
    Task ReserveSeatAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Освобождает одно место для события. Используется BookingService.
    /// </summary>
    Task ReleaseSeatAsync(Guid eventId, CancellationToken ct = default);
}
