using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;

/// <summary>
/// Сервис для работы с бронированиями через репозитории.
/// </summary>
public class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);
    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingService"/>.
    /// </summary>
    /// <param name="eventRepository">Репозиторий событий.</param>
    /// <param name="bookingRepository">Репозиторий бронирований.</param>
    public BookingService(IEventRepository eventRepository, IBookingRepository bookingRepository)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
    }

    /// <summary>
    /// Создает новую бронь для указанного события от имени пользователя.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="userId">Идентификатор пользователя, создающего бронь.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация о созданном бронировании.</returns>
    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        await BookingLock.WaitAsync(ct);
        try
        {
            var @event = await _eventRepository.FindByIdAsync(eventId, ct)
                         ?? throw new NotFoundException(eventId, $"Событие с идентификатором {eventId} не найдено.");

            var activeCount = await _bookingRepository.CountActiveByUserAsync(userId, ct);

            var newBooking = Booking.CreatePending(@event, userId, activeCount);

            @event.TryReserveSeats();

            await _bookingRepository.AddAsync(newBooking, ct);
            await _bookingRepository.SaveChangesAsync(ct);

            return ToInfo(newBooking);
        }
        finally
        {
            BookingLock.Release();
        }
    }

    /// <summary>
    /// Отменяет бронь. Администратор может отменить любую бронь; обычный пользователь — только свою.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования.</param>
    /// <param name="userId">Идентификатор пользователя, выполняющего отмену.</param>
    /// <param name="isAdmin">Признак администратора: если <c>true</c>, проверка владельца пропускается.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация об отменённом бронировании.</returns>
    public async Task<BookingInfo> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.FindByIdAsync(bookingId, ct)
                      ?? throw new NotFoundException(bookingId, $"Бронь с идентификатором {bookingId} не найдена.");

        var previousStatus = booking.Status;

        booking.Cancel(userId, isAdmin);

        if (previousStatus is BookingStatus.Pending or BookingStatus.Confirmed)
        {
            var @event = await _eventRepository.FindByIdAsync(booking.EventId, ct);
            @event?.ReleaseSeats();
        }

        await _bookingRepository.SaveChangesAsync(ct);
        return ToInfo(booking);
    }

    /// <summary>
    /// Возвращает бронь по идентификатору с проверкой прав доступа.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования.</param>
    /// <param name="userId">Идентификатор запрашивающего пользователя.</param>
    /// <param name="isAdmin">Признак администратора: если <c>true</c>, проверка владельца пропускается.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация о найденном бронировании.</returns>
    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.FindByIdAsync(bookingId, ct)
                      ?? throw new NotFoundException(bookingId, $"Бронь с идентификатором {bookingId} не найдена.");

        if (!isAdmin && booking.UserId != userId)
            throw new OperationNotAllowedException(userId,
                $"User {userId} is not allowed to view booking {bookingId} owned by another user.");

        return ToInfo(booking);
    }

    /// <summary>
    /// Маппинг сущности Booking в DTO BookingInfo.
    /// </summary>
    internal static BookingInfo ToInfo(Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        UserId = booking.UserId,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}
