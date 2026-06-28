using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;

namespace BookingService.Application.Services;

/// <summary>
/// Сервис для работы с бронированиями через репозиторий.
/// </summary>
public class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IBookingRepository _bookingRepository;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingService"/>.
    /// </summary>
    /// <param name="bookingRepository">Репозиторий бронирований.</param>
    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    /// <inheritdoc />
    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default)
    {
        await BookingLock.WaitAsync(ct);
        try
        {
            var activeCount = await _bookingRepository.CountActiveByUserAsync(userId, ct);

            var newBooking = Booking.CreatePending(eventId, userId, activeCount);

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

        booking.Cancel(userId, isAdmin);

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
