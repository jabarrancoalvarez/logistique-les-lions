using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Checklist de inspección presencial que el usuario rellena cuando va a ver el vehículo.
/// </summary>
/// <remarks>
/// ⚠️ Es <b>completamente privada</b>: pertenece a quien la escribe y la otra parte no
/// puede verla en ningún caso. Toda consulta debe filtrar por <see cref="UserId"/>.
///
/// ⚠️ No constituye una certificación de Yoon u Auto: es una nota personal de quien
/// visita el coche, sin valor de garantía.
///
/// Pertenece a una negociación avanzada, no a la búsqueda.
/// </remarks>
public class VehicleInspection : AuditableEntity
{
    public Guid NegotiationId { get; set; }

    /// <summary>Autor. Cada parte puede tener la suya, y no se ven entre sí.</summary>
    public Guid UserId { get; set; }

    /// <summary>Fecha de la visita, si el usuario la indica.</summary>
    public DateTimeOffset? VisitedAt { get; set; }

    /// <summary>Kilometraje observado en el vehículo durante la visita.</summary>
    public int? ObservedMileage { get; set; }

    /// <summary>Observaciones personales, libres.</summary>
    public string? Notes { get; set; }

    public Negotiation Negotiation { get; set; } = null!;
    public ICollection<VehicleInspectionItem> Items { get; set; } = [];
}

/// <summary>Valoración de un punto concreto de la checklist.</summary>
public class VehicleInspectionItem : AuditableEntity
{
    public Guid InspectionId { get; set; }
    public InspectionItemType Type { get; set; }

    /// <summary><c>null</c> mientras el usuario no lo haya valorado.</summary>
    public InspectionResult? Result { get; set; }

    public string? Notes { get; set; }

    public VehicleInspection Inspection { get; set; } = null!;
}
