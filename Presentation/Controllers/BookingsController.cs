using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Interfaces;

namespace Presentation.Controllers;

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
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Текущее состояние брони.</returns>
    /// <response code="200">Бронь найдена.</response>
    /// <response code="404">Бронь не найдена.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingInfo>> GetBooking(Guid id, CancellationToken ct)
    {
        var booking = await bookingService.GetBookingByIdAsync(id, ct);
        return Ok(booking);
    }

    /// <summary>
    /// Отменить бронирование. Может выполнить только владелец брони.
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Информация об отменённой брони.</returns>
    /// <response code="200">Бронь успешно отменена.</response>
    /// <response code="400">Отсутствует или некорректен заголовок X-User-Id.</response>
    /// <response code="403">Пользователь не является владельцем брони.</response>
    /// <response code="404">Бронь не найдена.</response>
    /// <response code="409">Бронь уже отменена.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingInfo>> CancelBooking(Guid id, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
            !Guid.TryParse(userIdHeader, out var userId))
            return BadRequest("Заголовок X-User-Id с корректным Guid обязателен.");

        var booking = await bookingService.CancelBookingAsync(id, userId, ct);
        return Ok(booking);
    }
}
