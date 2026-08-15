namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Tipo de intervención del historial de mantenimiento («Entretien»).
/// </summary>
/// <remarks>
/// La lista sale de la especificación. Con «Autre» al final, ninguna intervención se
/// queda sin registrar por no encajar en una categoría.
/// </remarks>
public enum MaintenanceType
{
    /// <summary>Vidange — cambio de aceite.</summary>
    Vidange = 1,
    /// <summary>Filtres.</summary>
    Filtres = 2,
    /// <summary>Pneus.</summary>
    Pneus = 3,
    /// <summary>Freins.</summary>
    Freins = 4,
    /// <summary>Batterie.</summary>
    Batterie = 5,
    /// <summary>Distribution — correa/cadena de distribución.</summary>
    Distribution = 6,
    /// <summary>Embrayage.</summary>
    Embrayage = 7,
    /// <summary>Suspension.</summary>
    Suspension = 8,
    /// <summary>Climatisation.</summary>
    Climatisation = 9,
    /// <summary>Réparation moteur.</summary>
    ReparationMoteur = 10,
    /// <summary>Révision générale.</summary>
    RevisionGenerale = 11,
    /// <summary>Autre.</summary>
    Autre = 99
}
