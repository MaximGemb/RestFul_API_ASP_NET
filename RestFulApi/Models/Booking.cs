using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using RestFulApi.Exceptions;

namespace RestFulApi.Models;

/// <summary>
/// Представляет бронь на событие.
/// </summary>
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
internal sealed class Booking
{
    // ReSharper disable once UnusedMember.Local
    private Booking()
    {
    }

    private Booking(
        Guid id,
        Guid eventId,
        BookingStatus status,
        DateTime createdAt)
    {
        Id = id;
        EventId = eventId;
        Status = status;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Уникальный идентификатор брони.
    /// </summary>
    [Required]
    public Guid Id { get; private set; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь.
    /// </summary>
    [Required]
    public Guid EventId { get; private set; }

    /// <summary>
    /// Текущий статус брони.
    /// </summary>
    [Required]
    public BookingStatus Status { get; private set; }

    /// <summary>
    /// Дата и время создания брони.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Дата и время обработки брони.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Событие, к которому относится данная бронь.
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Создает новую бронь в статусе <see cref="BookingStatus.Pending"/> для указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <returns>Новый экземпляр брони.</returns>
    /// <exception cref="Exceptions.NotFoundException">Выбрасывается, если передан пустой идентификатор события.</exception>
    internal static Booking CreatePending(Guid eventId)
    {
        return eventId == Guid.Empty
            ? throw new NotFoundException(null, $"Can't get event with id {Guid.Empty}. Event not found")
            : new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);
    }

    /// <summary>
    /// Подтверждает бронирование.
    /// </summary>
    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отклоняет бронирование.
    /// </summary>
    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}