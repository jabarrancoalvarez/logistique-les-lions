namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Pasa los recordatorios de «À venir» a «À faire» cuando se cumple su condición y avisa
/// al usuario.
/// </summary>
/// <remarks>
/// Se evalúa desde dos sitios, porque las dos condiciones llegan por caminos distintos:
/// la <b>fecha</b> se cumple sola con el paso del tiempo —de eso se encarga el trabajo en
/// segundo plano— y el <b>kilometraje</b> solo cambia cuando el usuario lo declara, así
/// que se comprueba justo después de esa declaración.
///
/// ⚠️ Nunca se estima cuánto ha rodado el vehículo: la especificación lo prohíbe
/// expresamente.
/// </remarks>
public interface IReminderService
{
    /// <summary>
    /// Evalúa los recordatorios abiertos de un vehículo. Devuelve cuántos han pasado a
    /// «À faire».
    /// </summary>
    Task<int> EvaluateVehicleAsync(Guid garageVehicleId, CancellationToken ct = default);

    /// <summary>
    /// Evalúa los recordatorios por fecha que ya han vencido en toda la plataforma.
    /// Lo usa el trabajo en segundo plano.
    /// </summary>
    Task<int> EvaluateDueByDateAsync(CancellationToken ct = default);
}
