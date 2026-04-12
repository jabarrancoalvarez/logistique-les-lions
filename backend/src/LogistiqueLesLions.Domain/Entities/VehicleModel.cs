using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Modelo concreto de un fabricante: Serie 3, Clase C, Corolla...</summary>
public class VehicleModel : AuditableEntity
{
    public Guid MakeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public List<BodyType> BodyTypes { get; set; } = [];

    // Navegación
    public VehicleMake Make { get; set; } = null!;
    public ICollection<Vehicle> Vehicles { get; set; } = [];
}
