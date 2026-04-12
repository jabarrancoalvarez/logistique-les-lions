using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Documento asociado a un vehículo (ITV, ficha técnica, COC, etc.).</summary>
public class VehicleDocument : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public VehicleDocumentType Type { get; set; }
    /// <summary>País al que pertenece el documento (ISO 2)</summary>
    public string Country { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public Guid? VerifiedBy { get; set; }
    public bool IsVerified => VerifiedAt.HasValue;

    public Vehicle Vehicle { get; set; } = null!;
}
