using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleBySlug;

public class GetVehicleBySlugQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetVehicleBySlugQuery, Result<VehicleDetailDto>>
{
    public async Task<Result<VehicleDetailDto>> Handle(
        GetVehicleBySlugQuery request, CancellationToken cancellationToken)
    {
        var v = await context.Vehicles
            .AsNoTracking()
            .Include(v => v.Make)
            .Include(v => v.Model)
            .Include(v => v.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(v => v.Slug == request.Slug, cancellationToken);

        if (v is null)
            return Result<VehicleDetailDto>.Failure($"Vehículo '{request.Slug}' no encontrado.");

        var country = await context.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == v.CountryOrigin, cancellationToken);

        var dto = new VehicleDetailDto(
            v.Id, v.Slug, v.Title, v.DescriptionEs, v.DescriptionEn,
            v.MakeId, v.Make.Name,
            v.ModelId, v.Model?.Name,
            v.Year, v.Mileage,
            v.Condition, v.BodyType, v.FuelType, v.Transmission,
            v.Color, v.Vin,
            v.Price, v.Currency, v.PriceNegotiable,
            v.CountryOrigin, country?.NameEs, country?.FlagEmoji,
            v.City, v.PostalCode,
            v.Status, v.IsFeatured, v.IsExportReady,
            v.Specs, v.Features,
            v.ViewsCount, v.FavoritesCount, v.ContactsCount,
            v.ExpiresAt, v.SoldAt, v.CreatedAt,
            v.Images.Select(i => new VehicleImageDto(i.Id, i.Url, i.ThumbnailUrl, i.IsPrimary, i.SortOrder, i.AltText))
                    .ToList()
                    .AsReadOnly(),
            v.SellerId
        );

        return Result<VehicleDetailDto>.Success(dto);
    }
}
