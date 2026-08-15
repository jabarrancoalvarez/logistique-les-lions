using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Intervención del historial de mantenimiento de un vehículo de Mon Garage.
/// </summary>
/// <remarks>
/// El objetivo del apartado es construir <b>progresivamente</b> un historial real del
/// vehículo: cada entrada se registra a mano y puede corregirse si el usuario se
/// equivoca, pero la traza de cuándo se creó y cuándo se tocó por última vez se conserva
/// (<c>CreatedAt</c> / <c>UpdatedAt</c> de la auditoría).
/// </remarks>
public class MaintenanceRecord : AuditableEntity
{
    public Guid GarageVehicleId { get; set; }

    public MaintenanceType Type { get; set; } = MaintenanceType.Autre;

    /// <summary>Fecha de la intervención. Es la que ordena el historial.</summary>
    public DateTimeOffset PerformedAt { get; set; }

    /// <summary>
    /// Kilometraje en el momento de la intervención.
    /// </summary>
    /// <remarks>
    /// Se guarda el del momento, no el actual del vehículo: el historial debe poder
    /// leerse años después («Dernière vidange: 145.320 km»).
    /// </remarks>
    public int? Mileage { get; set; }

    /// <summary>«Vidange + filtre à huile».</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Coste en FCFA. Opcional.</summary>
    public decimal? Cost { get; set; }

    /// <summary>Taller donde se hizo. Opcional.</summary>
    public string? Workshop { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Factura o documento asociado, si el usuario lo ha subido a Documents.
    /// </summary>
    /// <remarks>
    /// Se enlaza en lugar de duplicar el archivo: así el papel vive en un único sitio y
    /// la ficha puede mostrar «Facture disponible ✓» sin copiar nada.
    /// </remarks>
    public Guid? DocumentId { get; set; }

    public GarageVehicle GarageVehicle { get; set; } = null!;
    public GarageDocument? Document { get; set; }
    public ICollection<MaintenanceRecordImage> Images { get; set; } = [];

    /// <summary>«Facture disponible ✓» de la ficha.</summary>
    public bool HasInvoice => DocumentId is not null;
}

/// <summary>
/// Fotografía de una intervención.
/// </summary>
/// <remarks>
/// Va al almacenamiento privado, como los documentos: a diferencia de las fotos del
/// vehículo —que algún día se reutilizan al publicar el anuncio— estas no están
/// destinadas a hacerse públicas nunca.
/// </remarks>
public class MaintenanceRecordImage : AuditableEntity
{
    public Guid MaintenanceRecordId { get; set; }

    /// <summary>Clave en el almacenamiento privado. ❌ Nunca se expone en la API.</summary>
    public string StorageKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public MaintenanceRecord MaintenanceRecord { get; set; } = null!;
}
