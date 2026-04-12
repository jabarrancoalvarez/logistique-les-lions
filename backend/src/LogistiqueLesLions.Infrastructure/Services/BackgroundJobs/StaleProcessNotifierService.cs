using System.Text;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Enums;
using LogistiqueLesLions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogistiqueLesLions.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Cron en proceso: cada X horas busca procesos cuyo UpdatedAt es anterior al
/// umbral configurado y que siguen en estados activos. Envía un email-resumen
/// al admin (vía IEmailSender) y publica un webhook (vía IWebhookPublisher).
///
/// Implementación deliberadamente simple — no usamos Hangfire/Quartz para no
/// añadir otra dependencia. Si en el futuro hace falta alta cadencia o jobs
/// distribuidos en múltiples instancias, mover a Hangfire es trivial.
/// </summary>
public class StaleProcessNotifierService(
    IServiceScopeFactory scopeFactory,
    IOptions<StaleProcessOptions> options,
    ILogger<StaleProcessNotifierService> logger) : BackgroundService
{
    private readonly StaleProcessOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("StaleProcessNotifierService deshabilitado por configuración");
            return;
        }

        // Espera inicial: que la app termine de arrancar antes de tocar la DB.
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (OperationCanceledException) { return; }

        var interval = TimeSpan.FromHours(Math.Max(1, _options.CheckIntervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error revisando procesos atascados");
            }

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task CheckOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db        = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailer   = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var webhooks  = scope.ServiceProvider.GetRequiredService<IWebhookPublisher>();

        var threshold = DateTimeOffset.UtcNow.AddDays(-_options.DaysThreshold);

        var stale = await db.ImportExportProcesses
            .AsNoTracking()
            .Where(p => p.UpdatedAt < threshold
                     && p.Status != ProcessStatus.Completed
                     && p.Status != ProcessStatus.Cancelled)
            .OrderBy(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Id,
                p.TrackingCode,
                p.OriginCountry,
                p.DestinationCountry,
                p.Status,
                p.CompletionPercent,
                p.UpdatedAt,
                VehicleTitle = p.Vehicle.Title
            })
            .Take(50)
            .ToListAsync(ct);

        if (stale.Count == 0)
        {
            logger.LogInformation("Sin procesos atascados (>={Days} días)", _options.DaysThreshold);
            return;
        }

        logger.LogWarning(
            "Detectados {Count} procesos sin actividad en los últimos {Days} días",
            stale.Count, _options.DaysThreshold);

        // 1) Email al admin
        if (!string.IsNullOrWhiteSpace(_options.AdminEmail))
        {
            var html = new StringBuilder();
            html.Append("<h2>Procesos sin actividad reciente</h2>");
            html.Append($"<p>Se han detectado <strong>{stale.Count}</strong> procesos sin avance en los últimos {_options.DaysThreshold} días.</p>");
            html.Append("<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;font-family:sans-serif;font-size:13px'>");
            html.Append("<tr><th>Tracking</th><th>Vehículo</th><th>Ruta</th><th>Estado</th><th>%</th><th>Última actividad</th></tr>");
            foreach (var p in stale)
            {
                html.Append("<tr>")
                    .Append($"<td>{p.TrackingCode}</td>")
                    .Append($"<td>{System.Net.WebUtility.HtmlEncode(p.VehicleTitle ?? "—")}</td>")
                    .Append($"<td>{p.OriginCountry} → {p.DestinationCountry}</td>")
                    .Append($"<td>{p.Status}</td>")
                    .Append($"<td>{p.CompletionPercent}%</td>")
                    .Append($"<td>{p.UpdatedAt:dd/MM/yyyy}</td>")
                    .Append("</tr>");
            }
            html.Append("</table>");

            await emailer.SendAsync(new EmailMessage(
                To: _options.AdminEmail,
                Subject: $"⚠ {stale.Count} procesos sin actividad reciente",
                HtmlBody: html.ToString()), ct);
        }

        // 2) Webhook (n8n puede crear tarea, postear a Slack, etc.)
        await webhooks.PublishAsync("process.stale.detected", new
        {
            count        = stale.Count,
            daysThreshold = _options.DaysThreshold,
            processes    = stale
        }, ct);
    }
}
