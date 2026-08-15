using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Garage;

// ─── Comandos ──────────────────────────────────────────────────────────────

/// <summary>Alta de un documento cuyo archivo ya está en el almacenamiento privado.</summary>
public record AddGarageDocumentCommand(
    Guid UserId,
    Guid GarageVehicleId,
    GarageDocumentType Type,
    string Name,
    DateTimeOffset? DocumentDate,
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Notes
) : IRequest<Result<Guid>>;

/// <summary>Corrige la clasificación del documento. El archivo no se sustituye.</summary>
public record UpdateGarageDocumentCommand(
    Guid UserId,
    Guid DocumentId,
    GarageDocumentType Type,
    string Name,
    DateTimeOffset? DocumentDate,
    string? Notes
) : IRequest<Result>;

public record DeleteGarageDocumentCommand(Guid UserId, Guid DocumentId) : IRequest<Result>;

// ─── Consultas ─────────────────────────────────────────────────────────────

/// <summary>Historial documental de un vehículo, en orden cronológico.</summary>
public record GetGarageDocumentsQuery(Guid UserId, Guid GarageVehicleId)
    : IRequest<Result<IReadOnlyList<GarageDocumentDto>>>;

/// <remarks>
/// No expone la clave del almacenamiento: el archivo se descarga por su propio endpoint,
/// que vuelve a comprobar de quién es.
/// </remarks>
public record GarageDocumentDto(
    Guid Id,
    GarageDocumentType Type,
    string Name,
    DateTimeOffset? DocumentDate,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Notes,
    DateTimeOffset UploadedAt
);

/// <summary>Datos necesarios para servir el archivo de un documento.</summary>
public record GetGarageDocumentFileQuery(Guid UserId, Guid DocumentId)
    : IRequest<Result<GarageDocumentFileDto>>;

public record GarageDocumentFileDto(string StorageKey, string FileName, string ContentType);

// ─── Handlers ──────────────────────────────────────────────────────────────

internal static class GarageDocumentWorkflow
{
    /// <summary>Carga el documento comprobando que el vehículo es del usuario.</summary>
    public static async Task<(GarageDocument? document, string? error)> LoadAsync(
        IApplicationDbContext db, Guid userId, Guid documentId, CancellationToken ct)
    {
        var document = await db.GarageDocuments
            .Include(d => d.GarageVehicle)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document is null) return (null, "GarageDocument.NotFound");

        // La documentación es privada: ningún otro usuario accede a ella.
        if (document.GarageVehicle.UserId != userId)
            return (null, "GarageVehicle.AccessDenied");

        return (document, null);
    }
}

public class AddGarageDocumentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AddGarageDocumentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddGarageDocumentCommand request, CancellationToken ct)
    {
        var (vehicle, error) = await GarageWorkflow.LoadAsync(
            db, request.UserId, request.GarageVehicleId, ct);
        if (error is not null) return Result<Guid>.Failure(error);

        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name)) return Result<Guid>.Failure("GarageDocument.NameRequired");

        var document = new GarageDocument
        {
            GarageVehicleId = vehicle!.Id,
            Type            = request.Type,
            Name            = name,
            DocumentDate    = request.DocumentDate,
            StorageKey      = request.StorageKey,
            FileName        = request.FileName,
            ContentType     = request.ContentType,
            SizeBytes       = request.SizeBytes,
            Notes           = GarageWorkflow.Clean(request.Notes)
        };

        db.GarageDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(document.Id);
    }
}

public class UpdateGarageDocumentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateGarageDocumentCommand, Result>
{
    public async Task<Result> Handle(UpdateGarageDocumentCommand request, CancellationToken ct)
    {
        var (document, error) = await GarageDocumentWorkflow.LoadAsync(
            db, request.UserId, request.DocumentId, ct);
        if (error is not null) return Result.Failure(error);

        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name)) return Result.Failure("GarageDocument.NameRequired");

        document!.Type         = request.Type;
        document.Name          = name;
        document.DocumentDate  = request.DocumentDate;
        document.Notes         = GarageWorkflow.Clean(request.Notes);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

/// <remarks>
/// El registro se conserva (soft delete) pero el archivo se borra de verdad: si el
/// usuario retira un documento con sus datos personales, guardarlo «por si acaso» sería
/// justo lo contrario de lo que ha pedido.
/// </remarks>
public class DeleteGarageDocumentCommandHandler(
    IApplicationDbContext db,
    IStorageService storage)
    : IRequestHandler<DeleteGarageDocumentCommand, Result>
{
    public async Task<Result> Handle(DeleteGarageDocumentCommand request, CancellationToken ct)
    {
        var (document, error) = await GarageDocumentWorkflow.LoadAsync(
            db, request.UserId, request.DocumentId, ct);
        if (error is not null) return Result.Failure(error);

        document!.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await storage.DeletePrivateAsync(document.StorageKey, ct);

        return Result.Success();
    }
}

public class GetGarageDocumentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetGarageDocumentsQuery, Result<IReadOnlyList<GarageDocumentDto>>>
{
    public async Task<Result<IReadOnlyList<GarageDocumentDto>>> Handle(
        GetGarageDocumentsQuery request, CancellationToken ct)
    {
        var owns = await db.GarageVehicles
            .AnyAsync(v => v.Id == request.GarageVehicleId && v.UserId == request.UserId, ct);

        if (!owns)
        {
            var exists = await db.GarageVehicles.AnyAsync(v => v.Id == request.GarageVehicleId, ct);
            return Result<IReadOnlyList<GarageDocumentDto>>.Failure(
                exists ? "GarageVehicle.AccessDenied" : "GarageVehicle.NotFound");
        }

        var documents = await db.GarageDocuments
            .AsNoTracking()
            .Where(d => d.GarageVehicleId == request.GarageVehicleId)
            .ToListAsync(ct);

        // Orden cronológico: manda la fecha del documento y, si no la tiene, la de subida.
        var ordered = documents
            .OrderByDescending(d => d.DocumentDate ?? d.CreatedAt)
            .Select(d => new GarageDocumentDto(
                d.Id, d.Type, d.Name, d.DocumentDate, d.FileName, d.ContentType,
                d.SizeBytes, d.Notes, d.CreatedAt))
            .ToList();

        return Result<IReadOnlyList<GarageDocumentDto>>.Success(ordered);
    }
}

public class GetGarageDocumentFileQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetGarageDocumentFileQuery, Result<GarageDocumentFileDto>>
{
    public async Task<Result<GarageDocumentFileDto>> Handle(
        GetGarageDocumentFileQuery request, CancellationToken ct)
    {
        var (document, error) = await GarageDocumentWorkflow.LoadAsync(
            db, request.UserId, request.DocumentId, ct);

        if (error is not null) return Result<GarageDocumentFileDto>.Failure(error);

        return Result<GarageDocumentFileDto>.Success(new GarageDocumentFileDto(
            document!.StorageKey, document.FileName, document.ContentType));
    }
}
