using Microsoft.AspNetCore.Mvc;
using RestFulApi.DTOs;
using RestFulApi.Interfaces;
using RestFulApi.Models;

namespace RestFulApi.Controllers;

/// <summary>
/// Контроллер для управления событиями.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController(IEventService eventService) : ControllerBase
{
    /// <summary>
    /// Получить список всех событий.
    /// </summary>
    /// <returns>Список событий.</returns>
    /// <response code="200">Успешное выполнение.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {
        var events = await eventService.GetAll();
        return Ok(events);
    }

    /// <summary>
    /// Получить событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <returns>Событие с указанным идентификатором.</returns>
    /// <response code="200">Событие найдено.</response>
    /// <response code="404">Событие не найдено.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Event>> GetEvent(Guid id)
    {
        var ev = await eventService.GetById(id);
        return Ok(ev);
    }

    /// <summary>
    /// Создать новое событие.
    /// </summary>
    /// <param name="newEvent">Данные нового события.</param>
    /// <returns>Созданное событие.</returns>
    /// <response code="201">Событие успешно создано.</response>
    /// <response code="400">Переданы некорректные данные.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Event>> CreateEvent([FromBody] EventDto newEvent)
    {
        var createdEvent = await eventService.Create(newEvent);

        return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
    }

    /// <summary>
    /// Обновить существующее событие целиком.
    /// </summary>
    /// <param name="id">Идентификатор обновляемого события.</param>
    /// <param name="updatedEvent">Новые данные события.</param>
    /// <returns>Обновленное событие или статус операции.</returns>
    /// <response code="204">Событие успешно обновлено.</response>
    /// <response code="400">Переданы некорректные данные.</response>
    /// <response code="404">Событие не найдено.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] EventDto updatedEvent)
    {
        await eventService.Update(id, updatedEvent);
        return NoContent();
    }

    /// <summary>
    /// Удалить событие.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого события.</param>
    /// <returns>Статус операции.</returns>
    /// <response code="204">Событие успешно удалено.</response>
    /// <response code="404">Событие не найдено.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        await eventService.Delete(id);
        return NoContent();
    }
}