namespace EventService.Application.DTOs;

/// <summary>
/// Пагинированный результат запроса.
/// </summary>
/// <typeparam name="T">Тип элементов в результате.</typeparam>
public sealed record PaginatedResult<T>
{
    /// <summary>
    /// Список элементов на текущей странице.
    /// </summary>
    public required T[] Items { get; init; }

    /// <summary>
    /// Общее количество элементов (по всем страницам).
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Номер текущей страницы (начиная с 1).
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Количество элементов на странице.
    /// </summary>
    public required int PageSize { get; init; }
}
