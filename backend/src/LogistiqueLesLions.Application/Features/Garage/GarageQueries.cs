using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Garage;

/// <summary>Pantalla principal de Mon Garage: resumen y tarjetas.</summary>
public record GetMyGarageQuery(Guid UserId) : IRequest<Result<GarageDto>>;

/// <param name="VehicleCount">«2 véhicules» del resumen.</param>
/// <param name="OpenReminderCount">«1 rappel à venir» del resumen.</param>
/// <param name="TotalEstimatedValue">
/// «Valeur estimée totale». Suma solo los vehículos que <b>sí</b> tienen estimación;
/// <c>null</c> si ninguno la tiene.
/// </param>
public record GarageDto(
    int VehicleCount,
    int OpenReminderCount,
    decimal? TotalEstimatedValue,
    IReadOnlyList<GarageVehicleCardDto> Vehicles);

public record GarageVehicleCardDto(
    Guid Id,
    string Title,
    int Year,
    int? Mileage,
    string? Color,
    string? RegistrationPlate,
    /// <summary>
    /// Identificador de la foto principal, no su URL: se pide por endpoint autenticado.
    /// </summary>
    Guid? PrimaryImageId,
    /// <summary>Comprado dentro de Yoon u Auto con contrato validado.</summary>
    bool BoughtOnYoonUAuto,
    DateTimeOffset? PurchaseDate,
    /// <summary>«Prochain rappel: Vidange · 2.500 km». <c>null</c> si no hay ninguno abierto.</summary>
    GarageCardReminderDto? NextReminder,
    /// <summary>«Valeur estimée». <c>null</c> si no hay comparables suficientes.</summary>
    decimal? EstimatedValue,
    /// <summary>«Dossier 82 %». Lo completo que está el historial, nunca el estado mecánico.</summary>
    int CompletenessScore
);

public record GarageCardReminderDto(
    Guid Id,
    ReminderType Type,
    string Label,
    ReminderStatus Status,
    DateTimeOffset? DueDate,
    int? DueMileage,
    int? DaysRemaining,
    int? MileageRemaining
);

public class GetMyGarageQueryHandler(
    IApplicationDbContext db,
    IVehicleValuationService? valuation = null)
    : IRequestHandler<GetMyGarageQuery, Result<GarageDto>>
{
    public async Task<Result<GarageDto>> Handle(GetMyGarageQuery request, CancellationToken ct)
    {
        var vehicles = await db.GarageVehicles
            .AsNoTracking()
            .Where(v => v.UserId == request.UserId)
            .Include(v => v.Make)
            .Include(v => v.Model)
            .Include(v => v.Images)
            .Include(v => v.Reminders)
            // Documentos e intervenciones entran porque la tarjeta muestra el porcentaje
            // de complétude, que depende de ellos.
            .Include(v => v.Documents)
            .Include(v => v.MaintenanceRecords)
            .OrderByDescending(v => v.PurchaseDate ?? v.CreatedAt)
            .ToListAsync(ct);

        var openReminders = 0;
        decimal? total = null;

        // La estimación se calcula por vehículo: un garaje tiene dos o tres coches, no
        // miles, así que no compensa complicar la consulta para ahorrar viajes.
        var estimates = new Dictionary<Guid, decimal?>();
        if (valuation is not null)
        {
            foreach (var v in vehicles)
            {
                var estimate = await valuation.EstimateAsync(v.Id, ct);
                estimates[v.Id] = estimate.EstimatedValue;

                if (estimate.EstimatedValue is { } value) total = (total ?? 0m) + value;
            }
        }

        var cards = vehicles
            .Select(v =>
            {
                // El próximo rappel de la tarjeta: primero lo que ya toca, luego lo más
                // cercano por fecha y, en su defecto, por kilómetros.
                var open = v.Reminders.Where(r => r.IsOpen).ToList();
                openReminders += open.Count;

                var next = open
                    .OrderBy(r => r.Status == ReminderStatus.AFaire ? 0 : 1)
                    .ThenBy(r => r.DueDate ?? DateTimeOffset.MaxValue)
                    .ThenBy(r => ReminderWorkflow.MileageRemaining(r, v.Mileage) ?? int.MaxValue)
                    .FirstOrDefault();

                return new GarageVehicleCardDto(
                    v.Id,
                    GarageTitle.For(v.Make.Name, v.Model?.Name, v.Version),
                    v.Year, v.Mileage, v.Color, v.RegistrationPlate,
                    // El identificador, no una URL: la miniatura también es privada.
                    (v.Images.FirstOrDefault(i => i.IsPrimary) ?? v.Images.FirstOrDefault())?.Id,
                    v.SourceContractId is not null,
                    v.PurchaseDate,
                    next is null ? null : new GarageCardReminderDto(
                        next.Id, next.Type, next.Label, next.Status, next.DueDate, next.DueMileage,
                        ReminderWorkflow.DaysRemaining(next),
                        ReminderWorkflow.MileageRemaining(next, v.Mileage)),
                    estimates.GetValueOrDefault(v.Id),
                    CompletenessCalculator.For(v).Score);
            })
            .ToList();

        return Result<GarageDto>.Success(
            new GarageDto(cards.Count, openReminders, total, cards));
    }
}

/// <summary>Ficha completa de un vehículo del garaje.</summary>
public record GetGarageVehicleQuery(Guid UserId, Guid GarageVehicleId)
    : IRequest<Result<GarageVehicleDetailDto>>;

/// <summary>
/// Fotografía del garaje. ❌ No lleva URL: el archivo es privado y se pide por
/// <c>GET /garage/images/{id}</c>, que comprueba de quién es antes de servirlo.
/// </summary>
public record GarageVehicleImageDto(Guid Id, bool IsPrimary, int SortOrder);

public record GarageVehicleDetailDto(
    Guid Id,
    string Title,

    Guid MakeId,
    string MakeName,
    Guid? ModelId,
    string? ModelName,
    string? Version,
    int Year,
    int? Mileage,
    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    int? PowerCv,
    int? EngineDisplacementCc,
    string? Color,

    string? RegistrationPlate,
    string? Vin,

    DateTimeOffset? PurchaseDate,
    decimal? PurchasePrice,

    bool BoughtOnYoonUAuto,
    /// <summary>Anuncio del que salió, si se compró en Yoon u Auto.</summary>
    Guid? SourceVehicleId,

    /// <summary>Anuncio creado desde «Vendre ce véhicule», si está a la venta.</summary>
    Guid? ListedVehicleId,
    string? ListedVehicleSlug,
    VehicleStatus? ListedVehicleStatus,

    IReadOnlyList<GarageVehicleImageDto> Images,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public class GetGarageVehicleQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetGarageVehicleQuery, Result<GarageVehicleDetailDto>>
{
    public async Task<Result<GarageVehicleDetailDto>> Handle(
        GetGarageVehicleQuery request, CancellationToken ct)
    {
        var v = await db.GarageVehicles
            .AsNoTracking()
            .Include(x => x.Make)
            .Include(x => x.Model)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == request.GarageVehicleId, ct);

        if (v is null) return Result<GarageVehicleDetailDto>.Failure("GarageVehicle.NotFound");

        // Mon Garage es privado: nadie más entra, ni siquiera para leer.
        if (v.UserId != request.UserId)
            return Result<GarageVehicleDetailDto>.Failure("GarageVehicle.AccessDenied");

        var images = v.Images
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => new GarageVehicleImageDto(i.Id, i.IsPrimary, i.SortOrder))
            .ToList();

        // El anuncio se lee sin filtros: un borrador o un vehículo ya vendido siguen
        // siendo el enlace de esta ficha con el Marketplace.
        var listing = v.ListedVehicleId is null
            ? null
            : await db.Vehicles
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(x => x.Id == v.ListedVehicleId && x.DeletedAt == null)
                .Select(x => new { x.Id, x.Slug, x.Status })
                .FirstOrDefaultAsync(ct);

        var dto = new GarageVehicleDetailDto(
            v.Id,
            GarageTitle.For(v.Make.Name, v.Model?.Name, v.Version),
            v.MakeId, v.Make.Name, v.ModelId, v.Model?.Name, v.Version,
            v.Year, v.Mileage, v.FuelType, v.Transmission, v.BodyType,
            v.PowerCv, v.EngineDisplacementCc, v.Color,
            v.RegistrationPlate, v.Vin,
            v.PurchaseDate, v.PurchasePrice,
            v.SourceContractId is not null, v.SourceVehicleId,
            listing?.Id, listing?.Slug, listing?.Status,
            images, v.CreatedAt, v.UpdatedAt);

        return Result<GarageVehicleDetailDto>.Success(dto);
    }
}

/// <summary>
/// «Ajouter ce véhicule à Mon Garage» tras una venta verificada.
/// </summary>
/// <remarks>
/// Devuelve la ficha ya rellena con lo que consta en el contrato y en el anuncio, para
/// que quien compra solo tenga que revisarla. No crea nada: el alta la hace después
/// <see cref="CreateGarageVehicleCommand"/> con el contrato como origen.
/// </remarks>
public record GetGaragePrefillFromContractQuery(Guid UserId, Guid ContractId)
    : IRequest<Result<GaragePrefillDto>>;

public record GaragePrefillDto(
    Guid ContractId,
    /// <summary>Ya existe en el garaje: no debe ofrecerse añadirlo otra vez.</summary>
    bool AlreadyAdded,
    Guid? ExistingGarageVehicleId,
    GarageVehicleInput Vehicle,
    string MakeName,
    string? ModelName
);

public class GetGaragePrefillFromContractQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetGaragePrefillFromContractQuery, Result<GaragePrefillDto>>
{
    public async Task<Result<GaragePrefillDto>> Handle(
        GetGaragePrefillFromContractQuery request, CancellationToken ct)
    {
        var contract = await db.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ContractId, ct);

        if (contract is null) return Result<GaragePrefillDto>.Failure("Contract.NotFound");

        if (contract.BuyerId != request.UserId)
            return Result<GaragePrefillDto>.Failure("Contract.NotBuyer");

        if (contract.Status != ContractStatus.Valide)
            return Result<GaragePrefillDto>.Failure("Contract.NotValidated");

        // Los datos técnicos que el contrato no congela salen del anuncio de origen.
        var vehicle = await db.Vehicles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(v => v.Make)
            .Include(v => v.Model)
            .FirstOrDefaultAsync(v => v.Id == contract.VehicleId, ct);

        if (vehicle is null) return Result<GaragePrefillDto>.Failure("Vehicle.NotFound");

        var existing = await db.GarageVehicles
            .AsNoTracking()
            .Where(g => g.SourceContractId == contract.Id)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(ct);

        var input = new GarageVehicleInput(
            vehicle.MakeId,
            vehicle.ModelId,
            // El contrato manda sobre el anuncio: es lo que las partes firmaron.
            contract.VehicleVersion ?? vehicle.Version,
            contract.VehicleYear,
            contract.VehicleMileage ?? vehicle.Mileage,
            vehicle.FuelType,
            vehicle.Transmission,
            vehicle.BodyType,
            vehicle.PowerCv,
            vehicle.EngineDisplacementCc,
            vehicle.Color,
            contract.RegistrationPlate,
            contract.VehicleVin ?? vehicle.Vin,
            contract.SaleDate,
            contract.AgreedPrice);

        var dto = new GaragePrefillDto(
            contract.Id, existing is not null, existing, input,
            vehicle.Make.Name, vehicle.Model?.Name);

        return Result<GaragePrefillDto>.Success(dto);
    }
}

/// <summary>El título que se muestra en la tarjeta: "Toyota RAV4 2.0 D-4D".</summary>
internal static class GarageTitle
{
    public static string For(string make, string? model, string? version) =>
        string.Join(' ', new[] { make, model, version }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>Datos necesarios para servir la fotografía de un vehículo del garaje.</summary>
/// <remarks>
/// El archivo es privado: la identidad sale del token y aquí se comprueba que quien lo
/// pide es su dueño, igual que con los documentos.
/// </remarks>
public record GetGarageVehicleImageFileQuery(Guid UserId, Guid ImageId)
    : IRequest<Result<GarageDocumentFileDto>>;

public class GetGarageVehicleImageFileQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetGarageVehicleImageFileQuery, Result<GarageDocumentFileDto>>
{
    public async Task<Result<GarageDocumentFileDto>> Handle(
        GetGarageVehicleImageFileQuery request, CancellationToken ct)
    {
        var image = await db.GarageVehicleImages
            .AsNoTracking()
            .Include(i => i.GarageVehicle)
            .FirstOrDefaultAsync(i => i.Id == request.ImageId, ct);

        if (image is null)
            return Result<GarageDocumentFileDto>.Failure("GarageVehicle.ImageNotFound");

        if (image.GarageVehicle.UserId != request.UserId)
            return Result<GarageDocumentFileDto>.Failure("GarageVehicle.AccessDenied");

        return Result<GarageDocumentFileDto>.Success(new GarageDocumentFileDto(
            image.StorageKey, image.FileName, image.ContentType));
    }
}
