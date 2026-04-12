using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleFacets;

public class GetVehicleFacetsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetVehicleFacetsQuery, Result<VehicleFacetsDto>>
{
    public async Task<Result<VehicleFacetsDto>> Handle(
        GetVehicleFacetsQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = context.Vehicles
            .AsNoTracking()
            .Where(v => v.Status == VehicleStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLower();
            baseQuery = baseQuery.Where(v =>
                v.Title.ToLower().Contains(term) ||
                v.Make.Name.ToLower().Contains(term) ||
                (v.Model != null && v.Model.Name.ToLower().Contains(term)));
        }
        if (request.YearFrom.HasValue)  baseQuery = baseQuery.Where(v => v.Year >= request.YearFrom.Value);
        if (request.YearTo.HasValue)    baseQuery = baseQuery.Where(v => v.Year <= request.YearTo.Value);
        if (request.PriceFrom.HasValue) baseQuery = baseQuery.Where(v => v.Price >= request.PriceFrom.Value);
        if (request.PriceTo.HasValue)   baseQuery = baseQuery.Where(v => v.Price <= request.PriceTo.Value);

        // Facetas: cada faceta excluye su propio filtro para que los counts sigan
        // siendo orientativos al usuario (clásico Amazon-style).
        var qForMakes      = ApplyExcept(baseQuery, request, exceptMake: true);
        var qForFuel       = ApplyExcept(baseQuery, request, exceptFuel: true);
        var qForConditions = ApplyExcept(baseQuery, request, exceptCondition: true);
        var qForCountries  = ApplyExcept(baseQuery, request, exceptCountry: true);
        var qFull          = ApplyExcept(baseQuery, request);

        var makes = await qForMakes
            .GroupBy(v => new { v.MakeId, v.Make.Name })
            .Select(g => new FacetBucket(g.Key.MakeId.ToString(), g.Key.Name, g.Count()))
            .OrderByDescending(b => b.Count)
            .Take(20)
            .ToListAsync(cancellationToken);

        var fuels = await qForFuel
            .Where(v => v.FuelType != null)
            .GroupBy(v => v.FuelType)
            .Select(g => new FacetBucket(g.Key!.ToString()!, g.Key!.ToString()!, g.Count()))
            .OrderByDescending(b => b.Count)
            .ToListAsync(cancellationToken);

        var conditions = await qForConditions
            .GroupBy(v => v.Condition)
            .Select(g => new FacetBucket(g.Key.ToString(), g.Key.ToString(), g.Count()))
            .OrderByDescending(b => b.Count)
            .ToListAsync(cancellationToken);

        var countries = await qForCountries
            .GroupBy(v => v.CountryOrigin)
            .Select(g => new FacetBucket(g.Key, g.Key, g.Count()))
            .OrderByDescending(b => b.Count)
            .Take(20)
            .ToListAsync(cancellationToken);

        var total = await qFull.CountAsync(cancellationToken);

        return Result<VehicleFacetsDto>.Success(new VehicleFacetsDto(makes, fuels, conditions, countries, total));
    }

    private static IQueryable<Domain.Entities.Vehicle> ApplyExcept(
        IQueryable<Domain.Entities.Vehicle> q,
        GetVehicleFacetsQuery r,
        bool exceptMake = false,
        bool exceptFuel = false,
        bool exceptCondition = false,
        bool exceptCountry = false)
    {
        if (!exceptCountry && !string.IsNullOrWhiteSpace(r.CountryOrigin))
            q = q.Where(v => v.CountryOrigin == r.CountryOrigin);
        if (!exceptFuel && !string.IsNullOrWhiteSpace(r.FuelType)
                       && Enum.TryParse<FuelType>(r.FuelType, out var ft))
            q = q.Where(v => v.FuelType == ft);
        if (!exceptCondition && !string.IsNullOrWhiteSpace(r.Condition)
                            && Enum.TryParse<VehicleCondition>(r.Condition, out var cond))
            q = q.Where(v => v.Condition == cond);
        return q;
    }
}
