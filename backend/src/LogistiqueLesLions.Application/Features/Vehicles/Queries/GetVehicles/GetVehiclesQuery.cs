using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;

/// <summary>Búsqueda paginada de vehículos con filtros.</summary>
public record GetVehiclesQuery(
    string? Search,
    Guid? MakeId,
    Guid? ModelId,
    int? YearFrom,
    int? YearTo,
    decimal? PriceFrom,
    decimal? PriceTo,
    string? CountryOrigin,
    VehicleCondition? Condition,
    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    bool? IsExportReady,
    bool? IsFeatured,
    string SortBy = "createdAt",
    bool SortDesc = true,
    int Page = 1,
    int PageSize = 20,
    Guid? SellerId = null,
    VehicleStatus? Status = null
) : IRequest<Result<PagedResult<VehicleListDto>>>;
