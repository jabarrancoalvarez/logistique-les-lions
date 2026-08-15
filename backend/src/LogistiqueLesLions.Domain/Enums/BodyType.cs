namespace LogistiqueLesLions.Domain.Enums;

/// <summary>Carrocería. Valores del filtro «Carrocería» de la especificación.</summary>
public enum BodyType
{
    Citadine = 1,
    Berline = 2,
    Break = 3,
    /// <summary>SUV / 4x4.</summary>
    Suv = 4,
    Coupe = 5,
    Cabriolet = 6,
    Monospace = 7,
    PickUp = 8,
    /// <summary>Fourgon / Utilitaire.</summary>
    Utilitaire = 9,
    /// <summary>Solo para datos heredados que no encajan en las categorías anteriores.</summary>
    Autre = 99
}
