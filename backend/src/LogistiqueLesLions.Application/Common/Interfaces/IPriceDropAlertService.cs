namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <summary>
/// Avisa a quienes siguen un anuncio cuando su precio baja.
/// </summary>
/// <remarks>
/// Según la especificación, la alerta no es una entidad que el usuario gestione aparte:
/// es una propiedad del favorito. Aquí solo se genera la notificación cuando corresponde.
/// </remarks>
public interface IPriceDropAlertService
{
    /// <summary>
    /// Notifica la bajada a los usuarios que tengan el anuncio en Favoris con la alerta
    /// activa. Devuelve cuántas notificaciones se han creado.
    /// </summary>
    Task<int> NotifyPriceDropAsync(
        Guid vehicleId, decimal previousPrice, decimal newPrice, CancellationToken ct = default);
}
