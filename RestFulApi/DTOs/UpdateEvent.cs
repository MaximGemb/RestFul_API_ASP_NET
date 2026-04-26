namespace RestFulApi.DTOs;

/// <summary>
/// Данные для обновления существующего события.
/// </summary>
public sealed record UpdateEvent
{
    /// <summary>
    /// Новое название события.
    /// </summary>
    public string? Title { get; init; }
    /// <summary>
    /// Новая дата и время начала события.
    /// </summary>
    public DateTime? StartAt { get; init; }
    /// <summary>
    /// Новая дата и время окончания события.
    /// </summary>
    public DateTime? EndAt { get; init; }
    /// <summary>
    /// Новое описание события.
    /// </summary>
    public string? Description { get; init; }
}
