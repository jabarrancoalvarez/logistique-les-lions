using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Users;

/// <summary>Listado de usuarios del backoffice, con búsqueda y filtros.</summary>
public record GetAdminUsersQuery(
    string? Search = null,
    string? City = null,
    AccountType? AccountType = null,
    bool? PhoneVerified = null,
    AccountStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<AdminUserListDto>>;

public record AdminUserListDto(int TotalCount, int Page, int PageSize, IReadOnlyList<AdminUserRowDto> Items);

public record AdminUserRowDto(
    Guid Id,
    string DisplayName,
    string Phone,
    bool PhoneVerified,
    string? Email,
    string? City,
    AccountType AccountType,
    AccountStatus Status,
    DateTimeOffset? SuspendedUntil,
    UserRole Role,
    int ListingsCount,
    int VerifiedSalesCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt
);

public class GetAdminUsersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminUsersQuery, Result<AdminUserListDto>>
{
    public async Task<Result<AdminUserListDto>> Handle(
        GetAdminUsersQuery request, CancellationToken ct)
    {
        var query = db.UserProfiles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // Una sola caja para nombre, teléfono y correo: quien busca a alguien no
            // siempre sabe con cuál de los tres dará.
            var term = request.Search.Trim().ToLower();
            query = query.Where(u =>
                u.DisplayName.ToLower().Contains(term)
                || u.Phone.Contains(term)
                || (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim().ToLower();
            query = query.Where(u => u.City != null && u.City.ToLower() == city);
        }

        if (request.AccountType is { } accountType)
            query = query.Where(u => u.AccountType == accountType);

        if (request.PhoneVerified is { } verified)
            query = query.Where(u => u.PhoneVerified == verified);

        if (request.Status is { } status)
            query = query.Where(u => u.Status == status);

        var total = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var ids = users.Select(u => u.Id).ToList();

        // Los anuncios se cuentan aparte para no repetir la subconsulta por fila.
        var listings = await db.Vehicles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(v => ids.Contains(v.SellerId) && v.DeletedAt == null)
            .GroupBy(v => v.SellerId)
            .Select(g => new { SellerId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var listingsByUser = listings.ToDictionary(x => x.SellerId, x => x.Count);

        var rows = users
            .Select(u => new AdminUserRowDto(
                u.Id, u.DisplayName, u.Phone, u.PhoneVerified, u.Email, u.City,
                u.AccountType, u.Status, u.SuspendedUntil, u.Role,
                listingsByUser.GetValueOrDefault(u.Id),
                u.VerifiedSalesCount, u.CreatedAt, u.LastLoginAt))
            .ToList();

        return Result<AdminUserListDto>.Success(
            new AdminUserListDto(total, page, pageSize, rows));
    }
}

/// <summary>Ficha administrativa de un usuario.</summary>
public record GetAdminUserQuery(Guid UserId) : IRequest<Result<AdminUserDetailDto>>;

public record AdminUserDetailDto(
    AdminUserRowDto Profile,
    string? Region,
    AdminUserActivityDto Activity,
    IReadOnlyList<AdminActionDto> Actions,
    IReadOnlyList<AdminNoteDto> Notes
);

/// <param name="GarageVehicles">
/// Solo el número. El contenido de Mon Garage es privado y no se abre desde aquí.
/// </param>
public record AdminUserActivityDto(
    int ListingsPublished,
    int ListingsSold,
    int Negotiations,
    int OffersMade,
    int Contracts,
    int VerifiedSales,
    int Requests,
    int GarageVehicles,
    /// <summary>Signalements recibidos y emitidos por este usuario.</summary>
    int ReportsReceived,
    int ReportsMade
);

public record AdminActionDto(
    Guid Id,
    AdminActionType Type,
    string? Reason,
    string AdminName,
    DateTimeOffset CreatedAt
);

public record AdminNoteDto(Guid Id, string Body, string AdminName, DateTimeOffset CreatedAt);

public class GetAdminUserQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminUserQuery, Result<AdminUserDetailDto>>
{
    public async Task<Result<AdminUserDetailDto>> Handle(
        GetAdminUserQuery request, CancellationToken ct)
    {
        var user = await db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null) return Result<AdminUserDetailDto>.Failure("User.NotFound");

        var listings = db.Vehicles.AsNoTracking().IgnoreQueryFilters()
            .Where(v => v.SellerId == user.Id && v.DeletedAt == null);

        var activity = new AdminUserActivityDto(
            await listings.CountAsync(v => v.PublishedAt != null, ct),
            await listings.CountAsync(v => v.Status == VehicleStatus.Vendu, ct),
            await db.Negotiations.AsNoTracking()
                .CountAsync(n => n.BuyerId == user.Id || n.SellerId == user.Id, ct),
            await db.Offers.AsNoTracking().CountAsync(o => o.FromUserId == user.Id, ct),
            await db.Contracts.AsNoTracking()
                .CountAsync(c => c.BuyerId == user.Id || c.SellerId == user.Id, ct),
            user.VerifiedSalesCount,
            await db.VehicleRequests.AsNoTracking().CountAsync(r => r.UserId == user.Id, ct),
            // Solo el recuento: la documentación y el historial de Mon Garage son
            // privados, y el backoffice no es una puerta trasera a ellos.
            await db.GarageVehicles.AsNoTracking().CountAsync(g => g.UserId == user.Id, ct),
            await db.Reports.AsNoTracking().CountAsync(r => r.ReportedUserId == user.Id, ct),
            await db.Reports.AsNoTracking().CountAsync(r => r.ReporterId == user.Id, ct));

        var actions = await db.AdminActions
            .AsNoTracking()
            .Where(a => a.TargetType == AdminTargetType.User && a.TargetId == user.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AdminActionDto(
                a.Id, a.Type, a.Reason,
                a.Admin != null ? a.Admin.DisplayName : "—",
                a.CreatedAt))
            .ToListAsync(ct);

        var notes = await db.AdminNotes
            .AsNoTracking()
            .Where(n => n.TargetType == AdminTargetType.User && n.TargetId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new AdminNoteDto(
                n.Id, n.Body,
                n.Admin != null ? n.Admin.DisplayName : "—",
                n.CreatedAt))
            .ToListAsync(ct);

        var listingsCount = await listings.CountAsync(ct);

        var profile = new AdminUserRowDto(
            user.Id, user.DisplayName, user.Phone, user.PhoneVerified, user.Email, user.City,
            user.AccountType, user.Status, user.SuspendedUntil, user.Role,
            listingsCount, user.VerifiedSalesCount, user.CreatedAt, user.LastLoginAt);

        return Result<AdminUserDetailDto>.Success(
            new AdminUserDetailDto(profile, user.Region, activity, actions, notes));
    }
}
