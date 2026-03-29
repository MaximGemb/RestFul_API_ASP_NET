using RestFulApi.Exceptions;
using RestFulApi.Interfaces;
using RestFulApi.Models;

namespace RestFulApi.Services;

/// <summary>
/// Сервис для работы с бронированиями в памяти.
/// </summary>
public class BookingService : IBookingService
{
    private readonly List<Booking> _bookings = [];
    private readonly IEventService _eventService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingService"/>.
    /// </summary>
    /// <param name="eventService">Сервис для работы с событиями.</param>
    public BookingService(IEventService eventService)
    {
        _eventService = eventService;
    }

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
        _ = await _eventService.GetByIdAsync(eventId, ct);

        var newBooking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _bookings.Add(newBooking);
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

        var booking = _bookings.FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new NotFoundException(bookingId, $"Бронь с идентификатором {bookingId} не найдена.");

        return Task.FromResult(booking);
    }

    /// <summary>
    /// Получает список бронирований со статусом Pending.
    /// </summary>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Список ожидающих обработки бронирований.</returns>
    public Task<IEnumerable<Booking>> GetPendingBookingsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var pendingBookings = _bookings.Where(b => b.Status == BookingStatus.Pending).ToList();

        return Task.FromResult<IEnumerable<Booking>>(pendingBookings);
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

        var existingBooking = _bookings.FirstOrDefault(b => b.Id == booking.Id)
                              ?? throw new NotFoundException(booking.Id, $"Бронь с идентификатором {booking.Id} не найдена.");

        existingBooking.Status = booking.Status;
        existingBooking.ProcessedAt = booking.ProcessedAt;

        return Task.CompletedTask;
    }
}
