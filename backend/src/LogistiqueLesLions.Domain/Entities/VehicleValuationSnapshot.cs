using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Estimación de valor guardada en un momento dado, para poder dibujar
/// «Évolution de la valeur».
/// </summary>
/// <remarks>
/// Es un registro histórico: nunca se recalcula ni se corrige. Si el mercado cambia, lo
/// que cambia es la <b>siguiente</b> instantánea, no las anteriores — de eso trata
/// precisamente la evolución.
/// </remarks>
public class VehicleValuationSnapshot : AuditableEntity
{
    public Guid GarageVehicleId { get; set; }

    /// <summary>Mediana de los comparables en ese momento.</summary>
    public decimal EstimatedValue { get; set; }
    public decimal LowValue { get; set; }
    public decimal HighValue { get; set; }

    /// <summary>Cuántos anuncios sostenían la cifra.</summary>
    public int ComparableCount { get; set; }

    /// <summary>Kilometraje declarado cuando se tomó la instantánea.</summary>
    public int? Mileage { get; set; }

    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    public GarageVehicle GarageVehicle { get; set; } = null!;
}
