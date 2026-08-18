using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;

public class GetVehiclesQueryHandler(
    IApplicationDbContext context,
    IPriceIndicatorService priceIndicator)
    : IRequestHandler<GetVehiclesQuery, Result<PagedResult<VehicleListDto>>>
{
    /// <summary>Fotos que viajan con cada tarjeta del listado.</summary>
    private const int MaxCardImages = 8;

    public async Task<Result<PagedResult<VehicleListDto>>> Handle(
        GetVehiclesQuery request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var query = VehicleQueryFilters.ApplySorting(
            VehicleQueryFilters.Apply(context, request), request);

        var totalCount = await query.CountAsync(ct);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page     = Math.Max(request.Page, 1);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VehicleListDto(
                v.Id,
                v.PublicReference,
                v.Slug,
                v.Title,
                v.Make.Name,
                v.Model != null ? v.Model.Name : null,
                v.Version,
                v.Year,
                v.Mileage,
                v.Price,
                v.Currency,
                v.Region,
                v.City,
                v.Condition,
                v.FuelType,
                v.Transmission,
                v.BodyType,
                v.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                v.Images.Where(i => i.IsPrimary).Select(i => i.ThumbnailUrl).FirstOrDefault(),
                v.Images
                    .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                    .Select(i => i.ThumbnailUrl ?? i.Url)
                    .Take(MaxCardImages)
                    .ToList(),
                v.Images.Count,
                v.FeaturedUntil > now ? v.FeaturedTier : FeaturedTier.Aucune,
                v.FavoritesCount,
                v.ViewsCount,
                v.CreatedAt,
                v.Status,
                v.SellerId,
                null   // el indicador de precio se calcula después, en bloque
            ))
            .ToListAsync(ct);

        items = await WithPriceIndicatorAsync(items, ct);

        return Result<PagedResult<VehicleListDto>>.Success(
            new PagedResult<VehicleListDto>(items, totalCount, page, pageSize));
    }

    /// <summary>
    /// Añade el indicador de precio a las tarjetas de la página. Se calcula en bloque
    /// para no lanzar una consulta por tarjeta.
    /// </summary>
    private async Task<List<VehicleListDto>> WithPriceIndicatorAsync(
        List<VehicleListDto> items, CancellationToken ct)
    {
        if (items.Count == 0) return items;

        var indicators = await priceIndicator.CalculateManyAsync(
            items.Select(i => i.Id).ToList(), ct);

        return items
            .Select(i => indicators.TryGetValue(i.Id, out var r) && r.Indicator is not null
                ? i with { PriceIndicator = r.Indicator }
                : i)
            .ToList();
    }
}
