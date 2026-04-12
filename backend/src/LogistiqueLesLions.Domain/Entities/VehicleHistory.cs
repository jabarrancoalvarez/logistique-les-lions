using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Evento del historial de un vehículo (cambio propietario, accidente, ITV, etc.).</summary>
public class VehicleHistory : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public VehicleHistoryEventType EventType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset EventDate { get; set; }
    public int? MileageAtEvent { get; set; }
    /// <summary>País donde ocurrió el evento (ISO 2)</summary>
    public string? Country { get; set; }
    /// <summary>Fuente: carfax, autocheck, manual, oficial</summary>
    public string Source { get; set; } = "manual";
    public bool IsVerified { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
