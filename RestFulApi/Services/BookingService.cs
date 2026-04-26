using Microsoft.EntityFrameworkCore;
using RestFulApi.DataAccess;
using RestFulApi.DTOs;
using RestFulApi.Exceptions;
using RestFulApi.Interfaces;
using RestFulApi.Models;

namespace RestFulApi.Services;

/// <summary>
/// Сервис для работы с бронированиями через базу данных.
/// </summary>
internal class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingService"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Создает новую бронь для указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация о созданном бронировании.</returns>
    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        await BookingLock.WaitAsync(ct);
        try
        {
            var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct)
                         ?? throw new NotFoundException(eventId, $"Событие с идентификатором {eventId} не найдено.");

            @event.TryReserveSeats();

            var newBooking = Booking.CreatePending(eventId);
            await _context.Bookings.AddAsync(newBooking, ct);
            await _context.SaveChangesAsync(ct);

            return ToInfo(newBooking);
        }
        finally
        {
            BookingLock.Release();
        }
    }

    /// <summary>
    /// Возвращает бронь по идентификатору.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация о найденном бронировании.</returns>
    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct)
                      ?? throw new NotFoundException(bookingId, $"Бронь с идентификатором {bookingId} не найдена.");

        return ToInfo(booking);
    }

    /// <summary>
    /// Маппинг сущности Booking в DTO BookingInfo.
    /// </summary>
    internal static BookingInfo ToInfo(Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}