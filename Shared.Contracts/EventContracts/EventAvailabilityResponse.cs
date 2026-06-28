namespace Shared.Contracts.EventContracts;

/// <summary>
/// Ответ EventService с информацией о доступности события.
/// Используется BookingService при межсервисных запросах.
/// </summary>
public sealed record EventAvailabilityResponse
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
    /// Текущее количество свободных мест.
    /// </summary>
    public required int AvailableSeats { get; init; }
}
