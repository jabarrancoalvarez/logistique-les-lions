using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleFacets;

/// <summary>
/// Devuelve agregaciones (facetas) para construir filtros tipo Amazon en el listado.
/// Aplica los filtros pasados (excepto la propia faceta) para que los conteos sean
/// "context-aware" — al elegir diésel, las marcas mostradas cuentan solo coches diésel.
/// </summary>
public record GetVehicleFacetsQuery(
    string? Search,
    int? YearFrom,
    int? YearTo,
    decimal? PriceFrom,
    decimal? PriceTo,
    string? CountryOrigin,
    string? FuelType,
    string? Condition
) : IRequest<Result<VehicleFacetsDto>>;

public record VehicleFacetsDto(
    IReadOnlyList<FacetBucket> Makes,
    IReadOnlyList<FacetBucket> FuelTypes,
    IReadOnlyList<FacetBucket> Conditions,
    IReadOnlyList<FacetBucket> Countries,
    int TotalMatches);

public record FacetBucket(string Key, string Label, int Count);
