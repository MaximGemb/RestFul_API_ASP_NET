using Microsoft.AspNetCore.Mvc;
using RestFulApi.DTOs;
using RestFulApi.Interfaces;

namespace RestFulApi.Controllers;

/// <summary>
/// Контроллер для управления бронированиями.
/// </summary>
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
}
