using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LogistiqueLesLions.Application.Common.Behaviors;

/// <summary>
/// Pipeline de MediatR: registra entrada/salida de cada Command o Query
/// con duración y correlationId para trazabilidad completa.
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        logger.LogInformation("→ Procesando {RequestName} {@Request}", requestName, request);

        try
        {
            var response = await next();
            sw.Stop();

            logger.LogInformation("← Completado {RequestName} en {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "✗ Error en {RequestName} después de {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
