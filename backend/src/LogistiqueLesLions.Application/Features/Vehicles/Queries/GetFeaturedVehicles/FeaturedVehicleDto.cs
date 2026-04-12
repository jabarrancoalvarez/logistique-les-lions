using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetFeaturedVehicles;

public record FeaturedVehicleDto(
    Guid Id,
    string Slug,
    string Title,
    string MakeName,
    string? ModelName,
    int Year,
    int? Mileage,
    decimal Price,
    string Currency,
    string CountryOrigin,
    string? CountryFlagEmoji,
    VehicleCondition Condition,
    FuelType? FuelType,
    TransmissionType? Transmission,
    string? PrimaryImageUrl,
    string? ThumbnailUrl,
    int FavoritesCount,
    int ViewsCount,
    DateTimeOffset CreatedAt
);
