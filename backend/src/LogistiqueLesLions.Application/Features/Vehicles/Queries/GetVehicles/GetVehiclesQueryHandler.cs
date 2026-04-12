using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;

public class GetVehiclesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetVehiclesQuery, Result<PagedResult<VehicleListDto>>>
{
    public async Task<Result<PagedResult<VehicleListDto>>> Handle(
        GetVehiclesQuery request, CancellationToken cancellationToken)
    {
        // Si se filtra por vendedor, mostramos todos sus estados; si no, sólo activos
        var query = request.SellerId.HasValue
            ? context.Vehicles.AsNoTracking().IgnoreQueryFilters().Where(v => v.DeletedAt == null)
            : context.Vehicles.AsNoTracking().Where(v => v.Status == VehicleStatus.Active);

        // ─── Filtros ───────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLower();
            query = query.Where(v =>
                v.Title.ToLower().Contains(term) ||
                v.Make.Name.ToLower().Contains(term) ||
                (v.Model != null && v.Model.Name.ToLower().Contains(term)));
        }

        if (request.MakeId.HasValue)
            query = query.Where(v => v.MakeId == request.MakeId.Value);

        if (request.ModelId.HasValue)
            query = query.Where(v => v.ModelId == request.ModelId.Value);

        if (request.YearFrom.HasValue)
            query = query.Where(v => v.Year >= request.YearFrom.Value);

        if (request.YearTo.HasValue)
            query = query.Where(v => v.Year <= request.YearTo.Value);

        if (request.PriceFrom.HasValue)
            query = query.Where(v => v.Price >= request.PriceFrom.Value);

        if (request.PriceTo.HasValue)
            query = query.Where(v => v.Price <= request.PriceTo.Value);

        if (!string.IsNullOrWhiteSpace(request.CountryOrigin))
            query = query.Where(v => v.CountryOrigin == request.CountryOrigin);

        if (request.Condition.HasValue)
            query = query.Where(v => v.Condition == request.Condition.Value);

        if (request.FuelType.HasValue)
            query = query.Where(v => v.FuelType == request.FuelType.Value);

        if (request.Transmission.HasValue)
            query = query.Where(v => v.Transmission == request.Transmission.Value);

        if (request.BodyType.HasValue)
            query = query.Where(v => v.BodyType == request.BodyType.Value);

        if (request.IsExportReady.HasValue)
            query = query.Where(v => v.IsExportReady == request.IsExportReady.Value);

        if (request.SellerId.HasValue)
            query = query.Where(v => v.SellerId == request.SellerId.Value);

        if (request.Status.HasValue)
            query = query.Where(v => v.Status == request.Status.Value);

        if (request.IsFeatured.HasValue)
            query = query.Where(v => v.IsFeatured == request.IsFeatured.Value);

        // ─── Ordenación ────────────────────────────────────────────────────
        query = request.SortBy.ToLower() switch
        {
            "price"     => request.SortDesc ? query.OrderByDescending(v => v.Price)     : query.OrderBy(v => v.Price),
            "year"      => request.SortDesc ? query.OrderByDescending(v => v.Year)      : query.OrderBy(v => v.Year),
            "mileage"   => request.SortDesc ? query.OrderByDescending(v => v.Mileage)   : query.OrderBy(v => v.Mileage),
            "views"     => request.SortDesc ? query.OrderByDescending(v => v.ViewsCount): query.OrderBy(v => v.ViewsCount),
            _           => request.SortDesc ? query.OrderByDescending(v => v.CreatedAt) : query.OrderBy(v => v.CreatedAt),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page     = Math.Max(request.Page, 1);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(v => v.Make)
            .Include(v => v.Model)
            .Include(v => v.Images.Where(i => i.IsPrimary).Take(1))
            .Select(v => new VehicleListDto(
                v.Id,
                v.Slug,
                v.Title,
                v.Make.Name,
                v.Model != null ? v.Model.Name : null,
                v.Year,
                v.Mileage,
                v.Price,
                v.Currency,
                v.CountryOrigin,
                context.Countries.Where(c => c.Code == v.CountryOrigin).Select(c => c.FlagEmoji).FirstOrDefault(),
                v.Condition,
                v.FuelType,
                v.Transmission,
                v.BodyType,
                v.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                v.Images.Where(i => i.IsPrimary).Select(i => i.ThumbnailUrl).FirstOrDefault(),
                v.IsFeatured,
                v.IsExportReady,
                v.FavoritesCount,
                v.ViewsCount,
                v.CreatedAt,
                v.Status,
                v.SellerId
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<VehicleListDto>>.Success(
            new PagedResult<VehicleListDto>(items, totalCount, page, pageSize));
    }
}
