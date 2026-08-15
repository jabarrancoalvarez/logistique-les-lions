using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Vehículo de «Mon Garage»: uno que el usuario <b>posee</b>.
/// </summary>
/// <remarks>
/// No es un anuncio y no comparte tabla con <see cref="Vehicle"/> a propósito: un anuncio
/// es público y tiene precio, estado de publicación y visitas; esto es una ficha privada
/// que puede existir durante años sin que nadie más la vea.
///
/// Tampoco está limitado a lo comprado en Yoon u Auto: el usuario puede dar de alta un
/// coche que ya tenía antes de registrarse.
/// </remarks>
public class GarageVehicle : AuditableEntity
{
    public Guid UserId { get; set; }

    // ─── Datos principales ─────────────────────────────────────────────────
    public Guid MakeId { get; set; }
    public Guid? ModelId { get; set; }
    /// <summary>Versión/acabado: "2.0 D-4D Executive". Opcional.</summary>
    public string? Version { get; set; }
    public int Year { get; set; }
    /// <summary>Kilometraje actual. El usuario lo actualiza desde «Mettre à jour».</summary>
    public int? Mileage { get; set; }

    /// <summary>
    /// Cuándo se declaró por última vez el kilometraje.
    /// </summary>
    /// <remarks>
    /// No sirve <c>UpdatedAt</c>: cambia al tocar cualquier campo de la ficha, y aquí
    /// interesa saber si la <b>lectura del cuentakilómetros</b> está al día — de eso
    /// depende que los rappels por kilómetros tengan sentido y que la ficha se considere
    /// actualizada.
    /// </remarks>
    public DateTimeOffset? MileageUpdatedAt { get; set; }
    public FuelType? FuelType { get; set; }
    public TransmissionType? Transmission { get; set; }
    public BodyType? BodyType { get; set; }
    public int? PowerCv { get; set; }
    public int? EngineDisplacementCc { get; set; }
    public string? Color { get; set; }

    // ─── Identificación ────────────────────────────────────────────────────
    public string? RegistrationPlate { get; set; }
    public string? Vin { get; set; }

    // ─── Adquisición ───────────────────────────────────────────────────────
    public DateTimeOffset? PurchaseDate { get; set; }
    /// <summary>Precio pagado en FCFA.</summary>
    public decimal? PurchasePrice { get; set; }

    // ─── Origen ────────────────────────────────────────────────────────────
    /// <summary>
    /// Contrato de la compra, cuando el vehículo entró tras una venta verificada.
    /// </summary>
    /// <remarks>
    /// Sirve para dos cosas: distinguir en la ficha lo comprado dentro de la plataforma
    /// y evitar que el mismo contrato genere dos vehículos en el garaje.
    /// </remarks>
    public Guid? SourceContractId { get; set; }
    /// <summary>Anuncio del que salió, si procede.</summary>
    public Guid? SourceVehicleId { get; set; }

    /// <summary>
    /// Anuncio creado desde «Vendre ce véhicule», si el usuario ha puesto el coche a la
    /// venta.
    /// </summary>
    /// <remarks>
    /// Cierra el círculo de Yoon u Auto: el coche vuelve al Marketplace sin que su dueño
    /// tenga que reescribir la ficha. Evita además crear dos borradores del mismo coche.
    /// </remarks>
    public Guid? ListedVehicleId { get; set; }

    // ─── Navegación ────────────────────────────────────────────────────────
    public VehicleMake Make { get; set; } = null!;
    public VehicleModel? Model { get; set; }
    public ICollection<GarageVehicleImage> Images { get; set; } = [];
    public ICollection<GarageDocument> Documents { get; set; } = [];
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = [];
    public ICollection<VehicleReminder> Reminders { get; set; } = [];
    public ICollection<VehicleValuationSnapshot> ValuationSnapshots { get; set; } = [];

    /// <summary>Comprado dentro de Yoon u Auto con contrato validado.</summary>
    public bool BoughtOnYoonUAuto => SourceContractId is not null;
}

/// <summary>Fotografía de un vehículo de Mon Garage. Privada, como el resto de la ficha.</summary>
public class GarageVehicleImage : AuditableEntity
{
    public Guid GarageVehicleId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    public GarageVehicle GarageVehicle { get; set; } = null!;
}
