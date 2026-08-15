using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Recordatorio («Rappel») de un vehículo de Mon Garage.
/// </summary>
/// <remarks>
/// Puede depender de una fecha, de un kilometraje o de ambos:
/// <c>Vidange — 15 décembre 2026 ou 150.000 km</c>. Con las dos condiciones basta con que
/// se cumpla una: lo que llegue antes es lo que toca.
///
/// ⚠️ El kilometraje <b>nunca se estima</b>. Un recordatorio por kilómetros solo avanza
/// cuando el usuario pone al día el cuentakilómetros del vehículo, tal y como exige la
/// especificación.
/// </remarks>
public class VehicleReminder : AuditableEntity
{
    public Guid GarageVehicleId { get; set; }

    public ReminderType Type { get; set; } = ReminderType.Autre;

    /// <summary>«Prochaine vidange», «Assurance»…</summary>
    public string Label { get; set; } = string.Empty;

    public DateTimeOffset? DueDate { get; set; }
    public int? DueMileage { get; set; }

    public ReminderStatus Status { get; set; } = ReminderStatus.AVenir;

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Cuándo se avisó al usuario. Evita repetir la notificación en cada evaluación.
    /// </summary>
    public DateTimeOffset? NotifiedAt { get; set; }

    public string? Notes { get; set; }

    public GarageVehicle GarageVehicle { get; set; } = null!;

    /// <summary>Sigue pendiente: ni hecho ni anulado.</summary>
    public bool IsOpen => Status is ReminderStatus.AVenir or ReminderStatus.AFaire;

    /// <summary>
    /// La condición ya se cumple con la fecha o el kilometraje indicados.
    /// </summary>
    /// <param name="currentMileage">
    /// El último kilometraje que el usuario ha declarado. <c>null</c> si no consta: en
    /// ese caso la condición por kilómetros no se da por cumplida.
    /// </param>
    public bool IsDue(DateTimeOffset now, int? currentMileage) =>
        (DueDate is { } date && date <= now)
        || (DueMileage is { } mileage && currentMileage is { } current && current >= mileage);
}
