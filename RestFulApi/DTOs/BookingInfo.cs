using RestFulApi.Models;

namespace RestFulApi.DTOs;

/// <summary>
/// Представляет информацию о бронировании, возвращаемую клиенту.
/// </summary>
public sealed record BookingInfo
{
    /// <summary>
    /// Уникальный идентификатор брони.
    /// </summary>
    public required Guid Id { get; init; }
    /// <summary>
    /// Идентификатор события, к которому относится бронь.
    /// </summary>
    public required Guid EventId { get; init; }
    /// <summary>
    /// Текущий статус брони.
    /// </summary>
    public required BookingStatus Status { get; init; }
    /// <summary>
    /// Дата и время создания брони.
    /// </summary>
    public required DateTime CreatedAt { get; init; }
    /// <summary>
    /// Дата и время обработки брони. <see langword="null"/>, если бронь ещё не обработана.
    /// </summary>
    public DateTime? ProcessedAt { get; init; }
}
