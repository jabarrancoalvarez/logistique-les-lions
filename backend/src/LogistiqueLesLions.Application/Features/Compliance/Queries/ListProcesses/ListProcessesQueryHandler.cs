using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.ListProcesses;

public class ListProcessesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<ListProcessesQuery, Result<IReadOnlyList<ProcessListItemDto>>>
{
    public async Task<Result<IReadOnlyList<ProcessListItemDto>>> Handle(
        ListProcessesQuery request, CancellationToken cancellationToken)
    {
        var items = await context.ImportExportProcesses
            .AsNoTracking()
            .Include(p => p.Vehicle)
            .OrderByDescending(p => p.CreatedAt)
            .Take(200)
            .Select(p => new ProcessListItemDto(
                p.Id,
                p.VehicleId,
                p.Vehicle.Title,
                p.OriginCountry,
                p.DestinationCountry,
                p.ProcessType.ToString(),
                p.Status.ToString(),
                p.CompletionPercent,
                p.EstimatedCostEur ?? 0m,
                p.StartedAt,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProcessListItemDto>>.Success(items);
    }
}
