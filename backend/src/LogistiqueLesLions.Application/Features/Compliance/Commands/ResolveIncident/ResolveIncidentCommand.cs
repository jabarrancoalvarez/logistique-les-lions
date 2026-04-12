using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Commands.ResolveIncident;

public record ResolveIncidentCommand(Guid IncidentId, string? Resolution) : IRequest<Result>;

public class ResolveIncidentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ResolveIncidentCommand, Result>
{
    public async Task<Result> Handle(ResolveIncidentCommand request, CancellationToken ct)
    {
        var incident = await db.ProcessIncidents
            .FirstOrDefaultAsync(i => i.Id == request.IncidentId, ct);
        if (incident is null)
            return Result.Failure("Incident.NotFound");

        incident.Status     = IncidentStatus.Resolved;
        incident.ResolvedAt = DateTimeOffset.UtcNow;
        incident.Resolution = request.Resolution;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
