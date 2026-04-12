using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Queries.GetDashboardKpis;

public class GetDashboardKpisQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDashboardKpisQuery, Result<DashboardKpisDto>>
{
    public async Task<Result<DashboardKpisDto>> Handle(
        GetDashboardKpisQuery request,
        CancellationToken ct)
    {
        var now        = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var sixMonthsAgo = monthStart.AddMonths(-5);

        // 1. Procesos por estado — group by enum, traemos a memoria luego
        var processesByStatusRaw = await db.ImportExportProcesses
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var processesByStatus = processesByStatusRaw
            .Select(x => new StatusBucket(x.Status.ToString(), x.Count))
            .ToList();

        // 2. Vehículos por estado
        var vehiclesByStatusRaw = await db.Vehicles
            .GroupBy(v => v.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var vehiclesByStatus = vehiclesByStatusRaw
            .Select(x => new StatusBucket(x.Status.ToString(), x.Count))
            .ToList();

        // 3. Procesos por mes (últimos 6) — agrupamos en SQL por (year, month)
        var perMonthRaw = await db.ImportExportProcesses
            .Where(p => p.CreatedAt >= sixMonthsAgo)
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var processesPerMonth = Enumerable.Range(0, 6)
            .Select(offset =>
            {
                var bucket = monthStart.AddMonths(-5 + offset);
                var match  = perMonthRaw.FirstOrDefault(x => x.Year == bucket.Year && x.Month == bucket.Month);
                return new MonthBucket($"{bucket.Year:0000}-{bucket.Month:00}", match?.Count ?? 0);
            })
            .ToList();

        // 4. Lead time medio (días) para procesos completados con StartedAt y CompletedAt
        var completedWithDates = await db.ImportExportProcesses
            .Where(p => p.Status == ProcessStatus.Completed
                        && p.StartedAt != null
                        && p.CompletedAt != null)
            .Select(p => new { p.StartedAt, p.CompletedAt })
            .ToListAsync(ct);

        var averageLeadTimeDays = completedWithDates.Count == 0
            ? 0
            : completedWithDates.Average(x => (x.CompletedAt!.Value - x.StartedAt!.Value).TotalDays);

        // 5. Incidencias abiertas: cancelled + rejected (por revisar)
        var openIncidents = await db.ImportExportProcesses
            .CountAsync(p => p.Status == ProcessStatus.Cancelled || p.Status == ProcessStatus.Rejected, ct);

        // 6. Completados este mes
        var completedThisMonth = await db.ImportExportProcesses
            .CountAsync(p => p.Status == ProcessStatus.Completed
                             && p.CompletedAt != null
                             && p.CompletedAt >= monthStart, ct);

        return Result<DashboardKpisDto>.Success(new DashboardKpisDto(
            processesByStatus,
            vehiclesByStatus,
            processesPerMonth,
            Math.Round(averageLeadTimeDays, 1),
            openIncidents,
            completedThisMonth));
    }
}
