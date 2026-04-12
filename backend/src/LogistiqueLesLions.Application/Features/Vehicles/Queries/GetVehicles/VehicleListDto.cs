using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;

public record VehicleListDto(
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
    string? FlagEmoji,
    VehicleCondition Condition,
    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    string? PrimaryImageUrl,
    string? ThumbnailUrl,
    bool IsFeatured,
    bool IsExportReady,
    int FavoritesCount,
    int ViewsCount,
    DateTimeOffset CreatedAt,
    VehicleStatus Status,
    Guid SellerId
);
