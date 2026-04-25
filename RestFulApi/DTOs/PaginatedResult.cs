namespace RestFulApi.DTOs;

/// <summary>
/// Представляет структуру ответа для возврата списка событий с поддержкой пагинации.
/// </summary>
/// <typeparam name="T">Тип элементов в результирующей коллекции.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Общее количество элементов, соответствующих критериям поиска, без учета пагинации.
    /// </summary>

    public int TotalCount { get; init; }

    /// <summary>
    /// Коллекция элементов для текущей страницы.
    /// </summary>
    public List<T> Items { get; init; } = [];

    /// <summary>
    /// Номер текущей страницы (начиная с 1).
    /// </summary>
    public int CurrentPageNumber { get; init; }

    /// <summary>
    /// Количество элементов, на текущей странице.
    /// </summary>
    public int CurrentPageItemsCount { get; init; }
}