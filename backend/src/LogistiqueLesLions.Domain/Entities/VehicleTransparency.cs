using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// «Transparence du véhicule»: qué parte del historial de Mon Garage se enseña en el
/// anuncio.
/// </summary>
/// <remarks>
/// La documentación de Mon Garage es privada y <b>nada se publica automáticamente</b>:
/// quien vende elige expresamente qué comparte, y todo empieza apagado. Compartir el
/// historial es lo que permite que un anuncio diga
/// «7 entretiens enregistrés sur Yoon u Auto».
/// </remarks>
public class VehicleTransparency : AuditableEntity
{
    /// <summary>Anuncio al que se refiere.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Ficha de Mon Garage de la que sale el historial.</summary>
    public Guid GarageVehicleId { get; set; }

    /// <summary>Mostrar que hay historial de mantenimiento y cuántas intervenciones.</summary>
    public bool ShowMaintenanceHistory { get; set; }

    /// <summary>Mostrar además la fecha y el kilometraje de cada intervención.</summary>
    public bool ShowMaintenanceDetails { get; set; }

    /// <summary>Mostrar la evolución registrada del kilometraje.</summary>
    public bool ShowMileageEvolution { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public GarageVehicle GarageVehicle { get; set; } = null!;

    /// <summary>Intervenciones concretas que se enseñan.</summary>
    public ICollection<SharedMaintenanceRecord> SharedRecords { get; set; } = [];
}

/// <summary>
/// Intervención del historial que quien vende ha decidido enseñar.
/// </summary>
/// <remarks>
/// La factura se comparte por separado (<see cref="ShareInvoice"/>): enseñar que se hizo
/// una revisión no obliga a enseñar un papel que puede llevar datos personales.
/// </remarks>
public class SharedMaintenanceRecord : AuditableEntity
{
    public Guid TransparencyId { get; set; }
    public Guid MaintenanceRecordId { get; set; }

    /// <summary>Compartir también la factura enlazada a esa intervención.</summary>
    public bool ShareInvoice { get; set; }

    public VehicleTransparency Transparency { get; set; } = null!;
    public MaintenanceRecord MaintenanceRecord { get; set; } = null!;
}
