using System.Text.Json;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleBySlug;

public record VehicleDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string? DescriptionEs,
    string? DescriptionEn,
    Guid MakeId,
    string MakeName,
    Guid? ModelId,
    string? ModelName,
    int Year,
    int? Mileage,
    VehicleCondition Condition,
    BodyType? BodyType,
    FuelType? FuelType,
    TransmissionType? Transmission,
    string? Color,
    string? Vin,
    decimal Price,
    string Currency,
    bool PriceNegotiable,
    string CountryOrigin,
    string? CountryName,
    string? FlagEmoji,
    string? City,
    string? PostalCode,
    VehicleStatus Status,
    bool IsFeatured,
    bool IsExportReady,
    JsonDocument? Specs,
    JsonDocument? Features,
    int ViewsCount,
    int FavoritesCount,
    int ContactsCount,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? SoldAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<VehicleImageDto> Images,
    Guid SellerId
);

public record VehicleImageDto(
    Guid Id,
    string Url,
    string? ThumbnailUrl,
    bool IsPrimary,
    int SortOrder,
    string? AltText
);
