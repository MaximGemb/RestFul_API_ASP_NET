using Microsoft.AspNetCore.Mvc;
using EventService.Application.Interfaces;
using Shared.Contracts.EventContracts;

namespace EventService.Presentation.Controllers;

/// <summary>
/// Внутренний контроллер для межсервисного взаимодействия с EventService.
/// Вызывается только BookingService — не предназначен для внешних клиентов.
/// </summary>
/// <param name="eventService">Сервис для работы с событиями.</param>
[ApiController]
[Route("internal/events")]
[Produces("application/json")]
public class InternalEventsController(IEventService eventService) : ControllerBase
{
    /// <summary>
    /// Получить информацию о доступности события (для BookingService).
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Краткая информация о доступности события.</returns>
    /// <response code="200">Событие найдено.</response>
    /// <response code="404">Событие не найдено.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventAvailabilityResponse>> GetAvailability(Guid id, CancellationToken ct)
    {
        var @event = await eventService.GetEventEntityByIdAsync(id, ct);

        return Ok(new EventAvailabilityResponse
        {
            Id = @event.Id,
            Title = @event.Title,
            StartAt = @event.StartAt!.Value,
            AvailableSeats = @event.AvailableSeats
        });
    }

    /// <summary>
    /// Зарезервировать одно место для события (для BookingService).
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Статус операции.</returns>
    /// <response code="200">Место успешно зарезервировано.</response>
    /// <response code="404">Событие не найдено.</response>
    /// <response code="409">Свободных мест нет.</response>
    [HttpPost("{id:guid}/reserve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reserve(Guid id, CancellationToken ct)
    {
        await eventService.ReserveSeatAsync(id, ct);
        return Ok();
    }

    /// <summary>
    /// Освободить одно место для события (для BookingService).
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Статус операции.</returns>
    /// <response code="200">Место успешно освобождено.</response>
    [HttpPost("{id:guid}/release")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Release(Guid id, CancellationToken ct)
    {
        await eventService.ReleaseSeatAsync(id, ct);
        return Ok();
    }
}
