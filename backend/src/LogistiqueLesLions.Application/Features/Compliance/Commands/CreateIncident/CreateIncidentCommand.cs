using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Compliance.Commands.CreateIncident;

public record CreateIncidentCommand(
    Guid ProcessId,
    string Title,
    string? Description,
    IncidentSeverity Severity = IncidentSeverity.Medium,
    Guid? AssignedToUserId = null
) : IRequest<Result<Guid>>;

public class CreateIncidentCommandHandler(
    IApplicationDbContext db,
    IWebhookPublisher webhooks)
    : IRequestHandler<CreateIncidentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateIncidentCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<Guid>.Failure("Incident.TitleRequired");

        var process = await db.ImportExportProcesses
            .FirstOrDefaultAsync(p => p.Id == request.ProcessId, ct);
        if (process is null)
            return Result<Guid>.Failure("Process.NotFound");

        var incident = new ProcessIncident
        {
            ProcessId        = request.ProcessId,
            Title            = request.Title.Trim(),
            Description      = request.Description?.Trim(),
            Severity         = request.Severity,
            Status           = IncidentStatus.Open,
            AssignedToUserId = request.AssignedToUserId
        };

        db.ProcessIncidents.Add(incident);
        await db.SaveChangesAsync(ct);

        await webhooks.PublishAsync("incident.created", new
        {
            id           = incident.Id,
            processId    = incident.ProcessId,
            trackingCode = process.TrackingCode,
            title        = incident.Title,
            severity     = incident.Severity.ToString(),
            assignedToUserId = incident.AssignedToUserId
        }, ct);

        return Result<Guid>.Success(incident.Id);
    }
}
