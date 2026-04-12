using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace LogistiqueLesLions.API.Middleware;

/// <summary>
/// Middleware global de manejo de excepciones.
/// Traduce excepciones en respuestas HTTP estructuradas (RFC 7807 ProblemDetails).
/// Nunca expone stack traces en producción.
/// </summary>
public class ExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlerMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;

        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Errores de validación",
                validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "No autorizado",
                (Dictionary<string, string[]>?)null
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "Error interno del servidor",
                (Dictionary<string, string[]>?)null
            )
        };

        logger.LogError(exception,
            "Excepción no controlada [{CorrelationId}]: {ExceptionType} - {Message}",
            correlationId, exception.GetType().Name, exception.Message);

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions =
            {
                ["correlationId"] = correlationId,
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        };

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
