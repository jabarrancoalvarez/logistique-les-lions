using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Documento concreto dentro de un proceso de tramitación (checklist item).</summary>
public class ProcessDocument : AuditableEntity
{
    public Guid ProcessId { get; set; }
    /// <summary>Tipo de documento: "COC", "DUA", "FACTURA_COMPRA", etc.</summary>
    public string DocumentType { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    /// <summary>Responsable: "buyer" o "seller"</summary>
    public string ResponsibleParty { get; set; } = "buyer";
    public DateTimeOffset? RequiredByDate { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public string? FileUrl { get; set; }
    public string? RejectionReason { get; set; }
    /// <summary>URL a la plantilla oficial descargable</summary>
    public string? TemplateUrl { get; set; }
    /// <summary>URL al organismo oficial donde tramitar</summary>
    public string? OfficialUrl { get; set; }
    public string? InstructionsEs { get; set; }
    /// <summary>Coste estimado de obtención en EUR</summary>
    public decimal? EstimatedCostEur { get; set; }

    public ImportExportProcess Process { get; set; } = null!;
}
