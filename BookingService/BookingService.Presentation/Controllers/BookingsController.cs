using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;

namespace BookingService.Presentation.Controllers;

/// <summary>
/// Контроллер для управления бронированиями.
/// </summary>
/// <param name="bookingService">Сервис для работы с бронированиями.</param>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    /// <summary>
    /// Получить информацию о бронировании по идентификатору.
    /// Владелец брони или администратор могут просматривать бронь.
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Текущее состояние брони.</returns>
    /// <response code="200">Бронь найдена.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="403">Пользователь не является владельцем брони.</response>
    /// <response code="404">Бронь не найдена.</response>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingInfo>> GetBooking(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var userId))
            return Unauthorized();

        var isAdmin = User.FindFirstValue("role") == "Admin";

        var booking = await bookingService.GetBookingByIdAsync(id, userId, isAdmin, ct);
        return Ok(booking);
    }

    /// <summary>
    /// Забронировать участие в событии.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Информация о созданной брони.</returns>
    /// <response code="202">Запрос на бронирование принят в обработку.</response>
    /// <response code="400">Событие уже началось.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="404">Событие не найдено.</response>
    /// <response code="409">Свободные места закончились или превышен лимит бронирований.</response>
    [Authorize]
    [HttpPost("events/{eventId:guid}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookEvent(Guid eventId, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var userId))
            return Unauthorized();

        var booking = await bookingService.CreateBookingAsync(eventId, userId, ct);

        return AcceptedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
    }

    /// <summary>
    /// Отменить бронирование. Может выполнить только владелец брони или администратор.
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Нет содержимого.</returns>
    /// <response code="204">Бронь успешно отменена.</response>
    /// <response code="401">Пользователь не аутентифицирован.</response>
    /// <response code="403">Пользователь не является владельцем брони.</response>
    /// <response code="404">Бронь не найдена.</response>
    /// <response code="409">Бронь уже отменена.</response>
    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CancelBooking(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var userId))
            return Unauthorized();

        var isAdmin = User.FindFirstValue("role") == "Admin";

        await bookingService.CancelBookingAsync(id, userId, isAdmin, ct);
        return NoContent();
    }
}
