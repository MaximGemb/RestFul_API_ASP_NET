using System.ComponentModel.DataAnnotations;

namespace RestFulApi.Models;

/// <summary>
/// Представляет бронь на событие.
/// </summary>
public class Booking
{
    /// <summary>
    /// Уникальный идентификатор брони.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь.
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Текущий статус брони.
    /// </summary>
    [Required]
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Дата и время создания брони.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки брони.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}
