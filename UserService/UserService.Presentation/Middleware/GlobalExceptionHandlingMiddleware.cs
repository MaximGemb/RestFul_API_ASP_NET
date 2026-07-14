using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using UserService.Domain.Exceptions;

namespace UserService.Presentation.Middleware;

/// <summary>
/// Middleware для глобальной обработки исключений в UserService.
/// </summary>
/// <param name="next">Делегат следующего middleware в конвейере.</param>
/// <param name="logger">Логгер для записи ошибок.</param>
public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    /// <summary>
    /// Обрабатывает HTTP-запрос и перехватывает необработанные исключения.
    /// </summary>
    /// <param name="context">Текущий HTTP-контекст запроса.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception ex)
    {
        if (ex is OperationCanceledException)
            logger.LogInformation("Request was cancelled.");
        else
            logger.LogError(
                ex,
                "Unhandled exception. Method={Method}, Path={Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

        if (httpContext.Response.HasStarted)
            return;

        var (statusCode, title) = MapStatusCode(ex);

        if (ex is OperationCanceledException)
        {
            httpContext.Response.StatusCode = statusCode;
            return;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var safeDetail = ex is InvalidCredentialsException
                              or LoginAlreadyExistsException
                              or ValidationException
            ? ex.Message
            : "An unexpected error occurred.";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = safeDetail
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails);
    }

    private static (int statusCode, string title) MapStatusCode(Exception ex)
        => ex switch
        {
            OperationCanceledException => (499, "Client Closed Request"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            InvalidCredentialsException => (StatusCodes.Status404NotFound, "Not Found"),
            LoginAlreadyExistsException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };
}
