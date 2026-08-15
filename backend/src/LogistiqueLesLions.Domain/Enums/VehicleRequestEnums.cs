namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Estados de una solicitud «Trouvez-moi une voiture».
/// </summary>
/// <remarks>
/// La especificación fija deliberadamente pocos estados para el MVP:
/// Nouvelle demande → En recherche → Véhicule proposé → Terminée / Annulée.
/// </remarks>
public enum VehicleRequestStatus
{
    /// <summary>La solicitud acaba de enviarse.</summary>
    NouvelleDemande = 1,
    /// <summary>Yoon u Auto ha comenzado a gestionarla.</summary>
    EnRecherche = 2,
    /// <summary>Se ha encontrado al menos una posible opción.</summary>
    VehiculePropose = 3,
    Terminee = 4,
    /// <summary>Cancelada por el usuario o por administración. Nunca se borra.</summary>
    Annulee = 5
}

/// <summary>
/// Procedencia deseada del vehículo. En la V1 la función se orienta sobre todo a la
/// importación.
/// </summary>
public enum VehicleRequestOrigin
{
    Importation = 1,
    Senegal = 2,
    Indifferent = 3
}
