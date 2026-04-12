using System.Text.Json;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LogistiqueLesLions.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor de EF Core que escribe un AuditLog por cada cambio sobre
/// AuditableEntity (Created / Updated / SoftDeleted). Se ejecuta DESPUÉS
/// del AuditInterceptor de timestamps, así que lee los valores ya enriquecidos.
/// </summary>
public class AuditLogInterceptor(
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AppendAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AppendAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendAuditLogs(DbContext? context)
    {
        if (context is null) return;

        var correlationId = httpContextAccessor.HttpContext?.TraceIdentifier;
        var userId = currentUser.UserId;
        var userEmail = currentUser.Email;
        var now = DateTimeOffset.UtcNow;

        var entries = context.ChangeTracker.Entries<AuditableEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();

        var logs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            // Detectar soft-delete: el AuditInterceptor convierte Deleted en
            // Modified marcando DeletedAt. Si no hay otras props modificadas,
            // lo registramos como SoftDeleted.
            string action;
            if (entry.State == EntityState.Added)
            {
                action = "Created";
            }
            else
            {
                var deletedAtModified = entry.Property(nameof(AuditableEntity.DeletedAt)).IsModified
                                        && entry.Entity.DeletedAt.HasValue;
                action = deletedAtModified ? "SoftDeleted" : "Updated";
            }

            var (oldValues, newValues, changedColumns) = ExtractChanges(entry, action);

            logs.Add(new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id.ToString(),
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                ChangedColumns = changedColumns,
                UserId = userId,
                UserEmail = userEmail,
                CorrelationId = correlationId,
                Timestamp = now
            });
        }

        if (logs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(logs);
        }
    }

    private static (string? Old, string? New, string? Changed) ExtractChanges(
        EntityEntry<AuditableEntity> entry,
        string action)
    {
        // Solo serializar columnas escalares — evita ciclos con navegaciones
        var properties = entry.Properties
            .Where(p => !p.Metadata.IsShadowProperty())
            .ToList();

        if (action == "Created")
        {
            var snapshot = properties.ToDictionary(
                p => p.Metadata.Name,
                p => p.CurrentValue);
            return (null, JsonSerializer.Serialize(snapshot, JsonOptions), null);
        }

        var changed = properties
            .Where(p => p.IsModified)
            .ToList();

        if (changed.Count == 0)
        {
            return (null, null, null);
        }

        var oldDict = changed.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
        var newDict = changed.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        var columns = string.Join(",", changed.Select(p => p.Metadata.Name));

        return (
            JsonSerializer.Serialize(oldDict, JsonOptions),
            JsonSerializer.Serialize(newDict, JsonOptions),
            columns);
    }
}
