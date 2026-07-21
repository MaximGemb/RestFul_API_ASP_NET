using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventService.Application.DTOs;
using EventService.Application.Interfaces;

namespace EventService.Presentation.Controllers;

/// <summary>
/// Контроллер для управления событиями.
/// </summary>
/// <param name="eventService">Сервис для работы с событиями.</param>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EventsController(IEventService eventService) : ControllerBase
{
    /// <summary>
    /// Получить список событий с фильтрацией и пагинацией.
    /// </summary>
    /// <param name="title">Фильтр по части названия события.</param>
    /// <param name="from">Минимальная дата начала события.</param>
    /// <param name="to">Максимальная дата окончания события.</param>
    /// <param name="page">Номер страницы, начиная с 1.</param>
    /// <param name="pageSize">Количество элементов на странице.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Пагинированный список событий.</returns>
    /// <response code="200">Успешное выполнение.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<EventInfo>>> GetEvents(
        [FromQuery] string? title = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] [Range(1, int.MaxValue)] int page = 1,
        [FromQuery] [Range(1, int.MaxValue)] int pageSize = 10,
        CancellationToken ct = default)
    {
        var events = await eventService.GetAllEventsAsync(title, from, to, page, pageSize, ct);
        return Ok(events);
    }

    /// <summary>
    /// Получить топ-10 самых популярных событий по проценту проданных мест.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Список из 10 самых популярных событий.</returns>
    /// <response code="200">Успешное выполнение.</response>
    [HttpGet("top")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventInfo>>> GetTopEvents(CancellationToken ct)
    {
        var events = await eventService.GetTopEventsAsync(ct);
        return Ok(events);
    }

    /// <summary>
    /// Получить событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Событие с указанным идентификатором.</returns>
    /// <response code="200">Событие найдено.</response>
    /// <response code="404">Событие не найдено.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventInfo>> GetEvent(Guid id, CancellationToken ct)
    {
        var ev = await eventService.GetEventByIdAsync(id, ct);
        return Ok(ev);
    }

    /// <summary>
    /// Создать новое событие.
    /// </summary>
    /// <param name="newEvent">Данные нового события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Созданное событие.</returns>
    /// <response code="201">Событие успешно создано.</response>
    /// <response code="400">Переданы некорректные данные.</response>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventInfo>> CreateEvent([FromBody] CreateEvent newEvent, CancellationToken ct)
    {
        var createdEvent = await eventService.CreateEventAsync(newEvent, ct);
        return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
    }

    /// <summary>
    /// Обновить существующее событие целиком.
    /// </summary>
    /// <param name="id">Идентификатор обновляемого события.</param>
    /// <param name="updatedEvent">Новые данные события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Статус операции.</returns>
    /// <response code="204">Событие успешно обновлено.</response>
    /// <response code="400">Переданы некорректные данные.</response>
    /// <response code="404">Событие не найдено.</response>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEvent updatedEvent, CancellationToken ct)
    {
        await eventService.UpdateEventAsync(id, updatedEvent, ct);
        return NoContent();
    }

    /// <summary>
    /// Удалить событие.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Статус операции.</returns>
    /// <response code="204">Событие успешно удалено.</response>
    /// <response code="404">Событие не найдено.</response>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken ct)
    {
        await eventService.DeleteEventAsync(id, ct);
        return NoContent();
    }
}
