using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Vehículo guardado en favoritos por un usuario.</summary>
public class SavedVehicle : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
