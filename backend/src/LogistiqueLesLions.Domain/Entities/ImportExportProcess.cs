using System.Text.Json;
using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Proceso completo de importación/exportación de un vehículo.
/// Agregado raíz del módulo de tramitación.
/// </summary>
public class ImportExportProcess : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public string OriginCountry { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    public ProcessType ProcessType { get; set; }
    public ProcessStatus Status { get; set; } = ProcessStatus.Draft;

    // Costes
    public decimal? EstimatedCostEur { get; set; }
    public decimal? ActualCostEur { get; set; }

    // Fechas del proceso
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    /// <summary>Datos variables del proceso (referencias externas, notas, agente aduanero, etc.)</summary>
    public JsonDocument? Notes { get; set; }

    /// <summary>Porcentaje de documentos completados (0-100)</summary>
    public int CompletionPercent { get; set; }

    // Navegación
    public Vehicle Vehicle { get; set; } = null!;
    public ICollection<ProcessDocument> Documents { get; set; } = [];

    public void RecalculateCompletion()
    {
        if (!Documents.Any()) { CompletionPercent = 0; return; }
        var completed = Documents.Count(d => d.Status == DocumentStatus.Verified || d.Status == DocumentStatus.NotRequired);
        CompletionPercent = (int)Math.Round(completed * 100.0 / Documents.Count);
    }
}
