using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Fabricante de vehículo (marca): BMW, Mercedes, Toyota...</summary>
public class VehicleMake : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsPopular { get; set; }

    // Navegación
    public ICollection<VehicleModel> Models { get; set; } = [];
    public ICollection<Vehicle> Vehicles { get; set; } = [];
}
