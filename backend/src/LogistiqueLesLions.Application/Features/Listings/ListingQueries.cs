using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Listings;

/// <summary>
/// «Mes annonces»: los vehículos que el usuario está vendiendo o ha vendido.
/// </summary>
/// <remarks>
/// Es lo contrario de Mon Garage, que son los coches que posee. Un mismo vehículo puede
/// estar en los dos sitios.
/// </remarks>
public record GetMyListingsQuery(Guid UserId, VehicleStatus? Status = null)
    : IRequest<Result<MyListingsDto>>;

/// <param name="CountByStatus">Para las pestañas: cuántos anuncios hay en cada estado.</param>
public record MyListingsDto(
    IReadOnlyDictionary<VehicleStatus, int> CountByStatus,
    IReadOnlyList<MyListingDto> Listings
);

/// <param name="NegotiationCount">
/// Negociaciones abiertas sobre el anuncio: desde aquí se llega a ellas.
/// </param>
public record MyListingDto(
    Guid Id,
    string Slug,
    string PublicReference,
    string Title,
    VehicleStatus Status,
    decimal Price,
    int? Mileage,
    string? ThumbnailUrl,

    int ViewsCount,
    int FavoritesCount,
    int ContactsCount,
    int NegotiationCount,

    int QualityScore,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? SoldAt,

    /// <summary>Nivel de destacado vigente (Aucune si no lo está o ha caducado).</summary>
    FeaturedTier FeaturedTier,
    /// <summary>Hasta cuándo dura el destacado, para mostrar «En vedette jusqu'au…».</summary>
    DateTimeOffset? FeaturedUntil
);

public class GetMyListingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMyListingsQuery, Result<MyListingsDto>>
{
    public async Task<Result<MyListingsDto>> Handle(GetMyListingsQuery request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Sin el filtro global: los borradores y los archivados también son suyos.
        var mine = db.Vehicles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(v => v.SellerId == request.UserId && v.DeletedAt == null);

        var counts = await mine
            .GroupBy(v => v.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var listings = await mine
            .Where(v => request.Status == null || v.Status == request.Status)
            .Include(v => v.Images)
            .Include(v => v.Equipments)
            .OrderByDescending(v => v.PublishedAt ?? v.CreatedAt)
            .ToListAsync(ct);

        var ids = listings.Select(v => v.Id).ToList();

        var negotiations = await db.Negotiations
            .AsNoTracking()
            .Where(n => ids.Contains(n.VehicleId) && n.Status != NegotiationStatus.Terminee)
            .GroupBy(n => n.VehicleId)
            .Select(g => new { VehicleId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var negotiationsByVehicle = negotiations.ToDictionary(n => n.VehicleId, n => n.Count);

        var items = listings
            .Select(v => new MyListingDto(
                v.Id, v.Slug, v.PublicReference, v.Title, v.Status, v.Price, v.Mileage,
                v.Images.FirstOrDefault(i => i.IsPrimary)?.ThumbnailUrl
                    ?? v.Images.FirstOrDefault()?.ThumbnailUrl,
                v.ViewsCount, v.FavoritesCount, v.ContactsCount,
                negotiationsByVehicle.GetValueOrDefault(v.Id),
                ListingQualityCalculator.For(v).Score,
                v.CreatedAt, v.PublishedAt, v.SoldAt,
                v.IsFeaturedActive(now) ? v.FeaturedTier : FeaturedTier.Aucune,
                v.IsFeaturedActive(now) ? v.FeaturedUntil : null))
            .ToList();

        return Result<MyListingsDto>.Success(new MyListingsDto(
            counts.ToDictionary(c => c.Status, c => c.Count), items));
    }
}

/// <summary>Desglose de «Qualité de l'annonce» de un anuncio concreto.</summary>
public record GetListingQualityQuery(Guid UserId, Guid VehicleId)
    : IRequest<Result<ListingQualityDto>>;

public class GetListingQualityQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetListingQualityQuery, Result<ListingQualityDto>>
{
    public async Task<Result<ListingQualityDto>> Handle(
        GetListingQualityQuery request, CancellationToken ct)
    {
        var vehicle = await db.Vehicles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(v => v.Images)
            .Include(v => v.Equipments)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.DeletedAt == null, ct);

        if (vehicle is null) return Result<ListingQualityDto>.Failure("Vehicle.NotFound");

        if (vehicle.SellerId != request.UserId)
            return Result<ListingQualityDto>.Failure("Vehicle.NotOwner");

        return Result<ListingQualityDto>.Success(ListingQualityCalculator.For(vehicle));
    }
}
