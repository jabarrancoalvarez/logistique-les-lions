namespace LogistiqueLesLions.Application.Common.Interfaces;

/// <param name="Category">Ver <see cref="NotificationCategories"/>.</param>
public record PushedNotification(
    Guid Id, Guid UserId, string Category, string Title, string? Body, string? Link);

/// <summary>
/// Empuja notificaciones ya persistidas a los clientes conectados.
/// </summary>
/// <remarks>
/// Está separado de la persistencia a propósito: los servicios que generan avisos
/// guardan las filas dentro de su propia transacción y solo después llaman aquí. Así el
/// envío en tiempo real puede fallar sin arrastrar consigo la operación de negocio.
/// </remarks>
public interface INotificationPusher
{
    Task PushAsync(IReadOnlyCollection<PushedNotification> notifications, CancellationToken ct = default);
}

/// <summary>
/// Categorías de la campana. Determinan el icono que se muestra en la lista.
/// </summary>
public static class NotificationCategories
{
    /// <summary>Baisse de prix — un favorito ha bajado de precio.</summary>
    public const string PriceDrop = "price-drop";
    /// <summary>Nouvelle annonce — coincide con una búsqueda guardada.</summary>
    public const string NewListing = "new-listing";
    /// <summary>Véhicule trouvé — propuesta para una solicitud.</summary>
    public const string RequestProposal = "request-proposal";
    /// <summary>Mensaje dentro de una solicitud o de una negociación.</summary>
    public const string Message = "message";
    /// <summary>Nouvelle offre / contre-offre.</summary>
    public const string Offer = "offer";
    /// <summary>Contrat à valider.</summary>
    public const string Contract = "contract";
    /// <summary>Rappel d'entretien de Mon Garage.</summary>
    public const string Reminder = "reminder";
    /// <summary>Avisos dirigidos al backoffice.</summary>
    public const string Admin = "admin";
    /// <summary>Comunicaciones generales de la plataforma.</summary>
    public const string System = "system";
}
