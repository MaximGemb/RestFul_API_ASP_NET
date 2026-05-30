using RestFulApi.DTOs;
using Domain.Exceptions;
using RestFulApi.Interfaces;
using Domain.Entities;

namespace RestFulApi.Services;

/// <summary>
/// Сервис для работы с бронированиями через репозитории.
/// </summary>
internal class BookingService : IBookingService
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
            var @event = await _eventRepository.FindByIdAsync(eventId, ct)
                         ?? throw new NotFoundException(eventId, $"Событие с идентификатором {eventId} не найдено.");

            @event.TryReserveSeats();

            var newBooking = Booking.CreatePending(eventId);
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
    /// Возвращает бронь по идентификатору.
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Информация о найденном бронировании.</returns>
    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.FindByIdAsync(bookingId, ct)
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