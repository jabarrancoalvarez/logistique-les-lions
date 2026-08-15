using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Dashboard;

/// <summary>
/// «Tableau de bord» del backoffice.
/// </summary>
/// <remarks>
/// El objetivo no es un sistema de Business Intelligence, sino que el administrador sepa
/// de un vistazo qué está ocurriendo en la plataforma. Por eso son conteos directos y no
/// series históricas ni cubos agregados.
/// </remarks>
public record GetAdminDashboardQuery : IRequest<Result<AdminDashboardDto>>;

public record AdminDashboardDto(
    AdminUserStatsDto Users,
    AdminMarketplaceStatsDto Marketplace,
    AdminActivityStatsDto Activity,
    AdminDemandStatsDto Demand,
    AdminGarageStatsDto Garage
);

public record AdminUserStatsDto(
    int Total,
    int NewToday,
    int NewLast7Days,
    int NewLast30Days,
    int Particuliers,
    int Professionnels,
    int PhoneVerified
);

public record AdminMarketplaceStatsDto(
    int Active,
    int NewLast7Days,
    int NewLast30Days,
    int Reserved,
    int Sold,
    int Drafts,
    int Paused,
    int Archived,
    /// <summary>Anuncios con signalements abiertos, a la espera de revisión.</summary>
    int PendingModeration
);

/// <param name="MessagesSent">Últimos 30 días: el total histórico no dice nada del pulso actual.</param>
public record AdminActivityStatsDto(
    int NegotiationsStarted,
    int NegotiationsActive,
    int MessagesSent,
    int OffersMade,
    int OffersAccepted,
    int ContractsCreated,
    int ContractsValidated,
    int VerifiedSales
);

public record AdminDemandStatsDto(
    int SavedSearches,
    int SavedSearchesWithAlert,
    int FavoritesTotal,
    int RequestsPending,
    int RequestsSearching,
    /// <summary>Modelos más añadidos a Favoris.</summary>
    IReadOnlyList<ModelDemandDto> TopFavoritedModels,
    /// <summary>Modelos más pedidos en «Trouvez-moi cette voiture».</summary>
    IReadOnlyList<ModelDemandDto> TopRequestedModels
);

public record ModelDemandDto(string Label, int Count);

public record AdminGarageStatsDto(
    int VehiclesTotal,
    /// <summary>Incorporados tras una compra en Yoon u Auto.</summary>
    int FromYoonUAuto,
    /// <summary>Añadidos a mano por el usuario.</summary>
    int AddedManually,
    /// <summary>Que luego se han convertido en anuncio.</summary>
    int ConvertedToListings
);

public class GetAdminDashboardQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminDashboardQuery, Result<AdminDashboardDto>>
{
    /// <summary>Cuántos modelos se listan en los rankings de demanda.</summary>
    private const int TopModels = 5;

    public async Task<Result<AdminDashboardDto>> Handle(
        GetAdminDashboardQuery request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var today = now.Date;
        var last7 = now.AddDays(-7);
        var last30 = now.AddDays(-30);

        var users = await UsersAsync(today, last7, last30, ct);
        var marketplace = await MarketplaceAsync(last7, last30, ct);
        var activity = await ActivityAsync(last30, ct);
        var demand = await DemandAsync(ct);
        var garage = await GarageAsync(ct);

        return Result<AdminDashboardDto>.Success(
            new AdminDashboardDto(users, marketplace, activity, demand, garage));
    }

    private async Task<AdminUserStatsDto> UsersAsync(
        DateTime today, DateTimeOffset last7, DateTimeOffset last30, CancellationToken ct)
    {
        var users = db.UserProfiles.AsNoTracking();

        return new AdminUserStatsDto(
            await users.CountAsync(ct),
            await users.CountAsync(u => u.CreatedAt >= today, ct),
            await users.CountAsync(u => u.CreatedAt >= last7, ct),
            await users.CountAsync(u => u.CreatedAt >= last30, ct),
            await users.CountAsync(u => u.AccountType == AccountType.Particulier, ct),
            await users.CountAsync(u => u.AccountType == AccountType.Professionnel, ct),
            await users.CountAsync(u => u.PhoneVerified, ct));
    }

    private async Task<AdminMarketplaceStatsDto> MarketplaceAsync(
        DateTimeOffset last7, DateTimeOffset last30, CancellationToken ct)
    {
        // Sin el filtro global: al administrador le interesan también los borradores y
        // los archivados, que son parte del estado de la plataforma.
        var vehicles = db.Vehicles.AsNoTracking().IgnoreQueryFilters().Where(v => v.DeletedAt == null);

        return new AdminMarketplaceStatsDto(
            await vehicles.CountAsync(v => v.Status == VehicleStatus.Actif, ct),
            // «Nuevos anuncios» son los publicados, no los borradores creados.
            await vehicles.CountAsync(v => v.PublishedAt >= last7, ct),
            await vehicles.CountAsync(v => v.PublishedAt >= last30, ct),
            await vehicles.CountAsync(v => v.Status == VehicleStatus.Reserve, ct),
            await vehicles.CountAsync(v => v.Status == VehicleStatus.Vendu, ct),
            await vehicles.CountAsync(v => v.Status == VehicleStatus.Brouillon, ct),
            await vehicles.CountAsync(v => v.Status == VehicleStatus.EnPause, ct),
            await vehicles.CountAsync(v => v.Status == VehicleStatus.Archive, ct),
            await db.Reports.AsNoTracking()
                .Where(r => r.TargetType == ReportTargetType.Listing
                         && (r.Status == ReportStatus.Nouveau || r.Status == ReportStatus.EnExamen))
                .Select(r => r.TargetId)
                .Distinct()
                .CountAsync(ct));
    }

    private async Task<AdminActivityStatsDto> ActivityAsync(DateTimeOffset last30, CancellationToken ct)
    {
        return new AdminActivityStatsDto(
            await db.Negotiations.AsNoTracking().CountAsync(n => n.CreatedAt >= last30, ct),
            await db.Negotiations.AsNoTracking()
                .CountAsync(n => n.Status != NegotiationStatus.Terminee, ct),
            await db.Messages.AsNoTracking().CountAsync(m => m.CreatedAt >= last30, ct),
            await db.Offers.AsNoTracking().CountAsync(o => o.CreatedAt >= last30, ct),
            await db.Offers.AsNoTracking()
                .CountAsync(o => o.Status == OfferStatus.Acceptee && o.CreatedAt >= last30, ct),
            await db.Contracts.AsNoTracking().CountAsync(ct),
            await db.Contracts.AsNoTracking().CountAsync(c => c.Status == ContractStatus.Valide, ct),
            // La venta verificada es exactamente el contrato validado: no se cuenta por
            // otro camino para que las dos cifras no puedan discrepar.
            await db.Contracts.AsNoTracking().CountAsync(c => c.Status == ContractStatus.Valide, ct));
    }

    private async Task<AdminDemandStatsDto> DemandAsync(CancellationToken ct)
    {
        var savedSearches = db.SavedSearches.AsNoTracking();

        var topFavorited = await db.SavedVehicles
            .AsNoTracking()
            .Join(db.Vehicles.AsNoTracking(), s => s.VehicleId, v => v.Id, (s, v) => v)
            .Where(v => v.ModelId != null)
            .GroupBy(v => new { v.Make.Name, ModelName = v.Model!.Name })
            .Select(g => new { g.Key.Name, g.Key.ModelName, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(TopModels)
            .ToListAsync(ct);

        // Las solicitudes guardan el modelo como texto libre: quien pide un coche no
        // siempre encuentra el modelo exacto en el catálogo.
        var topRequested = await db.VehicleRequests
            .AsNoTracking()
            .Where(r => r.ModelName != null)
            .GroupBy(r => new { MakeName = r.Make!.Name, r.ModelName })
            .Select(g => new { g.Key.MakeName, g.Key.ModelName, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(TopModels)
            .ToListAsync(ct);

        return new AdminDemandStatsDto(
            await savedSearches.CountAsync(ct),
            await savedSearches.CountAsync(s => s.AlertEnabled, ct),
            await db.SavedVehicles.AsNoTracking().CountAsync(ct),
            await db.VehicleRequests.AsNoTracking()
                .CountAsync(r => r.Status == VehicleRequestStatus.NouvelleDemande, ct),
            await db.VehicleRequests.AsNoTracking()
                .CountAsync(r => r.Status == VehicleRequestStatus.EnRecherche, ct),
            topFavorited
                .Select(x => new ModelDemandDto($"{x.Name} {x.ModelName}", x.Count))
                .ToList(),
            topRequested
                .Select(x => new ModelDemandDto($"{x.MakeName} {x.ModelName}", x.Count))
                .ToList());
    }

    private async Task<AdminGarageStatsDto> GarageAsync(CancellationToken ct)
    {
        var garage = db.GarageVehicles.AsNoTracking();

        return new AdminGarageStatsDto(
            await garage.CountAsync(ct),
            await garage.CountAsync(v => v.SourceContractId != null, ct),
            await garage.CountAsync(v => v.SourceContractId == null, ct),
            await garage.CountAsync(v => v.ListedVehicleId != null, ct));
    }
}
