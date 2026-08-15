namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Estados del anuncio, tal y como los define la especificación funcional:
/// Brouillon · Actif · En pause · Réservé · Vendu · Archivé.
/// </summary>
public enum VehicleStatus
{
    /// <summary>Borrador: creado pero todavía no publicado.</summary>
    Brouillon = 0,
    /// <summary>Publicado y visible en el Marketplace.</summary>
    Actif = 1,
    /// <summary>Pausado por su propietario. No aparece en búsquedas.</summary>
    EnPause = 2,
    /// <summary>Reservado. Se informa claramente en la ficha.</summary>
    Reserve = 3,
    /// <summary>Vendido. La ficha sigue existiendo pero desactiva oferta y contacto.</summary>
    Vendu = 4,
    /// <summary>Archivado por el propietario o por administración.</summary>
    Archive = 5
}
