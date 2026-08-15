using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Catálogo de equipamiento seleccionable al publicar un anuncio: Climatisation,
/// Bluetooth, Caméra de recul, ISOFIX…
/// </summary>
/// <remarks>
/// Es un catálogo en base de datos y no una lista en el código porque la especificación
/// exige que el administrador pueda ampliarlo sin tocar el código (Configuration → Catálogos).
/// </remarks>
public class VehicleEquipment : AuditableEntity
{
    /// <summary>Identificador estable e independiente del idioma: CLIMATISATION, ISOFIX…</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Etiqueta mostrada al usuario, en francés.</summary>
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<VehicleEquipmentLink> Vehicles { get; set; } = [];
}

/// <summary>Relación N:M entre un anuncio y los equipamientos que declara.</summary>
public class VehicleEquipmentLink
{
    public Guid VehicleId { get; set; }
    public Guid EquipmentId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public VehicleEquipment Equipment { get; set; } = null!;
}
