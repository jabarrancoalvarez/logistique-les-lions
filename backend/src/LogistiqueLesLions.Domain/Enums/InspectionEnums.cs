namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Puntos de la checklist de inspección presencial, tal y como los enumera la
/// especificación funcional.
/// </summary>
public enum InspectionItemType
{
    Moteur = 1,
    Carrosserie = 2,
    Pneus = 3,
    Interieur = 4,
    Climatisation = 5,
    Feux = 6,
    Freins = 7,
    Direction = 8,
    Documents = 9,
    Vin = 10,
    EssaiRoutier = 11
}

/// <summary>Valoración de un punto de la checklist.</summary>
public enum InspectionResult
{
    Bon = 1,
    Moyen = 2,
    Mauvais = 3
}
