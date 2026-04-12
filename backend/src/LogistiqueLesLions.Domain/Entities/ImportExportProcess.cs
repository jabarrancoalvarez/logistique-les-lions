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
    /// <summary>
    /// Código corto público (12 chars) para tracking sin login.
    /// Se genera al crear el proceso. Único en la tabla.
    /// </summary>
    public string TrackingCode { get; set; } = string.Empty;

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

    /// <summary>
    /// Genera un código de tracking de 12 chars [A-Z0-9] sin caracteres ambiguos
    /// (sin 0/O ni 1/I/L) para que sea legible cuando el cliente lo escribe a mano.
    /// </summary>
    public static string GenerateTrackingCode()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        var bytes = new byte[12];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var chars = new char[12];
        for (var i = 0; i < 12; i++) chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }

    public void RecalculateCompletion()
    {
        if (!Documents.Any()) { CompletionPercent = 0; return; }
        var completed = Documents.Count(d => d.Status == DocumentStatus.Verified || d.Status == DocumentStatus.NotRequired);
        CompletionPercent = (int)Math.Round(completed * 100.0 / Documents.Count);
    }
}
