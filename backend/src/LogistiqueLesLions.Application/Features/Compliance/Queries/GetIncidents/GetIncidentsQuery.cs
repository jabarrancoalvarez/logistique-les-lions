using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.GetIncidents;

public record GetIncidentsQuery(Guid? ProcessId = null, IncidentStatus? Status = null)
    : IRequest<Result<IReadOnlyList<IncidentDto>>>;

public record IncidentDto(
    Guid Id,
    Guid ProcessId,
    string TrackingCode,
    string Title,
    string? Description,
    string Severity,
    string Status,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

public class GetIncidentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetIncidentsQuery, Result<IReadOnlyList<IncidentDto>>>
{
    public async Task<Result<IReadOnlyList<IncidentDto>>> Handle(GetIncidentsQuery request, CancellationToken ct)
    {
        var q = db.ProcessIncidents.AsNoTracking();
        if (request.ProcessId.HasValue) q = q.Where(i => i.ProcessId == request.ProcessId.Value);
        if (request.Status.HasValue)    q = q.Where(i => i.Status == request.Status.Value);

        var items = await q
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => new IncidentDto(
                i.Id,
                i.ProcessId,
                i.Process.TrackingCode,
                i.Title,
                i.Description,
                i.Severity.ToString(),
                i.Status.ToString(),
                i.AssignedToUserId,
                i.CreatedAt,
                i.ResolvedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<IncidentDto>>.Success(items);
    }
}
