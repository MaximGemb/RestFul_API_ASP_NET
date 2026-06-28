using System.Security.Claims;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
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

        var isAdmin = User.FindFirstValue("role") == nameof(Roles.Admin);

        var booking = await bookingService.GetBookingByIdAsync(id, userId, isAdmin, ct);
        return Ok(booking);
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

        var isAdmin = User.FindFirstValue("role") == nameof(Roles.Admin);

        await bookingService.CancelBookingAsync(id, userId, isAdmin, ct);
        return NoContent();
    }
}
