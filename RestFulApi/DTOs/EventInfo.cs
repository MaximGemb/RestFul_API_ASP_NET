namespace RestFulApi.DTOs;

/// <summary>
/// Представляет информацию о событии, возвращаемую клиенту.
/// </summary>
public sealed record EventInfo
{
    /// <summary>
    /// Уникальный идентификатор события.
    /// </summary>
    public required Guid Id { get; init; }
    /// <summary>
    /// Название события.
    /// </summary>
    public required string Title { get; init; }
    /// <summary>
    /// Дата и время начала события.
    /// </summary>
    public required DateTime StartAt { get; init; }
    /// <summary>
    /// Дата и время окончания события.
    /// </summary>
    public required DateTime EndAt { get; init; }
    /// <summary>
    /// Общее количество мест на событии.
    /// </summary>
    public required int TotalSeats { get; init; }
    /// <summary>
    /// Текущее количество свободных мест.
    /// </summary>
    public required int AvailableSeats { get; init; }
    /// <summary>
    /// Описание события.
    /// </summary>
    public string? Description { get; init; }
}
