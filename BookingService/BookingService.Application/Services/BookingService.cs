using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;

namespace BookingService.Application.Services;

/// <summary>
/// Сервис для работы с бронированиями через репозиторий и клиент EventService.
/// </summary>
public class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IBookingRepository _bookingRepository;
    private readonly IEventServiceClient _eventServiceClient;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingService"/>.
    /// </summary>
    /// <param name="bookingRepository">Репозиторий бронирований.</param>
    /// <param name="eventServiceClient">Клиент для взаимодействия с EventService.</param>
    public BookingService(
        IBookingRepository bookingRepository,
        IEventServiceClient eventServiceClient)
    {
        _bookingRepository = bookingRepository;
        _eventServiceClient = eventServiceClient;
    }

    /// <inheritdoc />
    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        await BookingLock.WaitAsync(ct);
        try
        {
            var eventInfo = await _eventServiceClient.GetEventAvailabilityAsync(eventId, ct)
                            ?? throw new NotFoundException(eventId,
                                $"Событие с идентификатором {eventId} не найдено.");

            var activeCount = await _bookingRepository.CountActiveByUserAsync(userId, ct);

            var newBooking = Booking.CreatePending(eventId, eventInfo.StartAt, userId, activeCount);

            await _eventServiceClient.ReserveSeatAsync(eventId, ct);

            await _bookingRepository.AddAsync(newBooking, ct);
            await _bookingRepository.SaveChangesAsync(ct);

            return ToInfo(newBooking);
        }
        finally
        {
            BookingLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<BookingInfo> CancelBookingAsync(
        Guid bookingId,
        Guid userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        var booking = await _bookingRepository.FindByIdAsync(bookingId, ct)
                      ?? throw new NotFoundException(bookingId,
                          $"Бронь с идентификатором {bookingId} не найдена.");

        var previousStatus = booking.Status;

        booking.Cancel(userId, isAdmin);

        if (previousStatus is BookingStatus.Pending or BookingStatus.Confirmed)
        {
            try
            {
                await _eventServiceClient.ReleaseSeatAsync(booking.EventId, ct);
            }
            catch (Exception)
            {
                // Best-effort: если EventService недоступен, всё равно отменяем бронь
            }
        }

        await _bookingRepository.SaveChangesAsync(ct);
        return ToInfo(booking);
    }

    /// <inheritdoc />
    public async Task<BookingInfo> GetBookingByIdAsync(
        Guid bookingId,
        Guid userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        var booking = await _bookingRepository.FindByIdAsync(bookingId, ct)
                      ?? throw new NotFoundException(bookingId,
                          $"Бронь с идентификатором {bookingId} не найдена.");

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
