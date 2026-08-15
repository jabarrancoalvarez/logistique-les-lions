using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Búsqueda guardada: el usuario no sigue un coche concreto, sino un tipo de coche.
/// </summary>
/// <remarks>
/// La alerta de nuevos vehículos <b>no es una entidad aparte</b>: es una propiedad de la
/// búsqueda, tal y como establece la especificación. Eso evita que el usuario tenga que
/// gestionar alertas y búsquedas por separado.
/// </remarks>
public class SavedSearch : AuditableEntity
{
    public Guid UserId { get; set; }

    /// <summary>Título mostrado: "Toyota Hilux".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Filtros exactos utilizados, serializados como JSON.
    /// </summary>
    /// <remarks>
    /// Se guardan como documento y no en columnas tipadas porque el conjunto de filtros
    /// crece con el producto: cada filtro nuevo exigiría una migración y una columna más.
    /// Al consultar se deserializan sobre el mismo objeto de filtros que usa el
    /// Marketplace, de modo que «ver los resultados» devuelve exactamente lo mismo.
    /// </remarks>
    public string FiltersJson { get; set; } = "{}";

    /// <summary>Alerte nouveaux véhicules: ON/OFF.</summary>
    public bool AlertEnabled { get; set; } = true;

    /// <summary>
    /// Fecha de publicación del anuncio más reciente ya notificado. Evita volver a
    /// avisar de vehículos que el usuario ya conoce.
    /// </summary>
    public DateTimeOffset? LastNotifiedAt { get; set; }
}
