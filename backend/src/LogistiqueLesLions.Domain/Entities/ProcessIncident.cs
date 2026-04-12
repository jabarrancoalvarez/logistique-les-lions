using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Incidencia abierta sobre un proceso de tramitación: aduana parada, documento
/// rechazado, retraso en transporte, etc. Se asocia a un ImportExportProcess.
/// </summary>
public class ProcessIncident : AuditableEntity
{
    public Guid ProcessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public Guid? AssignedToUserId { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? Resolution { get; set; }

    public ImportExportProcess Process { get; set; } = null!;
}
