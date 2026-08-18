using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleBySlug;

/// <summary>Ficha completa del vehículo («Détail du véhicule»).</summary>
public record VehicleDetailDto(
    Guid Id,
    string PublicReference,
    string Slug,
    string Title,
    /// <summary>Bloque «Description du vendeur». Texto íntegro del usuario.</summary>
    string? Description,

    // ─── Información general ───────────────────────────────────────────────
    Guid MakeId,
    string MakeName,
    Guid? ModelId,
    string? ModelName,
    string? Version,
    int Year,
    int? Mileage,
    VehicleCondition Condition,
    BodyType? BodyType,
    FuelType? FuelType,
    TransmissionType? Transmission,
    string? Color,
    int? Doors,
    int? Seats,
    string? Vin,

    // ─── Motor y características técnicas ──────────────────────────────────
    int? PowerCv,
    int? EngineDisplacementCc,
    Drivetrain? Drivetrain,
    string? EngineName,


    // ─── Precio ────────────────────────────────────────────────────────────
    decimal Price,
    string Currency,
    bool PriceNegotiable,
    /// <summary>Primer precio registrado. Alimenta «Évolution du prix».</summary>
    decimal? InitialPrice,
    /// <summary>Fecha de la última bajada, si la hubo.</summary>
    DateTimeOffset? PriceChangedAt,

    /// <summary>
    /// Bonne affaire / Prix correct / Prix élevé. <c>null</c> si no hay suficientes
    /// comparables; en ese caso no se muestra ningún indicador.
    /// </summary>
    PriceIndicator? PriceIndicator,
    /// <summary>Anuncios usados como referencia. Permite explicar el cálculo.</summary>
    int PriceComparablesCount,

    // ─── Localisation ──────────────────────────────────────────────────────
    string? Region,
    string? City,
    string? District,

    // ─── Estado ────────────────────────────────────────────────────────────
    VehicleStatus Status,
    FeaturedTier FeaturedTier,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ReservedAt,
    DateTimeOffset? SoldAt,

    // ─── Contadores ────────────────────────────────────────────────────────
    int ViewsCount,
    int FavoritesCount,
    int ContactsCount,
    DateTimeOffset CreatedAt,

    IReadOnlyList<VehicleImageDto> Images,
    /// <summary>Solo el equipamiento declarado por quien publicó el vehículo.</summary>
    IReadOnlyList<VehicleEquipmentDto> Equipments,

    // ─── Vendu par ─────────────────────────────────────────────────────────
    Guid SellerId,
    string SellerName,
    string SellerAccountType,
    string? SellerCity,
    bool SellerPhoneVerified,
    int SellerVerifiedSalesCount,
    DateTimeOffset SellerMemberSince
);

public record VehicleImageDto(
    Guid Id,
    string Url,
    string? ThumbnailUrl,
    bool IsPrimary,
    int SortOrder,
    string? AltText
);

public record VehicleEquipmentDto(Guid Id, string Code, string Name);
