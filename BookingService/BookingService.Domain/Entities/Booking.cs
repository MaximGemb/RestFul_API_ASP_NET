using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using BookingService.Domain.Exceptions;

namespace BookingService.Domain.Entities;

/// <summary>
/// Представляет бронь на событие.
/// Навигационное свойство Event убрано — связь с EventService только по идентификатору.
/// </summary>
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public sealed class Booking
{
    /// <summary>
    /// Максимальное количество активных бронирований для одного пользователя.
    /// </summary>
    public const int MaxActiveBookingsPerUser = 10;

    /// <summary>
    /// Конструктор по умолчанию для Entity Framework.
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private Booking()
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр брони с заданными параметрами.
    /// </summary>
    private Booking(
        Guid id,
        Guid eventId,
        Guid userId,
        BookingStatus status,
        DateTime createdAt)
    {
        Id = id;
        EventId = eventId;
        UserId = userId;
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
    /// Связь с EventService — только по идентификатору, без навигационного свойства.
    /// </summary>
    [Required]
    public Guid EventId { get; private set; }

    /// <summary>
    /// Идентификатор пользователя, создавшего бронь.
    /// Связь с UserService — только по идентификатору, без навигационного свойства.
    /// </summary>
    [Required]
    public Guid UserId { get; private set; }

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
    /// Создает новую бронь в статусе <see cref="BookingStatus.Pending"/>.
    /// Проверяет, что пользователь не превысил лимит активных бронирований.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="activeBookingsCount">Текущее количество активных бронирований пользователя.</param>
    /// <returns>Новый экземпляр брони.</returns>
    /// <exception cref="ActiveBookingsLimitExceededException">Превышен лимит активных бронирований.</exception>
    public static Booking CreatePending(Guid eventId, Guid userId, int activeBookingsCount)
    {
        if (activeBookingsCount >= MaxActiveBookingsPerUser)
            throw new ActiveBookingsLimitExceededException(userId, MaxActiveBookingsPerUser);

        return new Booking(Guid.NewGuid(), eventId, userId, BookingStatus.Pending, DateTime.UtcNow);
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

    /// <summary>
    /// Отменяет бронирование.
    /// Только владелец брони или администратор может её отменить.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя, выполняющего отмену.</param>
    /// <param name="isAdmin">Признак администратора: если <c>true</c>, проверка владельца пропускается.</param>
    /// <exception cref="OperationNotAllowedException">Пользователь не является владельцем брони.</exception>
    /// <exception cref="InvalidOperationException">Бронь уже отменена.</exception>
    public void Cancel(Guid userId, bool isAdmin = false)
    {
        if (!isAdmin && UserId != userId)
            throw new OperationNotAllowedException(userId,
                $"User {userId} is not allowed to cancel booking {Id} owned by another user.");

        if (Status == BookingStatus.Cancelled)
            throw new InvalidOperationException($"Booking {Id} is already cancelled.");

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}
