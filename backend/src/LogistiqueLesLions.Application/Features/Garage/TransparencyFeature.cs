using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Garage;

// ─── Configuración (propietario) ───────────────────────────────────────────

/// <summary>Lo que quien vende ve al decidir qué historial compartir.</summary>
public record GetTransparencySettingsQuery(Guid UserId, Guid VehicleId)
    : IRequest<Result<TransparencySettingsDto>>;

public record TransparencySettingsDto(
    Guid VehicleId,
    bool ShowMaintenanceHistory,
    bool ShowMaintenanceDetails,
    bool ShowMileageEvolution,
    IReadOnlyList<TransparencyRecordDto> Records
);

/// <param name="HasInvoice">La intervención tiene factura enlazada que podría compartirse.</param>
public record TransparencyRecordDto(
    Guid MaintenanceRecordId,
    MaintenanceType Type,
    DateTimeOffset PerformedAt,
    int? Mileage,
    string Description,
    bool HasInvoice,
    bool Shared,
    bool ShareInvoice
);

/// <summary>Guarda la selección. Todo lo que no se marque deja de compartirse.</summary>
public record SaveTransparencySettingsCommand(
    Guid UserId,
    Guid VehicleId,
    bool ShowMaintenanceHistory,
    bool ShowMaintenanceDetails,
    bool ShowMileageEvolution,
    IReadOnlyList<SharedRecordInput> Records
) : IRequest<Result>;

public record SharedRecordInput(Guid MaintenanceRecordId, bool Shared, bool ShareInvoice);

// ─── Lectura pública (anuncio) ─────────────────────────────────────────────

/// <summary>
/// «Transparence du véhicule» tal y como la ve quien mira el anuncio.
/// </summary>
/// <remarks>
/// Solo devuelve lo que quien vende ha marcado expresamente. Si no ha compartido nada,
/// no hay bloque que mostrar.
/// </remarks>
public record GetVehicleTransparencyQuery(Guid VehicleId)
    : IRequest<Result<PublicTransparencyDto?>>;

/// <param name="MaintenanceCount">«7 entretiens enregistrés sur Yoon u Auto».</param>
public record PublicTransparencyDto(
    int MaintenanceCount,
    bool ShowDetails,
    IReadOnlyList<PublicMaintenanceDto> Records,
    IReadOnlyList<PublicMileagePointDto> MileageEvolution
);

public record PublicMaintenanceDto(
    MaintenanceType Type,
    string Description,
    DateTimeOffset? PerformedAt,
    int? Mileage,
    /// <summary>Identificador del documento compartido, para descargarlo.</summary>
    Guid? InvoiceDocumentId
);

public record PublicMileagePointDto(DateTimeOffset Date, int Mileage);

/// <summary>Descarga pública de una factura que se ha compartido expresamente.</summary>
public record GetSharedInvoiceQuery(Guid VehicleId, Guid DocumentId)
    : IRequest<Result<GarageDocumentFileDto>>;

// ─── Handlers ──────────────────────────────────────────────────────────────

internal static class TransparencyWorkflow
{
    /// <summary>Carga la transparencia comprobando que el anuncio es del usuario.</summary>
    public static async Task<(VehicleTransparency? transparency, string? error)> LoadAsync(
        IApplicationDbContext db, Guid userId, Guid vehicleId, CancellationToken ct)
    {
        var transparency = await db.VehicleTransparencies
            .Include(t => t.SharedRecords)
            .Include(t => t.GarageVehicle)
            .FirstOrDefaultAsync(t => t.VehicleId == vehicleId, ct);

        if (transparency is null) return (null, "Transparency.NotFound");

        if (transparency.GarageVehicle.UserId != userId)
            return (null, "GarageVehicle.AccessDenied");

        return (transparency, null);
    }
}

public class GetTransparencySettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTransparencySettingsQuery, Result<TransparencySettingsDto>>
{
    public async Task<Result<TransparencySettingsDto>> Handle(
        GetTransparencySettingsQuery request, CancellationToken ct)
    {
        var (transparency, error) = await TransparencyWorkflow.LoadAsync(
            db, request.UserId, request.VehicleId, ct);

        if (error is not null) return Result<TransparencySettingsDto>.Failure(error);

        var records = await db.MaintenanceRecords
            .AsNoTracking()
            .Where(r => r.GarageVehicleId == transparency!.GarageVehicleId)
            .OrderByDescending(r => r.PerformedAt)
            .ToListAsync(ct);

        var shared = transparency!.SharedRecords.ToDictionary(s => s.MaintenanceRecordId);

        var dto = new TransparencySettingsDto(
            transparency.VehicleId,
            transparency.ShowMaintenanceHistory,
            transparency.ShowMaintenanceDetails,
            transparency.ShowMileageEvolution,
            records.Select(r => new TransparencyRecordDto(
                    r.Id, r.Type, r.PerformedAt, r.Mileage, r.Description,
                    r.DocumentId is not null,
                    shared.ContainsKey(r.Id),
                    shared.TryGetValue(r.Id, out var s) && s.ShareInvoice))
                .ToList());

        return Result<TransparencySettingsDto>.Success(dto);
    }
}

public class SaveTransparencySettingsCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SaveTransparencySettingsCommand, Result>
{
    public async Task<Result> Handle(SaveTransparencySettingsCommand request, CancellationToken ct)
    {
        var (transparency, error) = await TransparencyWorkflow.LoadAsync(
            db, request.UserId, request.VehicleId, ct);

        if (error is not null) return Result.Failure(error);

        transparency!.ShowMaintenanceHistory = request.ShowMaintenanceHistory;
        transparency.ShowMaintenanceDetails  = request.ShowMaintenanceDetails;
        transparency.ShowMileageEvolution    = request.ShowMileageEvolution;

        // Solo pueden compartirse intervenciones del propio vehículo.
        var own = await db.MaintenanceRecords
            .Where(r => r.GarageVehicleId == transparency.GarageVehicleId)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var wanted = request.Records
            .Where(r => r.Shared && own.Contains(r.MaintenanceRecordId))
            .ToDictionary(r => r.MaintenanceRecordId);

        // Lo que se desmarca deja de compartirse: retirar el permiso tiene que surtir
        // efecto de verdad, no quedarse en la pantalla.
        foreach (var existing in transparency.SharedRecords.ToList())
        {
            if (wanted.TryGetValue(existing.MaintenanceRecordId, out var keep))
            {
                existing.ShareInvoice = keep.ShareInvoice;
                wanted.Remove(existing.MaintenanceRecordId);
            }
            else
            {
                existing.DeletedAt = DateTimeOffset.UtcNow;
            }
        }

        foreach (var added in wanted.Values)
        {
            db.SharedMaintenanceRecords.Add(new SharedMaintenanceRecord
            {
                TransparencyId      = transparency.Id,
                MaintenanceRecordId = added.MaintenanceRecordId,
                ShareInvoice        = added.ShareInvoice
            });
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class GetVehicleTransparencyQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVehicleTransparencyQuery, Result<PublicTransparencyDto?>>
{
    public async Task<Result<PublicTransparencyDto?>> Handle(
        GetVehicleTransparencyQuery request, CancellationToken ct)
    {
        var transparency = await db.VehicleTransparencies
            .AsNoTracking()
            .Include(t => t.SharedRecords)
            .FirstOrDefaultAsync(t => t.VehicleId == request.VehicleId, ct);

        // Sin historial compartido no hay bloque que mostrar.
        if (transparency is null || !transparency.ShowMaintenanceHistory)
            return Result<PublicTransparencyDto?>.Success(null);

        var sharedIds = transparency.SharedRecords.Select(s => s.MaintenanceRecordId).ToList();

        var records = await db.MaintenanceRecords
            .AsNoTracking()
            .Where(r => sharedIds.Contains(r.Id))
            .OrderByDescending(r => r.PerformedAt)
            .ToListAsync(ct);

        var shareInvoice = transparency.SharedRecords
            .Where(s => s.ShareInvoice)
            .Select(s => s.MaintenanceRecordId)
            .ToHashSet();

        var visible = records
            .Select(r => new PublicMaintenanceDto(
                r.Type,
                r.Description,
                // Las fechas y el kilometraje son una decisión aparte: se puede enseñar
                // que hay historial sin detallar cuándo ni con cuántos kilómetros.
                transparency.ShowMaintenanceDetails ? r.PerformedAt : null,
                transparency.ShowMaintenanceDetails ? r.Mileage : null,
                shareInvoice.Contains(r.Id) ? r.DocumentId : null))
            .ToList();

        var evolution = new List<PublicMileagePointDto>();
        if (transparency.ShowMileageEvolution)
        {
            // La evolución sale de las lecturas ya registradas: no se estima nada.
            evolution = records
                .Where(r => r.Mileage is not null)
                .OrderBy(r => r.PerformedAt)
                .Select(r => new PublicMileagePointDto(r.PerformedAt, r.Mileage!.Value))
                .ToList();
        }

        // El contador cuenta el historial entero, aunque solo se detallen algunas.
        var total = await db.MaintenanceRecords
            .CountAsync(r => r.GarageVehicleId == transparency.GarageVehicleId, ct);

        return Result<PublicTransparencyDto?>.Success(new PublicTransparencyDto(
            total, transparency.ShowMaintenanceDetails, visible, evolution));
    }
}

/// <remarks>
/// La factura sigue siendo un documento privado: solo se sirve si su intervención está
/// compartida <b>y</b> se ha marcado expresamente compartir el papel.
/// </remarks>
public class GetSharedInvoiceQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSharedInvoiceQuery, Result<GarageDocumentFileDto>>
{
    public async Task<Result<GarageDocumentFileDto>> Handle(
        GetSharedInvoiceQuery request, CancellationToken ct)
    {
        var transparency = await db.VehicleTransparencies
            .AsNoTracking()
            .Include(t => t.SharedRecords)
            .FirstOrDefaultAsync(t => t.VehicleId == request.VehicleId, ct);

        if (transparency is null || !transparency.ShowMaintenanceHistory)
            return Result<GarageDocumentFileDto>.Failure("Transparency.NotShared");

        var allowed = transparency.SharedRecords
            .Where(s => s.ShareInvoice)
            .Select(s => s.MaintenanceRecordId)
            .ToList();

        var isShared = await db.MaintenanceRecords
            .AnyAsync(r => allowed.Contains(r.Id) && r.DocumentId == request.DocumentId, ct);

        if (!isShared) return Result<GarageDocumentFileDto>.Failure("Transparency.NotShared");

        var document = await db.GarageDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct);

        if (document is null) return Result<GarageDocumentFileDto>.Failure("GarageDocument.NotFound");

        return Result<GarageDocumentFileDto>.Success(new GarageDocumentFileDto(
            document.StorageKey, document.FileName, document.ContentType));
    }
}
