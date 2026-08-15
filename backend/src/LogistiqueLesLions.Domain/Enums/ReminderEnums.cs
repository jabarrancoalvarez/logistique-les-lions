namespace LogistiqueLesLions.Domain.Enums;

/// <summary>Motivo de un recordatorio de Mon Garage.</summary>
public enum ReminderType
{
    /// <summary>Vidange.</summary>
    Vidange = 1,
    /// <summary>Assurance.</summary>
    Assurance = 2,
    /// <summary>Contrôle technique.</summary>
    Inspection = 3,
    /// <summary>Pneus.</summary>
    Pneus = 4,
    /// <summary>Distribution.</summary>
    Distribution = 5,
    /// <summary>Freins.</summary>
    Freins = 6,
    /// <summary>Révision.</summary>
    Revision = 7,
    /// <summary>Autre.</summary>
    Autre = 99
}

/// <summary>
/// Estado de un recordatorio.
/// </summary>
/// <remarks>
/// <c>AVenir</c> → <c>AFaire</c> lo decide el sistema al cumplirse la condición; el resto
/// los marca el usuario.
/// </remarks>
public enum ReminderStatus
{
    /// <summary>À venir — todavía no toca.</summary>
    AVenir = 1,
    /// <summary>À faire — ha llegado la fecha o el kilometraje.</summary>
    AFaire = 2,
    /// <summary>Terminé.</summary>
    Termine = 3,
    /// <summary>Annulé.</summary>
    Annule = 4
}
