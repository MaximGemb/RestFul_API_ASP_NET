using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Presentation.Controllers;

/// <summary>
/// Контроллер для регистрации и аутентификации пользователей.
/// </summary>
/// <param name="userService">Сервис для работы с пользователями.</param>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Зарегистрировать нового пользователя.
    /// </summary>
    /// <param name="request">Данные нового пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Идентификатор созданного пользователя.</returns>
    /// <response code="200">Пользователь успешно зарегистрирован.</response>
    /// <response code="400">Переданы некорректные данные.</response>
    /// <response code="409">Пользователь с таким логином уже существует.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var userId = await userService.RegisterAsync(request.Login, request.Password, request.Role, ct);
        return Ok(userId);
    }

    /// <summary>
    /// Войти в систему и получить JWT-токен.
    /// </summary>
    /// <param name="request">Учётные данные пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Подписанный JWT-токен.</returns>
    /// <response code="200">Успешная аутентификация, возвращён токен.</response>
    /// <response code="401">Неверный логин или пароль.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var token = await userService.LoginAsync(request.Login, request.Password, ct);
        return Ok(new LoginResponse { Token = token });
    }
}
