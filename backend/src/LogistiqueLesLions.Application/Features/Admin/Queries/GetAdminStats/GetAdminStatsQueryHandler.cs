using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Queries.GetAdminStats;

public class GetAdminStatsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminStatsQuery, Result<AdminStatsDto>>
{
    public async Task<Result<AdminStatsDto>> Handle(GetAdminStatsQuery request, CancellationToken ct)
    {
        var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var totalVehicles     = await db.Vehicles.CountAsync(ct);
        var activeListings    = await db.Vehicles.CountAsync(v => v.Status == VehicleStatus.Active, ct);
        var totalUsers        = await db.UserProfiles.CountAsync(ct);
        var newUsers          = await db.UserProfiles.CountAsync(u => u.CreatedAt >= monthStart, ct);
        var activeProcesses   = await db.ImportExportProcesses.CountAsync(p => p.Status == ProcessStatus.InProgress, ct);
        var completedProcs    = await db.ImportExportProcesses.CountAsync(p => p.Status == ProcessStatus.Completed, ct);
        var totalConvs        = await db.Conversations.CountAsync(ct);
        var totalValue        = await db.Vehicles
            .Where(v => v.Status == VehicleStatus.Active)
            .SumAsync(v => v.Price, ct);

        return Result<AdminStatsDto>.Success(new AdminStatsDto(
            totalVehicles, activeListings,
            totalUsers, newUsers,
            activeProcesses, completedProcs,
            totalConvs, totalValue));
    }
}
