namespace EventService.Application.DTOs;

/// <summary>
/// Запрос на создание нового события.
/// </summary>
public sealed record CreateEvent
{
    /// <summary>
    /// Название события.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Дата и время начала.
    /// </summary>
    public required DateTime StartAt { get; init; }

    /// <summary>
    /// Дата и время окончания.
    /// </summary>
    public required DateTime EndAt { get; init; }

    /// <summary>
    /// Общее количество мест.
    /// </summary>
    public required int TotalSeats { get; init; }

    /// <summary>
    /// Описание события.
    /// </summary>
    public string? Description { get; init; }
}
