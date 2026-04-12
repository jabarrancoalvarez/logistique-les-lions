using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Plantilla de documento descargable por tipo y país.</summary>
public class DocumentTemplate : AuditableEntity
{
    public string Country { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? TemplateUrl { get; set; }
    public string? InstructionsEs { get; set; }
    public string? InstructionsEn { get; set; }
    public string? OfficialUrl { get; set; }
    public string? IssuingAuthority { get; set; }
    public decimal? EstimatedCostEur { get; set; }
    public int? EstimatedDays { get; set; }
}
