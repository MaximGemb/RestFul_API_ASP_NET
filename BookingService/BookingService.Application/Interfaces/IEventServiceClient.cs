using Shared.Contracts.EventContracts;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Клиент для межсервисного взаимодействия с EventService.
/// Реализация находится в Infrastructure (HTTP-клиент).
/// </summary>
public interface IEventServiceClient
{
    /// <summary>
    /// Получает информацию о доступности события из EventService.
    /// Возвращает <c>null</c>, если событие не найдено.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<EventAvailabilityResponse?> GetEventAvailabilityAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Резервирует одно место для события в EventService.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="BookingService.Domain.Exceptions.NoAvailableSeatsException">Нет свободных мест.</exception>
    Task ReserveSeatAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Освобождает одно место для события в EventService (при отмене брони).
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="ct">Токен отмены.</param>
    Task ReleaseSeatAsync(Guid eventId, CancellationToken ct = default);
}
