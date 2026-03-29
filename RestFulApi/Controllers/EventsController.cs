using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RestFulApi.DTOs;
using RestFulApi.Interfaces;
using RestFulApi.Models;

namespace RestFulApi.Controllers;

/// <summary>
/// Контроллер для управления событиями.
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EventsController(IEventService eventService, IBookingService bookingService) : ControllerBase
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
    public async Task<ActionResult<PaginatedResult<Event>>> GetEvents(
        [FromQuery] string? title = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] [Range(1, int.MaxValue)] int page = 1,
        [FromQuery] [Range(1, int.MaxValue)] int pageSize = 10,
        CancellationToken ct = default)
    {
        var events = await eventService.GetAllAsync(title, from, to, page, pageSize, ct);
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
    public async Task<ActionResult<Event>> GetEvent(Guid id, CancellationToken ct)
    {
        var ev = await eventService.GetByIdAsync(id, ct);
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
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Event>> CreateEvent([FromBody] EventDto newEvent, CancellationToken ct)
    {
        var createdEvent = await eventService.CreateAsync(newEvent, ct);

        return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
    }

    /// <summary>
    /// Обновить существующее событие целиком.
    /// </summary>
    /// <param name="id">Идентификатор обновляемого события.</param>
    /// <param name="updatedEvent">Новые данные события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновленное событие или статус операции.</returns>
    /// <response code="204">Событие успешно обновлено.</response>
    /// <response code="400">Переданы некорректные данные.</response>
    /// <response code="404">Событие не найдено.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] EventDto updatedEvent, CancellationToken ct)
    {
        await eventService.UpdateAsync(id, updatedEvent, ct);
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
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken ct)
    {
        await eventService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Забронировать участие в событии.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Информация о созданной брони.</returns>
    /// <response code="202">Запрос на бронирование принят в обработку.</response>
    /// <response code="404">Событие не найдено.</response>
    [HttpPost("{id:guid}/book")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BookEvent(Guid id, CancellationToken ct)
    {
        var booking = await bookingService.CreateBookingAsync(id, ct);

        // Возвращаем Location заголовок и информацию о брони
        return AcceptedAtAction("GetBooking", "Bookings", new { id = booking.Id }, booking);
    }
}