using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleBySlug;

public class GetVehicleBySlugQueryHandler(
    IApplicationDbContext context,
    IPriceIndicatorService priceIndicator)
    : IRequestHandler<GetVehicleBySlugQuery, Result<VehicleDetailDto>>
{
    public async Task<Result<VehicleDetailDto>> Handle(
        GetVehicleBySlugQuery request, CancellationToken ct)
    {
        var v = await context.Vehicles
            .AsNoTracking()
            .Include(v => v.Make)
            .Include(v => v.Model)
            .Include(v => v.Seller)
            .Include(v => v.Images.OrderBy(i => i.SortOrder))
            .Include(v => v.Equipments).ThenInclude(l => l.Equipment)
            .FirstOrDefaultAsync(v => v.Slug == request.Slug, ct);

        if (v is null)
            return Result<VehicleDetailDto>.Failure("Vehicle.NotFound");

        // Un anuncio sin página pública solo lo abre su dueño o el backoffice. Se
        // responde «no encontrado» y no «prohibido»: decir que existe ya sería contar
        // algo de un borrador ajeno.
        if (!v.HasPublicPage
            && !request.IsAdmin
            && (request.RequesterId is null || v.SellerId != request.RequesterId))
        {
            return Result<VehicleDetailDto>.Failure("Vehicle.NotFound");
        }

        // «Évolution du prix»: precio inicial y fecha del último cambio.
        var history = await context.VehiclePriceHistories
            .AsNoTracking()
            .Where(h => h.VehicleId == v.Id)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new { h.Price, h.ChangedAt })
            .ToListAsync(ct);

        var initialPrice = history.Count > 0 ? history[0].Price : (decimal?)null;
        // Solo interesa la fecha si el precio ha cambiado alguna vez.
        var priceChangedAt = history.Count > 1 ? history[^1].ChangedAt : (DateTimeOffset?)null;

        var indicator = await priceIndicator.CalculateAsync(v.Id, ct);

        var dto = new VehicleDetailDto(
            v.Id, v.PublicReference, v.Slug, v.Title, v.Description,
            v.MakeId, v.Make.Name,
            v.ModelId, v.Model?.Name,
            v.Version, v.Year, v.Mileage,
            v.Condition, v.BodyType, v.FuelType, v.Transmission,
            v.Color, v.Doors, v.Seats, v.Vin,
            v.PowerCv, v.EngineDisplacementCc, v.Drivetrain, v.EngineName,
            v.CustomsStatus,
            v.Price, v.Currency, v.PriceNegotiable, initialPrice, priceChangedAt,
            indicator.Indicator, indicator.ComparablesCount,
            v.Region, v.City, v.District,
            v.Status, v.IsFeatured, v.PublishedAt, v.ReservedAt, v.SoldAt,
            v.ViewsCount, v.FavoritesCount, v.ContactsCount, v.CreatedAt,
            v.Images
                .Select(i => new VehicleImageDto(i.Id, i.Url, i.ThumbnailUrl, i.IsPrimary, i.SortOrder, i.AltText))
                .ToList()
                .AsReadOnly(),
            v.Equipments
                .Where(l => l.Equipment.IsActive)
                .OrderBy(l => l.Equipment.DisplayOrder)
                .Select(l => new VehicleEquipmentDto(l.Equipment.Id, l.Equipment.Code, l.Equipment.Name))
                .ToList()
                .AsReadOnly(),
            v.SellerId,
            v.Seller?.DisplayName ?? "—",
            v.Seller?.AccountType.ToString() ?? "Particulier",
            v.Seller?.City,
            v.Seller?.PhoneVerified ?? false,
            v.Seller?.VerifiedSalesCount ?? 0,
            v.Seller?.CreatedAt ?? v.CreatedAt
        );

        return Result<VehicleDetailDto>.Success(dto);
    }
}
