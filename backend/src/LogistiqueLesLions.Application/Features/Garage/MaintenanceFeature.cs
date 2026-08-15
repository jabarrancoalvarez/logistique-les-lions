using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Garage;

// ─── Comandos ──────────────────────────────────────────────────────────────

/// <summary>Datos de una intervención. Solo el tipo, la fecha y la descripción son obligatorios.</summary>
public record MaintenanceInput(
    MaintenanceType Type,
    DateTimeOffset PerformedAt,
    int? Mileage,
    string Description,
    decimal? Cost,
    string? Workshop,
    string? Notes,
    /// <summary>Factura ya subida a Documents, si la hay.</summary>
    Guid? DocumentId
);

public record AddMaintenanceRecordCommand(Guid UserId, Guid GarageVehicleId, MaintenanceInput Record)
    : IRequest<Result<Guid>>;

/// <summary>Corregir una entrada. La traza de creación y modificación se conserva.</summary>
public record UpdateMaintenanceRecordCommand(Guid UserId, Guid RecordId, MaintenanceInput Record)
    : IRequest<Result>;

public record DeleteMaintenanceRecordCommand(Guid UserId, Guid RecordId) : IRequest<Result>;

/// <summary>Alta de una fotografía ya subida al almacenamiento privado.</summary>
public record AddMaintenanceImageCommand(
    Guid UserId,
    Guid RecordId,
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes
) : IRequest<Result<Guid>>;

public record DeleteMaintenanceImageCommand(Guid UserId, Guid ImageId) : IRequest<Result>;

// ─── Consultas ─────────────────────────────────────────────────────────────

/// <summary>Historial de mantenimiento de un vehículo, agrupado por año.</summary>
public record GetMaintenanceHistoryQuery(Guid UserId, Guid GarageVehicleId)
    : IRequest<Result<MaintenanceHistoryDto>>;

/// <param name="Years">De más reciente a más antiguo, como en la especificación.</param>
/// <param name="LastMileage">
/// Kilometraje de la última intervención registrada. Es lo que la ficha muestra como
/// «Dernière vidange: 145.320 km».
/// </param>
public record MaintenanceHistoryDto(
    int RecordCount,
    decimal TotalCost,
    int? LastMileage,
    IReadOnlyList<MaintenanceYearDto> Years
);

public record MaintenanceYearDto(int Year, IReadOnlyList<MaintenanceRecordDto> Records);

public record MaintenanceRecordDto(
    Guid Id,
    MaintenanceType Type,
    DateTimeOffset PerformedAt,
    int? Mileage,
    string Description,
    decimal? Cost,
    string? Workshop,
    string? Notes,
    /// <summary>«Facture disponible ✓».</summary>
    bool HasInvoice,
    Guid? DocumentId,
    IReadOnlyList<MaintenanceImageDto> Images,
    DateTimeOffset CreatedAt,
    /// <summary>Última corrección. Igual a <c>CreatedAt</c> si nunca se ha tocado.</summary>
    DateTimeOffset UpdatedAt
);

/// <remarks>Sin la clave del almacenamiento: la foto se sirve por su propio endpoint.</remarks>
public record MaintenanceImageDto(Guid Id, string FileName, long SizeBytes);

/// <summary>Datos necesarios para servir la fotografía de una intervención.</summary>
public record GetMaintenanceImageFileQuery(Guid UserId, Guid ImageId)
    : IRequest<Result<GarageDocumentFileDto>>;

// ─── Handlers ──────────────────────────────────────────────────────────────

internal static class MaintenanceWorkflow
{
    public static string? Validate(MaintenanceInput r)
    {
        if (string.IsNullOrWhiteSpace(r.Description)) return "Maintenance.DescriptionRequired";
        if (r.Mileage is < 0) return "Maintenance.InvalidMileage";
        if (r.Cost is < 0) return "Maintenance.InvalidCost";

        // Una intervención es algo que ya se ha hecho; lo que viene es un rappel.
        if (r.PerformedAt > DateTimeOffset.UtcNow.AddDays(1))
            return "Maintenance.DateInFuture";

        return null;
    }

    public static void Apply(MaintenanceRecord entity, MaintenanceInput r)
    {
        entity.Type        = r.Type;
        entity.PerformedAt = r.PerformedAt;
        entity.Mileage     = r.Mileage;
        entity.Description = r.Description.Trim();
        entity.Cost        = r.Cost;
        entity.Workshop    = GarageWorkflow.Clean(r.Workshop);
        entity.Notes       = GarageWorkflow.Clean(r.Notes);
        entity.DocumentId  = r.DocumentId;
    }

    /// <summary>Carga la intervención comprobando que el vehículo es del usuario.</summary>
    public static async Task<(MaintenanceRecord? record, string? error)> LoadAsync(
        IApplicationDbContext db, Guid userId, Guid recordId, CancellationToken ct)
    {
        var record = await db.MaintenanceRecords
            .Include(r => r.GarageVehicle)
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == recordId, ct);

        if (record is null) return (null, "Maintenance.NotFound");

        if (record.GarageVehicle.UserId != userId)
            return (null, "GarageVehicle.AccessDenied");

        return (record, null);
    }

    /// <summary>
    /// La factura enlazada tiene que ser un documento del mismo vehículo.
    /// </summary>
    /// <remarks>
    /// Sin esta comprobación, un usuario podría enlazar el documento de otro vehículo
    /// —o de otra cuenta— y verlo desde aquí.
    /// </remarks>
    public static async Task<bool> DocumentBelongsAsync(
        IApplicationDbContext db, Guid? documentId, Guid garageVehicleId, CancellationToken ct) =>
        documentId is null ||
        await db.GarageDocuments.AnyAsync(
            d => d.Id == documentId && d.GarageVehicleId == garageVehicleId, ct);
}

public class AddMaintenanceRecordCommandHandler(
    IApplicationDbContext db,
    IReminderService? reminders = null)
    : IRequestHandler<AddMaintenanceRecordCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddMaintenanceRecordCommand request, CancellationToken ct)
    {
        var (vehicle, error) = await GarageWorkflow.LoadAsync(
            db, request.UserId, request.GarageVehicleId, ct);
        if (error is not null) return Result<Guid>.Failure(error);

        var invalid = MaintenanceWorkflow.Validate(request.Record);
        if (invalid is not null) return Result<Guid>.Failure(invalid);

        if (!await MaintenanceWorkflow.DocumentBelongsAsync(
                db, request.Record.DocumentId, vehicle!.Id, ct))
            return Result<Guid>.Failure("GarageDocument.NotFound");

        var record = new MaintenanceRecord { GarageVehicleId = vehicle.Id };
        MaintenanceWorkflow.Apply(record, request.Record);

        db.MaintenanceRecords.Add(record);

        // Una intervención posterior al último kilometraje conocido lo pone al día:
        // es la lectura más reciente que tenemos del vehículo.
        var mileageAdvanced = request.Record.Mileage is { } mileage && mileage > (vehicle.Mileage ?? 0);
        if (mileageAdvanced)
        {
            vehicle.Mileage = request.Record.Mileage;
            vehicle.MileageUpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        // Declarar kilómetros nuevos puede hacer vencer un recordatorio por kilometraje.
        if (mileageAdvanced && reminders is not null)
            await reminders.EvaluateVehicleAsync(vehicle.Id, ct);

        return Result<Guid>.Success(record.Id);
    }
}

public class UpdateMaintenanceRecordCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateMaintenanceRecordCommand, Result>
{
    public async Task<Result> Handle(UpdateMaintenanceRecordCommand request, CancellationToken ct)
    {
        var (record, error) = await MaintenanceWorkflow.LoadAsync(db, request.UserId, request.RecordId, ct);
        if (error is not null) return Result.Failure(error);

        var invalid = MaintenanceWorkflow.Validate(request.Record);
        if (invalid is not null) return Result.Failure(invalid);

        if (!await MaintenanceWorkflow.DocumentBelongsAsync(
                db, request.Record.DocumentId, record!.GarageVehicleId, ct))
            return Result.Failure("GarageDocument.NotFound");

        MaintenanceWorkflow.Apply(record, request.Record);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class DeleteMaintenanceRecordCommandHandler(
    IApplicationDbContext db,
    IStorageService storage)
    : IRequestHandler<DeleteMaintenanceRecordCommand, Result>
{
    public async Task<Result> Handle(DeleteMaintenanceRecordCommand request, CancellationToken ct)
    {
        var (record, error) = await MaintenanceWorkflow.LoadAsync(db, request.UserId, request.RecordId, ct);
        if (error is not null) return Result.Failure(error);

        var now = DateTimeOffset.UtcNow;
        record!.DeletedAt = now;
        foreach (var image in record.Images) image.DeletedAt = now;

        await db.SaveChangesAsync(ct);

        // La factura enlazada no se toca: vive en Documents y puede seguir haciendo falta.
        foreach (var image in record.Images)
            await storage.DeletePrivateAsync(image.StorageKey, ct);

        return Result.Success();
    }
}

public class AddMaintenanceImageCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AddMaintenanceImageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddMaintenanceImageCommand request, CancellationToken ct)
    {
        var (record, error) = await MaintenanceWorkflow.LoadAsync(db, request.UserId, request.RecordId, ct);
        if (error is not null) return Result<Guid>.Failure(error);

        var image = new MaintenanceRecordImage
        {
            MaintenanceRecordId = record!.Id,
            StorageKey          = request.StorageKey,
            FileName            = request.FileName,
            ContentType         = request.ContentType,
            SizeBytes           = request.SizeBytes
        };

        db.MaintenanceRecordImages.Add(image);
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(image.Id);
    }
}

public class DeleteMaintenanceImageCommandHandler(
    IApplicationDbContext db,
    IStorageService storage)
    : IRequestHandler<DeleteMaintenanceImageCommand, Result>
{
    public async Task<Result> Handle(DeleteMaintenanceImageCommand request, CancellationToken ct)
    {
        var image = await db.MaintenanceRecordImages
            .Include(i => i.MaintenanceRecord).ThenInclude(r => r.GarageVehicle)
            .FirstOrDefaultAsync(i => i.Id == request.ImageId, ct);

        if (image is null) return Result.Failure("Maintenance.ImageNotFound");

        if (image.MaintenanceRecord.GarageVehicle.UserId != request.UserId)
            return Result.Failure("GarageVehicle.AccessDenied");

        image.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await storage.DeletePrivateAsync(image.StorageKey, ct);

        return Result.Success();
    }
}

public class GetMaintenanceHistoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMaintenanceHistoryQuery, Result<MaintenanceHistoryDto>>
{
    public async Task<Result<MaintenanceHistoryDto>> Handle(
        GetMaintenanceHistoryQuery request, CancellationToken ct)
    {
        var owns = await db.GarageVehicles
            .AnyAsync(v => v.Id == request.GarageVehicleId && v.UserId == request.UserId, ct);

        if (!owns)
        {
            var exists = await db.GarageVehicles.AnyAsync(v => v.Id == request.GarageVehicleId, ct);
            return Result<MaintenanceHistoryDto>.Failure(
                exists ? "GarageVehicle.AccessDenied" : "GarageVehicle.NotFound");
        }

        var records = await db.MaintenanceRecords
            .AsNoTracking()
            .Include(r => r.Images)
            .Where(r => r.GarageVehicleId == request.GarageVehicleId)
            .OrderByDescending(r => r.PerformedAt)
            .ToListAsync(ct);

        // Agrupado por año, de más reciente a más antiguo: «2026 → Vidange · 145.320 km».
        var years = records
            .GroupBy(r => r.PerformedAt.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => new MaintenanceYearDto(
                g.Key,
                g.Select(r => new MaintenanceRecordDto(
                        r.Id, r.Type, r.PerformedAt, r.Mileage, r.Description, r.Cost,
                        r.Workshop, r.Notes, r.DocumentId is not null, r.DocumentId,
                        r.Images
                            .Select(i => new MaintenanceImageDto(i.Id, i.FileName, i.SizeBytes))
                            .ToList(),
                        r.CreatedAt, r.UpdatedAt))
                    .ToList()))
            .ToList();

        var dto = new MaintenanceHistoryDto(
            records.Count,
            records.Sum(r => r.Cost ?? 0m),
            records.Select(r => r.Mileage).FirstOrDefault(m => m is not null),
            years);

        return Result<MaintenanceHistoryDto>.Success(dto);
    }
}

public class GetMaintenanceImageFileQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMaintenanceImageFileQuery, Result<GarageDocumentFileDto>>
{
    public async Task<Result<GarageDocumentFileDto>> Handle(
        GetMaintenanceImageFileQuery request, CancellationToken ct)
    {
        var image = await db.MaintenanceRecordImages
            .AsNoTracking()
            .Include(i => i.MaintenanceRecord).ThenInclude(r => r.GarageVehicle)
            .FirstOrDefaultAsync(i => i.Id == request.ImageId, ct);

        if (image is null) return Result<GarageDocumentFileDto>.Failure("Maintenance.ImageNotFound");

        if (image.MaintenanceRecord.GarageVehicle.UserId != request.UserId)
            return Result<GarageDocumentFileDto>.Failure("GarageVehicle.AccessDenied");

        return Result<GarageDocumentFileDto>.Success(new GarageDocumentFileDto(
            image.StorageKey, image.FileName, image.ContentType));
    }
}
