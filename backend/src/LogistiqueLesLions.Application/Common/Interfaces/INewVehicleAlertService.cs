namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Avisa a los usuarios cuyas búsquedas guardadas coinciden con un anuncio recién
/// publicado («Alerte nouveaux véhicules»).
/// </summary>
public interface INewVehicleAlertService
{
    /// <summary>
    /// Notifica el anuncio a las búsquedas guardadas con alerta activa que lo incluyan.
    /// Devuelve cuántas notificaciones se han creado.
    /// </summary>
    Task<int> NotifyMatchingSearchesAsync(Guid vehicleId, CancellationToken ct = default);
}
