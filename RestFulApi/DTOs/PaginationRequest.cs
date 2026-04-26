namespace RestFulApi.DTOs;

/// <summary>
/// Параметры запроса для пагинации.
/// </summary>
public sealed record PaginationRequest
{
    /// <summary>
    /// Номер страницы, начиная с 1.
    /// </summary>
    public int Page { get; init; } = 1;
    /// <summary>
    /// Количество элементов на странице.
    /// </summary>
    public int PageSize { get; init; } = 10;
}
