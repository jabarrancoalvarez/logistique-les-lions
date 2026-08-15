using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Configuration;

/// <summary>
/// Catálogos: marcas, modelos y equipamiento.
/// </summary>
/// <remarks>
/// Son las listas de las que se alimentan el formulario de publicación y los filtros.
/// Que las administre una persona y no un despliegue es justo lo que pide el documento.
///
/// ❌ Nada se borra de verdad: una marca usada por doscientos anuncios no puede
/// desaparecer. Lo que hay es retirar del catálogo, que la esconde de los formularios y
/// deja intactos los anuncios existentes.
/// </remarks>
public record GetCatalogsQuery : IRequest<Result<CatalogsDto>>;

public record CatalogsDto(
    IReadOnlyList<CatalogMakeDto> Makes,
    IReadOnlyList<CatalogEquipmentDto> Equipments,
    IReadOnlyList<CatalogFeatureDto> UpcomingFeatures
);

/// <param name="ListingsCount">Anuncios que la usan: lo que impide retirarla a la ligera.</param>
public record CatalogMakeDto(
    Guid Id, string Name, string? Country, bool IsPopular,
    int ModelsCount, int ListingsCount,
    IReadOnlyList<CatalogModelDto> Models);

public record CatalogModelDto(Guid Id, string Name, string? Category, int ListingsCount);

public record CatalogEquipmentDto(
    Guid Id, string Code, string Name, int DisplayOrder, bool IsActive, int ListingsCount);

public record CatalogFeatureDto(
    Guid Id, string Code, string Name, string? Description,
    int DisplayOrder, bool IsActive, int InterestedCount);

public class GetCatalogsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCatalogsQuery, Result<CatalogsDto>>
{
    public async Task<Result<CatalogsDto>> Handle(GetCatalogsQuery request, CancellationToken ct)
    {
        // Los recuentos se resuelven en memoria sobre listas pequeñas: son catálogos,
        // no tablas de hechos.
        var listingsByMake = await CountByAsync(db, v => v.MakeId, ct);
        var listingsByModel = await db.Vehicles
            .AsNoTracking()
            .Where(v => v.ModelId != null)
            .Select(v => v.ModelId!.Value)
            .ToListAsync(ct);

        var modelCounts = listingsByModel
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var models = await db.VehicleModels
            .AsNoTracking()
            .Select(m => new { m.Id, m.MakeId, m.Name, m.Category })
            .ToListAsync(ct);

        var makes = await db.VehicleMakes
            .AsNoTracking()
            .Select(m => new { m.Id, m.Name, m.Country, m.IsPopular })
            .ToListAsync(ct);

        var makeRows = makes
            .Select(m =>
            {
                var own = models
                    .Where(x => x.MakeId == m.Id)
                    .OrderBy(x => x.Name)
                    .Select(x => new CatalogModelDto(
                        x.Id, x.Name, x.Category, modelCounts.GetValueOrDefault(x.Id)))
                    .ToList();

                return new CatalogMakeDto(
                    m.Id, m.Name, m.Country, m.IsPopular,
                    own.Count, listingsByMake.GetValueOrDefault(m.Id), own);
            })
            .OrderBy(m => m.Name)
            .ToList();

        var equipmentUsage = await db.VehicleEquipmentLinks
            .AsNoTracking()
            .Select(l => l.EquipmentId)
            .ToListAsync(ct);

        var equipmentCounts = equipmentUsage
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var equipments = await db.VehicleEquipments
            .AsNoTracking()
            .OrderBy(e => e.DisplayOrder).ThenBy(e => e.Name)
            .Select(e => new { e.Id, e.Code, e.Name, e.DisplayOrder, e.IsActive })
            .ToListAsync(ct);

        var features = await db.UpcomingFeatures
            .AsNoTracking()
            .OrderBy(f => f.DisplayOrder).ThenBy(f => f.Name)
            .Select(f => new CatalogFeatureDto(
                f.Id, f.Code, f.Name, f.Description, f.DisplayOrder, f.IsActive,
                f.Interests.Count))
            .ToListAsync(ct);

        return Result<CatalogsDto>.Success(new CatalogsDto(
            makeRows,
            equipments
                .Select(e => new CatalogEquipmentDto(
                    e.Id, e.Code, e.Name, e.DisplayOrder, e.IsActive,
                    equipmentCounts.GetValueOrDefault(e.Id)))
                .ToList(),
            features));
    }

    private static async Task<Dictionary<Guid, int>> CountByAsync(
        IApplicationDbContext db,
        System.Linq.Expressions.Expression<Func<Vehicle, Guid>> selector,
        CancellationToken ct)
    {
        var ids = await db.Vehicles.AsNoTracking().Select(selector).ToListAsync(ct);
        return ids.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
    }
}

// ─── Marcas y modelos ───────────────────────────────────────────────────────

public record SaveCatalogMakeCommand(
    Guid AdminId, Guid? Id, string Name, string? Country, bool IsPopular)
    : IRequest<Result<Guid>>;

public class SaveCatalogMakeCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SaveCatalogMakeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SaveCatalogMakeCommand request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return Result<Guid>.Failure("Catalog.NameRequired");

        var duplicate = await db.VehicleMakes
            .AnyAsync(m => m.Name.ToLower() == name.ToLower() && m.Id != request.Id, ct);
        if (duplicate) return Result<Guid>.Failure("Catalog.MakeAlreadyExists");

        VehicleMake make;
        string? oldValue = null;

        if (request.Id is { } id)
        {
            var existing = await db.VehicleMakes.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (existing is null) return Result<Guid>.Failure("Catalog.MakeNotFound");

            oldValue = existing.Name;
            make = existing;
        }
        else
        {
            make = new VehicleMake();
            db.VehicleMakes.Add(make);
        }

        make.Name      = name;
        make.Country   = string.IsNullOrWhiteSpace(request.Country) ? null : request.Country.Trim();
        make.IsPopular = request.IsPopular;

        db.AdminActions.Add(Journal(request.AdminId, make.Id, $"Marque « {name} »", oldValue, name));

        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(make.Id);
    }

    internal static AdminAction Journal(
        Guid adminId, Guid targetId, string what, string? oldValue, string? newValue) =>
        new()
        {
            AdminId    = adminId,
            TargetType = AdminTargetType.Settings,
            TargetId   = targetId,
            Type       = AdminActionType.CatalogChanged,
            Reason     = what,
            OldValue   = oldValue,
            NewValue   = newValue
        };
}

public record SaveCatalogModelCommand(
    Guid AdminId, Guid? Id, Guid MakeId, string Name, string? Category)
    : IRequest<Result<Guid>>;

public class SaveCatalogModelCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SaveCatalogModelCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SaveCatalogModelCommand request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return Result<Guid>.Failure("Catalog.NameRequired");

        if (!await db.VehicleMakes.AnyAsync(m => m.Id == request.MakeId, ct))
            return Result<Guid>.Failure("Catalog.MakeNotFound");

        // Dos «Corolla» de marcas distintas son legítimos; dos de la misma, no.
        var duplicate = await db.VehicleModels.AnyAsync(
            m => m.MakeId == request.MakeId
              && m.Name.ToLower() == name.ToLower()
              && m.Id != request.Id, ct);
        if (duplicate) return Result<Guid>.Failure("Catalog.ModelAlreadyExists");

        VehicleModel model;
        string? oldValue = null;

        if (request.Id is { } id)
        {
            var existing = await db.VehicleModels.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (existing is null) return Result<Guid>.Failure("Catalog.ModelNotFound");

            oldValue = existing.Name;
            model = existing;
        }
        else
        {
            model = new VehicleModel { MakeId = request.MakeId };
            db.VehicleModels.Add(model);
        }

        model.MakeId   = request.MakeId;
        model.Name     = name;
        model.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();

        db.AdminActions.Add(SaveCatalogMakeCommandHandler.Journal(
            request.AdminId, model.Id, $"Modèle « {name} »", oldValue, name));

        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(model.Id);
    }
}

// ─── Equipamiento ───────────────────────────────────────────────────────────

public record SaveCatalogEquipmentCommand(
    Guid AdminId, Guid? Id, string Code, string Name, int DisplayOrder, bool IsActive)
    : IRequest<Result<Guid>>;

public class SaveCatalogEquipmentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SaveCatalogEquipmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        SaveCatalogEquipmentCommand request, CancellationToken ct)
    {
        var code = request.Code?.Trim().ToUpperInvariant();
        var name = request.Name?.Trim();

        if (string.IsNullOrEmpty(code)) return Result<Guid>.Failure("Catalog.CodeRequired");
        if (string.IsNullOrEmpty(name)) return Result<Guid>.Failure("Catalog.NameRequired");

        var duplicate = await db.VehicleEquipments
            .AnyAsync(e => e.Code == code && e.Id != request.Id, ct);
        if (duplicate) return Result<Guid>.Failure("Catalog.EquipmentAlreadyExists");

        VehicleEquipment equipment;
        string? oldValue = null;

        if (request.Id is { } id)
        {
            var existing = await db.VehicleEquipments.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (existing is null) return Result<Guid>.Failure("Catalog.EquipmentNotFound");

            oldValue = $"{existing.Name} ({(existing.IsActive ? "actif" : "retiré")})";
            equipment = existing;
        }
        else
        {
            equipment = new VehicleEquipment();
            db.VehicleEquipments.Add(equipment);
        }

        // El código es el contrato: una vez creado no se cambia, porque los anuncios ya
        // enlazados dejarían de significar lo mismo.
        if (request.Id is null) equipment.Code = code;

        equipment.Name         = name;
        equipment.DisplayOrder = request.DisplayOrder;
        equipment.IsActive     = request.IsActive;

        db.AdminActions.Add(SaveCatalogMakeCommandHandler.Journal(
            request.AdminId, equipment.Id, $"Équipement « {name} »", oldValue,
            $"{name} ({(request.IsActive ? "actif" : "retiré")})"));

        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(equipment.Id);
    }
}

// ─── Fonctionnalités à venir ────────────────────────────────────────────────

public record SaveUpcomingFeatureCommand(
    Guid AdminId, Guid? Id, string Code, string Name, string? Description,
    int DisplayOrder, bool IsActive)
    : IRequest<Result<Guid>>;

public class SaveUpcomingFeatureCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SaveUpcomingFeatureCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        SaveUpcomingFeatureCommand request, CancellationToken ct)
    {
        var code = request.Code?.Trim().ToUpperInvariant();
        var name = request.Name?.Trim();

        if (string.IsNullOrEmpty(code)) return Result<Guid>.Failure("Catalog.CodeRequired");
        if (string.IsNullOrEmpty(name)) return Result<Guid>.Failure("Catalog.NameRequired");

        var duplicate = await db.UpcomingFeatures
            .AnyAsync(f => f.Code == code && f.Id != request.Id, ct);
        if (duplicate) return Result<Guid>.Failure("Catalog.FeatureAlreadyExists");

        UpcomingFeature feature;
        string? oldValue = null;

        if (request.Id is { } id)
        {
            var existing = await db.UpcomingFeatures.FirstOrDefaultAsync(f => f.Id == id, ct);
            if (existing is null) return Result<Guid>.Failure("Catalog.FeatureNotFound");

            oldValue = $"{existing.Name} ({(existing.IsActive ? "visible" : "retirée")})";
            feature = existing;
        }
        else
        {
            feature = new UpcomingFeature();
            db.UpcomingFeatures.Add(feature);
        }

        if (request.Id is null) feature.Code = code;

        feature.Name         = name;
        feature.Description  = string.IsNullOrWhiteSpace(request.Description)
            ? null : request.Description.Trim();
        feature.DisplayOrder = request.DisplayOrder;
        feature.IsActive     = request.IsActive;

        db.AdminActions.Add(SaveCatalogMakeCommandHandler.Journal(
            request.AdminId, feature.Id, $"Fonctionnalité « {name} »", oldValue,
            $"{name} ({(request.IsActive ? "visible" : "retirée")})"));

        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(feature.Id);
    }
}
