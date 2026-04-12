using System.Text.Json;
using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Requisitos técnicos de homologación por país destino y categoría de vehículo.</summary>
public class HomologationRequirement : AuditableEntity
{
    public string DestinationCountry { get; set; } = string.Empty;
    /// <summary>Categoría de vehículo: M1 (turismo), N1 (furgoneta), L3e (moto)...</summary>
    public string VehicleCategory { get; set; } = "M1";
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    /// <summary>Norma de emisiones mínima requerida: Euro 5, Euro 6...</summary>
    public string? EmissionStandard { get; set; }
    /// <summary>Lista de modificaciones técnicas obligatorias en JSONB</summary>
    public JsonDocument? RequiredModifications { get; set; }
    public decimal EstimatedCostEur { get; set; }
    public string? CertifyingBody { get; set; }
    public string? NotesEs { get; set; }
    public string? NotesEn { get; set; }
}
