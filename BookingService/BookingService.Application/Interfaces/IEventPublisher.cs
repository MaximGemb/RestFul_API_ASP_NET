using Shared.Contracts.BookingContracts;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Абстракция издателя событий брони.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Публикует событие подтверждения брони в брокер сообщений.
    /// </summary>
    /// <param name="message">Данные подтверждённой брони.</param>
    /// <param name="ct">Токен отмены.</param>
    Task PublishBookingConfirmedAsync(BookingConfirmed message, CancellationToken ct = default);
}
