using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Imagen asociada a un anuncio de vehículo.</summary>
public class VehicleImage : AuditableEntity
{
    public Guid VehicleId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public string? AltText { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    /// <summary>webp, jpeg, png</summary>
    public string Format { get; set; } = "webp";

    // Navegación
    public Vehicle Vehicle { get; set; } = null!;
}
