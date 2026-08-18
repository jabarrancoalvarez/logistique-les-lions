using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Listings;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Listings;

/// <summary>Listado de anuncios del backoffice.</summary>
public record GetAdminListingsQuery(
    string? Search = null,
    Guid? MakeId = null,
    Guid? ModelId = null,
    Guid? SellerId = null,
    string? City = null,
    VehicleStatus? Status = null,
    AccountType? SellerAccountType = null,
    decimal? PriceFrom = null,
    decimal? PriceTo = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null,
    bool? Hidden = null,
    bool? Flagged = null,
    bool? Reported = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<AdminListingListDto>>;

public record AdminListingListDto(
    int TotalCount, int Page, int PageSize, IReadOnlyList<AdminListingRowDto> Items);

public record AdminListingRowDto(
    Guid Id,
    string PublicReference,
    string Slug,
    string Title,
    VehicleStatus Status,
    bool HiddenByAdmin,
    bool FlaggedForReview,
    decimal Price,
    string? City,
    Guid SellerId,
    string SellerName,
    AccountType SellerAccountType,
    int ViewsCount,
    int FavoritesCount,
    int QualityScore,
    /// <summary>Signalements abiertos sobre este anuncio.</summary>
    int OpenReports,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt
);

public class GetAdminListingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminListingsQuery, Result<AdminListingListDto>>
{
    public async Task<Result<AdminListingListDto>> Handle(
        GetAdminListingsQuery request, CancellationToken ct)
    {
        // Sin filtros globales: el administrador ve también borradores y archivados.
        var query = db.Vehicles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(v => v.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // La referencia Yoon es lo primero que se teclea cuando alguien reporta algo.
            var term = request.Search.Trim().ToLower();
            query = query.Where(v =>
                v.PublicReference.ToLower().Contains(term)
                || v.Title.ToLower().Contains(term));
        }

        if (request.MakeId is { } makeId) query = query.Where(v => v.MakeId == makeId);
        if (request.ModelId is { } modelId) query = query.Where(v => v.ModelId == modelId);
        if (request.SellerId is { } sellerId) query = query.Where(v => v.SellerId == sellerId);

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim().ToLower();
            query = query.Where(v => v.City != null && v.City.ToLower() == city);
        }

        if (request.Status is { } status) query = query.Where(v => v.Status == status);
        if (request.PriceFrom is { } from) query = query.Where(v => v.Price >= from);
        if (request.PriceTo is { } to) query = query.Where(v => v.Price <= to);
        if (request.CreatedFrom is { } createdFrom) query = query.Where(v => v.CreatedAt >= createdFrom);
        if (request.CreatedTo is { } createdTo) query = query.Where(v => v.CreatedAt <= createdTo);

        if (request.Hidden is { } hidden)
            query = hidden ? query.Where(v => v.AdminHiddenAt != null)
                           : query.Where(v => v.AdminHiddenAt == null);

        if (request.Flagged is { } flagged)
            query = flagged ? query.Where(v => v.AdminFlaggedAt != null)
                            : query.Where(v => v.AdminFlaggedAt == null);

        // «Reportado» es tener signalements abiertos: uno ya resuelto no deja el anuncio
        // marcado para siempre.
        if (request.Reported is { } reported)
        {
            var openListingReports = db.Reports.Where(r =>
                r.TargetType == ReportTargetType.Listing
                && (r.Status == ReportStatus.Nouveau || r.Status == ReportStatus.EnExamen));

            query = reported
                ? query.Where(v => openListingReports.Any(r => r.TargetId == v.Id))
                : query.Where(v => !openListingReports.Any(r => r.TargetId == v.Id));
        }

        if (request.SellerAccountType is { } accountType)
        {
            query = query.Where(v => db.UserProfiles
                .Any(u => u.Id == v.SellerId && u.AccountType == accountType));
        }

        var total = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var listings = await query
            .Include(v => v.Images)
            .Include(v => v.Equipments)
            .Include(v => v.Seller)
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var listingIds = listings.Select(v => v.Id).ToList();
        var reportsByListing = await db.Reports
            .AsNoTracking()
            .Where(r => r.TargetType == ReportTargetType.Listing
                     && listingIds.Contains(r.TargetId)
                     && (r.Status == ReportStatus.Nouveau || r.Status == ReportStatus.EnExamen))
            .GroupBy(r => r.TargetId)
            .Select(g => new { TargetId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var openReports = reportsByListing.ToDictionary(x => x.TargetId, x => x.Count);

        var rows = listings
            .Select(v => new AdminListingRowDto(
                v.Id, v.PublicReference, v.Slug, v.Title, v.Status,
                v.AdminHiddenAt is not null, v.AdminFlaggedAt is not null,
                v.Price, v.City, v.SellerId,
                v.Seller?.DisplayName ?? "—",
                v.Seller?.AccountType ?? AccountType.Particulier,
                v.ViewsCount, v.FavoritesCount,
                ListingQualityCalculator.For(v).Score,
                openReports.GetValueOrDefault(v.Id),
                v.CreatedAt, v.PublishedAt))
            .ToList();

        return Result<AdminListingListDto>.Success(
            new AdminListingListDto(total, page, pageSize, rows));
    }
}

/// <summary>Ficha administrativa de un anuncio.</summary>
public record GetAdminListingQuery(Guid VehicleId) : IRequest<Result<AdminListingDetailDto>>;

public record AdminListingDetailDto(
    AdminListingRowDto Listing,
    DateTimeOffset UpdatedAt,
    string SellerPhone,
    int ContactsCount,
    int NegotiationsCount,
    int OffersReceived,
    ListingQualityDto Quality,
    IReadOnlyList<AdminPriceHistoryDto> PriceHistory,
    IReadOnlyList<Users.AdminActionDto> Actions,
    IReadOnlyList<Users.AdminNoteDto> Notes
);

public record AdminPriceHistoryDto(decimal Price, DateTimeOffset ChangedAt);

public class GetAdminListingQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminListingQuery, Result<AdminListingDetailDto>>
{
    public async Task<Result<AdminListingDetailDto>> Handle(
        GetAdminListingQuery request, CancellationToken ct)
    {
        var v = await db.Vehicles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.Images)
            .Include(x => x.Equipments)
            .Include(x => x.Seller)
            .FirstOrDefaultAsync(x => x.Id == request.VehicleId && x.DeletedAt == null, ct);

        if (v is null) return Result<AdminListingDetailDto>.Failure("Vehicle.NotFound");

        var row = new AdminListingRowDto(
            v.Id, v.PublicReference, v.Slug, v.Title, v.Status,
            v.AdminHiddenAt is not null, v.AdminFlaggedAt is not null,
            v.Price, v.City, v.SellerId,
            v.Seller?.DisplayName ?? "—",
            v.Seller?.AccountType ?? AccountType.Particulier,
            v.ViewsCount, v.FavoritesCount,
            ListingQualityCalculator.For(v).Score,
            await db.Reports.CountAsync(r =>
                r.TargetType == ReportTargetType.Listing && r.TargetId == v.Id
                && (r.Status == ReportStatus.Nouveau || r.Status == ReportStatus.EnExamen), ct),
            v.CreatedAt, v.PublishedAt);

        var priceHistory = await db.VehiclePriceHistories
            .AsNoTracking()
            .Where(h => h.VehicleId == v.Id)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new AdminPriceHistoryDto(h.Price, h.ChangedAt))
            .ToListAsync(ct);

        var actions = await db.AdminActions
            .AsNoTracking()
            .Where(a => a.TargetType == AdminTargetType.Listing && a.TargetId == v.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new Users.AdminActionDto(
                a.Id, a.Type, a.Reason, a.Admin != null ? a.Admin.DisplayName : "—", a.CreatedAt))
            .ToListAsync(ct);

        var notes = await db.AdminNotes
            .AsNoTracking()
            .Where(n => n.TargetType == AdminTargetType.Listing && n.TargetId == v.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new Users.AdminNoteDto(
                n.Id, n.Body, n.Admin != null ? n.Admin.DisplayName : "—", n.CreatedAt))
            .ToListAsync(ct);

        var dto = new AdminListingDetailDto(
            row, v.UpdatedAt,
            v.Seller?.Phone ?? "—",
            v.ContactsCount,
            await db.Negotiations.AsNoTracking().CountAsync(n => n.VehicleId == v.Id, ct),
            await db.Offers.AsNoTracking()
                .CountAsync(o => o.Negotiation.VehicleId == v.Id, ct),
            ListingQualityCalculator.For(v),
            priceHistory, actions, notes);

        return Result<AdminListingDetailDto>.Success(dto);
    }
}
