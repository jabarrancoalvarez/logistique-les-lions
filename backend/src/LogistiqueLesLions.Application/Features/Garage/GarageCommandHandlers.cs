using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Garage;

internal static class GarageWorkflow
{
    public static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Comprobaciones comunes a crear y corregir la ficha.</summary>
    public static async Task<string?> ValidateAsync(
        IApplicationDbContext db, GarageVehicleInput v, CancellationToken ct)
    {
        // El año se pide siempre: es lo que distingue un mismo modelo en la tarjeta.
        if (v.Year < 1900 || v.Year > DateTimeOffset.UtcNow.Year + 1)
            return "GarageVehicle.InvalidYear";

        if (v.Mileage is < 0) return "GarageVehicle.InvalidMileage";
        if (v.PurchasePrice is < 0) return "GarageVehicle.InvalidPrice";

        if (!await db.VehicleMakes.AnyAsync(m => m.Id == v.MakeId, ct))
            return "VehicleMake.NotFound";

        // El modelo, si se indica, tiene que ser de esa marca.
        if (v.ModelId is { } modelId &&
            !await db.VehicleModels.AnyAsync(m => m.Id == modelId && m.MakeId == v.MakeId, ct))
            return "VehicleModel.NotFound";

        return null;
    }

    public static void Apply(GarageVehicle entity, GarageVehicleInput v)
    {
        // Declarar el kilometraje es un acto propio: se marca cuándo ocurre, y no se
        // confunde con haber tocado cualquier otro campo de la ficha.
        if (v.Mileage is not null && v.Mileage != entity.Mileage)
            entity.MileageUpdatedAt = DateTimeOffset.UtcNow;

        entity.MakeId               = v.MakeId;
        entity.ModelId              = v.ModelId;
        entity.Version              = Clean(v.Version);
        entity.Year                 = v.Year;
        entity.Mileage              = v.Mileage;
        entity.FuelType             = v.FuelType;
        entity.Transmission         = v.Transmission;
        entity.BodyType             = v.BodyType;
        entity.PowerCv              = v.PowerCv;
        entity.EngineDisplacementCc = v.EngineDisplacementCc;
        entity.Color                = Clean(v.Color);
        entity.RegistrationPlate    = Clean(v.RegistrationPlate)?.ToUpperInvariant();
        entity.Vin                  = Clean(v.Vin)?.ToUpperInvariant();
        entity.PurchaseDate         = v.PurchaseDate;
        entity.PurchasePrice        = v.PurchasePrice;
    }

    /// <summary>Carga un vehículo del garaje comprobando que es del usuario.</summary>
    public static async Task<(GarageVehicle? vehicle, string? error)> LoadAsync(
        IApplicationDbContext db, Guid userId, Guid id, CancellationToken ct)
    {
        var vehicle = await db.GarageVehicles
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (vehicle is null) return (null, "GarageVehicle.NotFound");

        // Mon Garage es privado: nadie más entra, ni siquiera para leer.
        if (vehicle.UserId != userId) return (null, "GarageVehicle.AccessDenied");

        return (vehicle, null);
    }
}

public class CreateGarageVehicleCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateGarageVehicleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateGarageVehicleCommand request, CancellationToken ct)
    {
        var error = await GarageWorkflow.ValidateAsync(db, request.Vehicle, ct);
        if (error is not null) return Result<Guid>.Failure(error);

        var entity = new GarageVehicle { UserId = request.UserId };
        GarageWorkflow.Apply(entity, request.Vehicle);

        if (request.SourceContractId is { } contractId)
        {
            var contract = await db.Contracts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contractId, ct);

            if (contract is null) return Result<Guid>.Failure("Contract.NotFound");

            // El vehículo entra en el garaje de quien lo compra, y solo tras la venta
            // verificada: el contrato en borrador todavía no acredita nada.
            if (contract.BuyerId != request.UserId)
                return Result<Guid>.Failure("Contract.NotBuyer");

            if (contract.Status != ContractStatus.Valide)
                return Result<Guid>.Failure("Contract.NotValidated");

            if (await db.GarageVehicles.AnyAsync(v => v.SourceContractId == contractId, ct))
                return Result<Guid>.Failure("GarageVehicle.AlreadyAdded");

            entity.SourceContractId = contract.Id;
            entity.SourceVehicleId  = contract.VehicleId;
        }

        db.GarageVehicles.Add(entity);
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(entity.Id);
    }
}

public class UpdateGarageVehicleCommandHandler(
    IApplicationDbContext db,
    IReminderService? reminders = null)
    : IRequestHandler<UpdateGarageVehicleCommand, Result>
{
    public async Task<Result> Handle(UpdateGarageVehicleCommand request, CancellationToken ct)
    {
        var (vehicle, error) = await GarageWorkflow.LoadAsync(
            db, request.UserId, request.GarageVehicleId, ct);
        if (error is not null) return Result.Failure(error);

        var invalid = await GarageWorkflow.ValidateAsync(db, request.Vehicle, ct);
        if (invalid is not null) return Result.Failure(invalid);

        // El kilometraje solo avanza: corregirlo a la baja suele ser un error de tecleo,
        // y el historial de mantenimiento se apoya en él.
        if (request.Vehicle.Mileage is { } mileage && vehicle!.Mileage is { } current
            && mileage < current)
            return Result.Failure("GarageVehicle.MileageWentBackwards");

        var previousMileage = vehicle!.Mileage;

        GarageWorkflow.Apply(vehicle, request.Vehicle);
        await db.SaveChangesAsync(ct);

        // Los recordatorios por kilómetros solo pueden vencer cuando el usuario declara
        // una lectura nueva: es el único momento en que sabemos cuánto ha rodado.
        if (reminders is not null && vehicle.Mileage is not null && vehicle.Mileage != previousMileage)
            await reminders.EvaluateVehicleAsync(vehicle.Id, ct);

        return Result.Success();
    }
}

public class DeleteGarageVehicleCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteGarageVehicleCommand, Result>
{
    public async Task<Result> Handle(DeleteGarageVehicleCommand request, CancellationToken ct)
    {
        var (vehicle, error) = await GarageWorkflow.LoadAsync(
            db, request.UserId, request.GarageVehicleId, ct);
        if (error is not null) return Result.Failure(error);

        var now = DateTimeOffset.UtcNow;
        vehicle!.DeletedAt = now;
        foreach (var image in vehicle.Images) image.DeletedAt = now;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class AddGarageVehicleImageCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AddGarageVehicleImageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddGarageVehicleImageCommand request, CancellationToken ct)
    {
        var (vehicle, error) = await GarageWorkflow.LoadAsync(
            db, request.UserId, request.GarageVehicleId, ct);
        if (error is not null) return Result<Guid>.Failure(error);

        // La primera fotografía es siempre la principal: si no, la tarjeta saldría vacía.
        var isPrimary = request.IsPrimary || vehicle!.Images.Count == 0;

        if (isPrimary)
            foreach (var existing in vehicle!.Images) existing.IsPrimary = false;

        var image = new GarageVehicleImage
        {
            GarageVehicleId = vehicle!.Id,
            Url             = request.Url,
            ThumbnailUrl    = request.ThumbnailUrl,
            IsPrimary       = isPrimary,
            SortOrder       = request.SortOrder
        };

        db.GarageVehicleImages.Add(image);
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(image.Id);
    }
}

public class DeleteGarageVehicleImageCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteGarageVehicleImageCommand, Result>
{
    public async Task<Result> Handle(DeleteGarageVehicleImageCommand request, CancellationToken ct)
    {
        var image = await db.GarageVehicleImages
            .Include(i => i.GarageVehicle)
            .FirstOrDefaultAsync(i => i.Id == request.ImageId, ct);

        if (image is null) return Result.Failure("GarageVehicleImage.NotFound");
        if (image.GarageVehicle.UserId != request.UserId)
            return Result.Failure("GarageVehicle.AccessDenied");

        image.DeletedAt = DateTimeOffset.UtcNow;

        // Si se borra la principal, otra ocupa su sitio: la tarjeta necesita una.
        if (image.IsPrimary)
        {
            var next = await db.GarageVehicleImages
                .Where(i => i.GarageVehicleId == image.GarageVehicleId && i.Id != image.Id)
                .OrderBy(i => i.SortOrder)
                .FirstOrDefaultAsync(ct);

            if (next is not null) next.IsPrimary = true;
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
