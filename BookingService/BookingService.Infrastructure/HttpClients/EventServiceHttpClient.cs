using System.Net;
using System.Net.Http.Json;
using BookingService.Application.Interfaces;
using BookingService.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Shared.Contracts.EventContracts;

namespace BookingService.Infrastructure.HttpClients;

/// <summary>
/// HTTP-клиент для межсервисного взаимодействия с EventService.
/// Вызывает внутренние эндпоинты EventService (/internal/events/*).
/// </summary>
public class EventServiceHttpClient : IEventServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventServiceHttpClient> _logger;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="EventServiceHttpClient"/>.
    /// </summary>
    /// <param name="httpClient">HTTP-клиент с настроенным BaseAddress.</param>
    /// <param name="logger">Логгер.</param>
    public EventServiceHttpClient(HttpClient httpClient, ILogger<EventServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventAvailabilityResponse?> GetEventAvailabilityAsync(
        Guid eventId,
        CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/internal/events/{eventId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<EventAvailabilityResponse>(ct);
    }

    /// <inheritdoc />
    public async Task ReserveSeatAsync(Guid eventId, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync($"/internal/events/{eventId}/reserve", null, ct);

        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new NoAvailableSeatsException();

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new NotFoundException(eventId, $"Событие {eventId} не найдено в EventService.");

        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task ReleaseSeatAsync(Guid eventId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/internal/events/{eventId}/release", null, ct);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning(
                    "EventService вернул {StatusCode} при освобождении места для события {EventId}.",
                    response.StatusCode, eventId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось освободить место для события {EventId} в EventService.", eventId);
        }
    }
}
