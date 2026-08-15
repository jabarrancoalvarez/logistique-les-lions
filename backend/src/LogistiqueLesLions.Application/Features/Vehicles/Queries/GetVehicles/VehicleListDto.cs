using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;

/// <summary>
/// Tarjeta del Marketplace. Lleva todo lo que la especificación pide mostrar sin abrir
/// el anuncio: marca, modelo, versión, combustible, año, kilometraje, ciudad y precio.
/// </summary>
public record VehicleListDto(
    Guid Id,
    string PublicReference,
    string Slug,
    string Title,
    string MakeName,
    string? ModelName,
    string? Version,
    int Year,
    int? Mileage,
    decimal Price,
    string Currency,
    string? Region,
    string? City,
    CustomsStatus? CustomsStatus,
    VehicleCondition Condition,
    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    string? PrimaryImageUrl,
    string? ThumbnailUrl,
    /// <summary>
    /// Fotografías en orden, para poder deslizarlas dentro de la propia tarjeta sin
    /// abrir el anuncio. Se limitan a las primeras para no inflar el listado.
    /// </summary>
    IReadOnlyList<string> Images,
    int ImageCount,
    bool IsFeatured,
    int FavoritesCount,
    int ViewsCount,
    DateTimeOffset CreatedAt,
    VehicleStatus Status,
    Guid SellerId,
    /// <summary>
    /// Bonne affaire / Prix correct / Prix élevé. <c>null</c> cuando no hay suficientes
    /// vehículos comparables: nunca debe mostrarse un indicador inventado.
    /// </summary>
    /// <remarks>
    /// No lleva valor por defecto a propósito: las consultas lo proyectan dentro de un
    /// árbol de expresión, que no admite argumentos opcionales. Se rellena después,
    /// en bloque, con <see cref="Common.Interfaces.IPriceIndicatorService"/>.
    /// </remarks>
    PriceIndicator? PriceIndicator
);
