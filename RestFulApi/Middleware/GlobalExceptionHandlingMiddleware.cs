using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RestFulApi.Exceptions;

namespace RestFulApi.Middleware;

/// <summary>
/// Middleware для глобальной обработки исключений.
/// Перехватывает все необработанные исключения и возвращает единообразный JSON-ответ
/// в формате Problem Details (RFC 7807).
/// </summary>
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
        {
            logger.LogInformation("Request was cancelled.");
        }
        else
        {
            logger.LogError(
                ex,
                "Unhandled exception. Method={Method}, Path={Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        if (httpContext.Response.HasStarted)
            return;

        var statusCode = MapStatusCode(ex);

        // Для отмененных запросов не возвращаем ProblemDetails, просто ставим статус-код
        if (ex is OperationCanceledException)
        {
            httpContext.Response.StatusCode = statusCode;
            return;
        }

        var detailMessage = statusCode is StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred."
            : ex.Message;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var error = new ProblemDetails
        {
            Status = statusCode,
            Detail = detailMessage
        };

        await httpContext.Response.WriteAsJsonAsync(error);
    }

    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            OperationCanceledException => StatusCodes.Status499ClientClosedRequest,
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            NoAvailableSeatsException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
}