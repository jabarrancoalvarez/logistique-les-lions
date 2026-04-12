namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Registro inmutable de cambios sobre entidades de dominio.
/// Se inserta automáticamente por el AuditLogInterceptor antes de SaveChanges.
/// No hereda de AuditableEntity para evitar recursión y mantenerlo append-only.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Nombre de la entidad afectada (ej. "Vehicle").</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Clave primaria de la entidad afectada, serializada.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Created | Updated | Deleted | SoftDeleted</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Snapshot JSON de los valores anteriores (null en Create).</summary>
    public string? OldValues { get; set; }

    /// <summary>Snapshot JSON de los valores nuevos (null en Delete duro).</summary>
    public string? NewValues { get; set; }

    /// <summary>Lista CSV de propiedades modificadas en Update.</summary>
    public string? ChangedColumns { get; set; }

    /// <summary>Usuario autenticado que provocó el cambio (puede ser null en jobs).</summary>
    public Guid? UserId { get; set; }

    public string? UserEmail { get; set; }

    public string? CorrelationId { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
