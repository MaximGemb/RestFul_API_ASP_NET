namespace RestFulApi.DTOs;

/// <summary>
/// Представляет структуру ответа для возврата списка элементов с поддержкой пагинации.
/// </summary>
/// <typeparam name="T">Тип элементов в результирующей коллекции.</typeparam>
public sealed record PaginatedResult<T>
{
    /// <summary>
    /// Коллекция элементов для текущей страницы.
    /// </summary>
    public required T[] Items { get; init; }

    /// <summary>
    /// Общее количество элементов, соответствующих критериям поиска, без учета пагинации.
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

    /// <summary>
    /// Общее количество страниц.
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}