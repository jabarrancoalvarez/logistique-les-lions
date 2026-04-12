using System.Text.Json;
using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Normativa de importación/exportación para un par (origen, destino).
/// Actualizable por admins sin código.
/// </summary>
public class CountryRequirement : AuditableEntity
{
    public string OriginCountry { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    /// <summary>Tipos de documentos requeridos (serializado como JSON array de strings)</summary>
    public string DocumentTypesJson { get; set; } = "[]";
    public bool HomologationRequired { get; set; }
    /// <summary>Arancel aduanero en % (0 para intra-UE)</summary>
    public decimal CustomsRatePercent { get; set; }
    /// <summary>IVA a la importación en %</summary>
    public decimal VatRatePercent { get; set; }
    /// <summary>Fórmula del impuesto de matriculación en JSONB</summary>
    public JsonDocument? RoadTaxFormula { get; set; }
    public bool TechnicalInspectionRequired { get; set; }
    /// <summary>Coste estimado total de tramitación en EUR</summary>
    public decimal EstimatedProcessingCostEur { get; set; }
    /// <summary>Tiempo estimado en días</summary>
    public int EstimatedDays { get; set; }
    public string? NotesEs { get; set; }
    public string? NotesEn { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }

    public IReadOnlyList<string> GetDocumentTypes()
    {
        return JsonSerializer.Deserialize<List<string>>(DocumentTypesJson) ?? [];
    }
}
