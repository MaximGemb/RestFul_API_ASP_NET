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

        // Для отмененных запросов не возвращаем ProblemDetails, просто ставим статус-код
        if (ex is OperationCanceledException)
        {
            httpContext.Response.StatusCode = statusCode;
            return;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        if (ex is CustomValidationException validationEx)
        {
            var validationProblemDetails = new ValidationProblemDetails(
                validationEx.Errors.ToDictionary(k => k.Key, v => v.Value.ToArray()))
            {
                Status = statusCode,
                Title = title,
                Detail = ex.Message
            };

            await httpContext.Response.WriteAsJsonAsync(validationProblemDetails);
            return;
        }

        var safeDetail = ex is NotFoundException or NoAvailableSeatsException or ValidationException
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
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Client Closed Request"),
            CustomValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            NoAvailableSeatsException => (StatusCodes.Status409Conflict, "No Available Seats"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };
}