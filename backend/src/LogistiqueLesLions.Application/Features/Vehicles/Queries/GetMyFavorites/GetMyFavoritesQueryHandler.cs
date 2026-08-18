using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetMyFavorites;

public class GetMyFavoritesQueryHandler(
    IApplicationDbContext context,
    IPriceIndicatorService priceIndicator)
    : IRequestHandler<GetMyFavoritesQuery, Result<FavoritesDto>>
{
    public async Task<Result<FavoritesDto>> Handle(GetMyFavoritesQuery request, CancellationToken ct)
    {
        var alertsAllEnabled = await context.UserProfiles
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => u.FavoriteAlertsAllEnabled)
            .FirstOrDefaultAsync(ct);

        var rows = await context.SavedVehicles
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId)
            // El favorito sobrevive a la venta: solo se ocultan los anuncios que su
            // dueño nunca llegó a publicar o que retiró del todo.
            .Where(s => s.Vehicle.Status != VehicleStatus.Brouillon
                     && s.Vehicle.Status != VehicleStatus.Archive)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.PriceWhenSaved,
                s.PriceAlertEnabled,
                SavedAt = s.CreatedAt,
                Vehicle = new VehicleListDto(
                    s.Vehicle.Id,
                    s.Vehicle.PublicReference,
                    s.Vehicle.Slug,
                    s.Vehicle.Title,
                    s.Vehicle.Make.Name,
                    s.Vehicle.Model != null ? s.Vehicle.Model.Name : null,
                    s.Vehicle.Version,
                    s.Vehicle.Year,
                    s.Vehicle.Mileage,
                    s.Vehicle.Price,
                    s.Vehicle.Currency,
                    s.Vehicle.Region,
                    s.Vehicle.City,
                    s.Vehicle.Condition,
                    s.Vehicle.FuelType,
                    s.Vehicle.Transmission,
                    s.Vehicle.BodyType,
                    s.Vehicle.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                    s.Vehicle.Images.Where(i => i.IsPrimary).Select(i => i.ThumbnailUrl).FirstOrDefault(),
                    s.Vehicle.Images
                        .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                        .Select(i => i.ThumbnailUrl ?? i.Url)
                        .Take(8)
                        .ToList(),
                    s.Vehicle.Images.Count,
                    s.Vehicle.IsFeatured,
                    s.Vehicle.FavoritesCount,
                    s.Vehicle.ViewsCount,
                    s.Vehicle.CreatedAt,
                    s.Vehicle.Status,
                    s.Vehicle.SellerId,
                    null)   // el indicador de precio se calcula después, en bloque
            })
            .ToListAsync(ct);

        var indicators = await priceIndicator.CalculateManyAsync(
            rows.Select(r => r.Vehicle.Id).ToList(), ct);

        var items = rows.Select(r =>
        {
            var vehicle = indicators.TryGetValue(r.Vehicle.Id, out var ind) && ind.Indicator is not null
                ? r.Vehicle with { PriceIndicator = ind.Indicator }
                : r.Vehicle;

            var drop = r.PriceWhenSaved > vehicle.Price
                ? r.PriceWhenSaved - vehicle.Price
                : (decimal?)null;

            return new FavoriteItemDto(vehicle, r.PriceWhenSaved, drop, r.PriceAlertEnabled, r.SavedAt);
        }).ToList();

        return Result<FavoritesDto>.Success(new FavoritesDto(alertsAllEnabled, items));
    }
}
