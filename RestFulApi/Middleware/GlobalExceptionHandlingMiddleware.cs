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
    /// 
    /// </summary>
    /// <param name="context"></param>
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
        logger.LogError(
            ex,
            "Unhandled exception. Method={Method}, Path={Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        if (httpContext.Response.HasStarted)
            return;

        var statusCode = MapStatusCode(ex);
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
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };
}