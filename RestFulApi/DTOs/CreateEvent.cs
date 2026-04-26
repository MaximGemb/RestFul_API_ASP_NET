namespace RestFulApi.DTOs;

/// <summary>
/// Данные для создания нового события.
/// </summary>
public sealed record CreateEvent
{
    /// <summary>
    /// Название события.
    /// </summary>
    public string? Title { get; init; }
    /// <summary>
    /// Дата и время начала события.
    /// </summary>
    public DateTime? StartAt { get; init; }
    /// <summary>
    /// Дата и время окончания события.
    /// </summary>
    public DateTime? EndAt { get; init; }
    /// <summary>
    /// Общее количество мест на событии.
    /// </summary>
    public int? TotalSeats { get; init; }
    /// <summary>
    /// Описание события.
    /// </summary>
    public string? Description { get; init; }
}
