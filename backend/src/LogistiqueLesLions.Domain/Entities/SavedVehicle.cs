using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Vehículo guardado en Favoris por un usuario.
/// </summary>
/// <remarks>
/// Guarda una <b>referencia</b> al anuncio, nunca una copia de sus datos: si el precio
/// cambia, el favorito debe mostrar el precio actualizado, y si se vende debe aparecer
/// como «Vendu» en lugar de desaparecer.
/// </remarks>
public class SavedVehicle : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }

    /// <summary>
    /// Precio del anuncio en el momento de guardarlo. Es lo único que se copia, y solo
    /// para poder mostrar «depuis que vous l'avez enregistré, le prix a baissé de X».
    /// </summary>
    public decimal PriceWhenSaved { get; set; }

    /// <summary>
    /// Alerta de bajada de precio para este favorito concreto.
    /// </summary>
    /// <remarks>
    /// Solo se consulta cuando el usuario ha desactivado el interruptor general
    /// <see cref="UserProfile.FavoriteAlertsAllEnabled"/>. Ver <c>IsAlertActive</c>.
    /// </remarks>
    public bool PriceAlertEnabled { get; set; } = true;

    /// <summary>Última bajada notificada, para no avisar dos veces del mismo precio.</summary>
    public decimal? LastAlertedPrice { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
