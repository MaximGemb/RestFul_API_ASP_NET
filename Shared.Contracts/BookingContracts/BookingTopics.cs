namespace Shared.Contracts.BookingContracts;

/// <summary>
/// Имена топиков Kafka, связанных с бронированием.
/// Используются издателем и подписчиком — обе стороны ссылаются на одни и те же строки.
/// </summary>
public static class BookingTopics
{
    /// <summary>
    /// Топик, в который BookingService публикует событие об успешном подтверждении брони.
    /// </summary>
    public const string BOOKING_CONFIRMED = "booking-confirmed";
}
