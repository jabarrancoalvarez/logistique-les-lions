namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Registro de cada precio que ha tenido un anuncio.
/// </summary>
/// <remarks>
/// Alimenta dos funcionalidades de la especificación: el bloque «Évolution du prix» de
/// la ficha y las alertas de bajada de precio de los Favoritos. Nunca se modifica ni se
/// borra: es un histórico append-only.
/// </remarks>
public class VehiclePriceHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }

    /// <summary>Precio en FCFA a partir de <see cref="ChangedAt"/>.</summary>
    public decimal Price { get; set; }

    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    public Vehicle Vehicle { get; set; } = null!;
}
