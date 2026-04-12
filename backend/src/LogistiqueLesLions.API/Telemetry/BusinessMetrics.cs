using System.Diagnostics.Metrics;

namespace LogistiqueLesLions.API.Telemetry;

/// <summary>
/// Métricas de negocio expuestas a Prometheus en /metrics.
/// El Meter "LogistiqueLesLions.Business" se registra en Program.cs.
/// </summary>
public sealed class BusinessMetrics : IDisposable
{
    public const string MeterName = "LogistiqueLesLions.Business";
    private readonly Meter _meter;

    public Counter<long> VehiclesCreated { get; }
    public Counter<long> ProcessesInitiated { get; }
    public Counter<long> IncidentsCreated { get; }
    public Counter<long> AiDescriptionsGenerated { get; }
    public Counter<long> WebhooksDispatched { get; }

    public BusinessMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        VehiclesCreated         = _meter.CreateCounter<long>("lll_vehicles_created_total");
        ProcessesInitiated      = _meter.CreateCounter<long>("lll_processes_initiated_total");
        IncidentsCreated        = _meter.CreateCounter<long>("lll_incidents_created_total");
        AiDescriptionsGenerated = _meter.CreateCounter<long>("lll_ai_descriptions_generated_total");
        WebhooksDispatched      = _meter.CreateCounter<long>("lll_webhooks_dispatched_total");
    }

    public void Dispose() => _meter.Dispose();
}
