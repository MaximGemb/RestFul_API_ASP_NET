namespace EventService.Application.DTOs;

/// <summary>
/// Запрос на обновление существующего события.
/// </summary>
public sealed record UpdateEvent
{
    /// <summary>
    /// Новое название события.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Новая дата и время начала.
    /// </summary>
    public required DateTime StartAt { get; init; }

    /// <summary>
    /// Новая дата и время окончания.
    /// </summary>
    public required DateTime EndAt { get; init; }

    /// <summary>
    /// Новое описание события.
    /// </summary>
    public string? Description { get; init; }
}
