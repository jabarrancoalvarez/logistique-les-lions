using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetFeaturedVehicles;

public class GetFeaturedVehiclesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetFeaturedVehiclesQuery, Result<IEnumerable<FeaturedVehicleDto>>>
{
    public async Task<Result<IEnumerable<FeaturedVehicleDto>>> Handle(
        GetFeaturedVehiclesQuery request,
        CancellationToken cancellationToken)
    {
        var vehicles = await context.Vehicles
            .AsNoTracking()
            .Where(v => v.IsFeatured && v.Status == VehicleStatus.Active)
            .OrderByDescending(v => v.CreatedAt)
            .Take(request.Count)
            .Include(v => v.Make)
            .Include(v => v.Model)
            .Include(v => v.Images.Where(i => i.IsPrimary).Take(1))
            .Select(v => new FeaturedVehicleDto(
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
                context.Countries
                    .Where(c => c.Code == v.CountryOrigin)
                    .Select(c => c.FlagEmoji)
                    .FirstOrDefault(),
                v.Condition,
                v.FuelType,
                v.Transmission,
                v.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                v.Images.Where(i => i.IsPrimary).Select(i => i.ThumbnailUrl).FirstOrDefault(),
                v.FavoritesCount,
                v.ViewsCount,
                v.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<FeaturedVehicleDto>>.Success(vehicles);
    }
}
