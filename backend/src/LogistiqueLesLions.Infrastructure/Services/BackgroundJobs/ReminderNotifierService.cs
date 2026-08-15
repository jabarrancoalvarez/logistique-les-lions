using LogistiqueLesLions.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogistiqueLesLions.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Cada seis horas pasa a «À faire» los recordatorios de Mon Garage cuya fecha ya ha
/// llegado y avisa a su dueño.
/// </summary>
/// <remarks>
/// Solo se ocupa de la condición por <b>fecha</b>, que se cumple sola con el paso del
/// tiempo. La condición por <b>kilometraje</b> se evalúa cuando el usuario declara una
/// lectura nueva: la especificación prohíbe estimar cuánto ha rodado el vehículo.
///
/// Sigue el patrón de <see cref="StaleProcessNotifierService"/>: cron en proceso, sin
/// Hangfire ni Quartz. Con varias instancias en paralelo cada una haría el mismo trabajo,
/// pero el aviso no se duplica porque <c>NotifiedAt</c> lo impide.
/// </remarks>
public class ReminderNotifierService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReminderNotifierService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Espera inicial: que la app termine de arrancar antes de tocar la DB.
        try { await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<IReminderService>();

                var count = await reminders.EvaluateDueByDateAsync(stoppingToken);

                if (count > 0)
                    logger.LogInformation("{Count} rappels pasados a «À faire»", count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error evaluando los rappels de Mon Garage");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}
