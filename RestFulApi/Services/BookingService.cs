using System.Collections.Concurrent;
using RestFulApi.Exceptions;
using RestFulApi.Interfaces;
using RestFulApi.Models;

namespace RestFulApi.Services;

/// <summary>
/// Сервис для работы с бронированиями в памяти.
/// </summary>
public class BookingService : IBookingService
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();
    private readonly Lock _bookingLock = new();
    private readonly IEventService _eventService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingService"/>.
    /// </summary>
    /// <param name="eventService">Сервис для работы с событиями.</param>
    public BookingService(IEventService eventService) =>
        _eventService = eventService;

    /// <summary>
    /// Создает новую бронь для указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Созданное бронирование.</returns>
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Проверяем, существует ли событие
        var @event = await _eventService.GetByIdAsync(eventId, ct);

        lock (_bookingLock)
            @event.TryReserveSeats();

        var newBooking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _bookings.TryAdd(newBooking.Id, newBooking);
        return newBooking;
    }

    /// <summary>
    /// Возвращает бронь по идентификатору.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Найденное бронирование.</returns>
    public Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return _bookings.TryGetValue(bookingId, out var booking)
            ? Task.FromResult(booking)
            : throw new NotFoundException(bookingId, $"Бронь с идентификатором {bookingId} не найдена.");
    }

    /// <summary>
    /// Получает список бронирований со статусом Pending.
    /// </summary>
    /// <param name="ct"> Токен отмены операции.</param>
    /// <returns>Список ожидающих обработки бронирований.</returns>
    public Task<IEnumerable<Booking>> GetPendingBookingsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var pendingBookings = _bookings.Values.Where(b => b.Status == BookingStatus.Pending);

        return Task.FromResult(pendingBookings);
    }

    /// <summary>
    /// Обновляет информацию о бронировании.
    /// </summary>
    /// <param name="booking">Обновленное бронирование.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public Task UpdateBookingAsync(Booking booking, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_bookings.ContainsKey(booking.Id))
            throw new NotFoundException(booking.Id, $"Бронь с идентификатором {booking.Id} не найдена.");

        _bookings[booking.Id] = booking;
        return Task.CompletedTask;
    }
}