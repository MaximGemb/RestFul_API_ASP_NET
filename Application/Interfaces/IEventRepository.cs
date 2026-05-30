using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Определяет контракт репозитория для работы с событиями.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Возвращает страницу событий с учётом фильтрации.
    /// </summary>
    Task<(List<Event> Items, int TotalCount)> GetPagedAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Возвращает событие по идентификатору или <c>null</c>, если не найдено.
    /// </summary>
    Task<Event?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Добавляет новое событие в контекст.
    /// </summary>
    Task AddAsync(Event @event, CancellationToken ct = default);

    /// <summary>
    /// Помечает событие на удаление.
    /// </summary>
    void Remove(Event @event);

    /// <summary>
    /// Сохраняет все изменения в базе данных.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
