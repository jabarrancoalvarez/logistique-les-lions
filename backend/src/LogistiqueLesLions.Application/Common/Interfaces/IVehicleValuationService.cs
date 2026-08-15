namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Criterios con los que se ha construido la muestra.
/// </summary>
/// <remarks>
/// Se devuelven para poder decirle al usuario en qué se basa la cifra. La búsqueda
/// empieza por lo más parecido y va soltando criterios hasta reunir la muestra mínima;
/// nunca baja de marca, modelo y año.
/// </remarks>
[Flags]
public enum ValuationCriteria
{
    None = 0,
    /// <summary>Marca, modelo y franja de años. Siempre presente.</summary>
    MakeModelYear = 1,
    Mileage = 2,
    FuelAndTransmission = 4,
    Region = 8
}

/// <summary>
/// Estimación de valor de un vehículo de Mon Garage.
/// </summary>
/// <param name="HasEstimate">
/// <c>false</c> cuando no hay comparables suficientes. En ese caso no hay cifra alguna:
/// la especificación prohíbe inventarse una valoración.
/// </param>
public record VehicleValuationResult(
    bool HasEstimate,
    decimal? EstimatedValue,
    decimal? LowValue,
    decimal? HighValue,
    int ComparableCount,
    ValuationCriteria Criteria)
{
    public static VehicleValuationResult NotEnoughData(int found) =>
        new(false, null, null, null, found, ValuationCriteria.None);
}

/// <summary>
/// Calcula el valor estimado de un vehículo a partir de los anuncios comparables
/// publicados en Yoon u Auto.
/// </summary>
/// <remarks>
/// ⚠️ Sin Inteligencia Artificial y sin predicciones a futuro: la especificación lo
/// prohíbe expresamente en el MVP. Es estadística sobre los datos que ya hay.
/// </remarks>
public interface IVehicleValuationService
{
    Task<VehicleValuationResult> EstimateAsync(Guid garageVehicleId, CancellationToken ct = default);
}
