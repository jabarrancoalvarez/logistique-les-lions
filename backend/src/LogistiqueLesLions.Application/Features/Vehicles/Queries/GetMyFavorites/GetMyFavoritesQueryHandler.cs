using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetMyFavorites;

public class GetMyFavoritesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetMyFavoritesQuery, Result<List<VehicleListDto>>>
{
    public async Task<Result<List<VehicleListDto>>> Handle(
        GetMyFavoritesQuery request, CancellationToken cancellationToken)
    {
        var items = await context.SavedVehicles
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.Vehicle)
            .Where(v => v.Status == VehicleStatus.Active)
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

        return Result<List<VehicleListDto>>.Success(items);
    }
}
