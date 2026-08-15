using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Listings;

// ─── Comandos ──────────────────────────────────────────────────────────────

/// <summary>
/// Publier · Pausar · Reactivar · Réservé · Vendu · Archiver.
/// </summary>
/// <remarks>
/// Todas las acciones de estado pasan por aquí para que las transiciones válidas estén
/// en un solo sitio y no repartidas por seis endpoints.
/// </remarks>
public record ChangeListingStatusCommand(Guid UserId, Guid VehicleId, VehicleStatus Status)
    : IRequest<Result>;

/// <summary>«Actualiser le prix» sin abrir el formulario entero.</summary>
public record UpdateListingPriceCommand(Guid UserId, Guid VehicleId, decimal Price)
    : IRequest<Result>;

/// <summary>«Actualiser le kilométrage».</summary>
public record UpdateListingMileageCommand(Guid UserId, Guid VehicleId, int Mileage)
    : IRequest<Result>;

/// <summary>«Dupliquer»: un borrador nuevo a partir de un anuncio existente.</summary>
public record DuplicateListingCommand(Guid UserId, Guid VehicleId) : IRequest<Result<Guid>>;

/// <summary>«Réordonner les photos». El primer identificador pasa a ser la principal.</summary>
public record ReorderListingImagesCommand(
    Guid UserId, Guid VehicleId, IReadOnlyList<Guid> ImageIds) : IRequest<Result>;

// ─── Handlers ──────────────────────────────────────────────────────────────

internal static class ListingWorkflow
{
    /// <summary>
    /// Transiciones admitidas. Lo que no está aquí, no se puede hacer.
    /// </summary>
    /// <remarks>
    /// Un anuncio vendido no vuelve atrás: su ficha sostiene contratos, favoritos y
    /// comparaciones, y reabrirlo cambiaría el pasado. Para volver a venderlo se duplica.
    /// </remarks>
    private static readonly Dictionary<VehicleStatus, VehicleStatus[]> Allowed = new()
    {
        [VehicleStatus.Brouillon] = [VehicleStatus.Actif, VehicleStatus.Archive],
        [VehicleStatus.Actif]     = [VehicleStatus.EnPause, VehicleStatus.Reserve,
                                     VehicleStatus.Vendu, VehicleStatus.Archive],
        [VehicleStatus.EnPause]   = [VehicleStatus.Actif, VehicleStatus.Reserve,
                                     VehicleStatus.Vendu, VehicleStatus.Archive],
        [VehicleStatus.Reserve]   = [VehicleStatus.Actif, VehicleStatus.Vendu,
                                     VehicleStatus.Archive],
        [VehicleStatus.Vendu]     = [VehicleStatus.Archive],
        [VehicleStatus.Archive]   = [VehicleStatus.Brouillon]
    };

    public static bool CanTransition(VehicleStatus from, VehicleStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>Carga el anuncio comprobando que es de quien lo pide.</summary>
    public static async Task<(Vehicle? vehicle, string? error)> LoadAsync(
        IApplicationDbContext db, Guid userId, Guid vehicleId, CancellationToken ct)
    {
        var vehicle = await db.Vehicles
            .IgnoreQueryFilters()
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.DeletedAt == null, ct);

        if (vehicle is null) return (null, "Vehicle.NotFound");

        // La gestión del anuncio pertenece a quien lo publica.
        if (vehicle.SellerId != userId) return (null, "Vehicle.NotOwner");

        return (vehicle, null);
    }
}

public class ChangeListingStatusCommandHandler(
    IApplicationDbContext db,
    INewVehicleAlertService? newVehicleAlerts = null)
    : IRequestHandler<ChangeListingStatusCommand, Result>
{
    public async Task<Result> Handle(ChangeListingStatusCommand request, CancellationToken ct)
    {
        var (vehicle, error) = await ListingWorkflow.LoadAsync(db, request.UserId, request.VehicleId, ct);
        if (error is not null) return Result.Failure(error);

        if (vehicle!.Status == request.Status) return Result.Success();

        if (!ListingWorkflow.CanTransition(vehicle.Status, request.Status))
            return Result.Failure("Listing.InvalidTransition");

        var now = DateTimeOffset.UtcNow;

        // Publicar por primera vez: es ahora cuando el anuncio se convierte en novedad.
        var isFirstPublication = request.Status == VehicleStatus.Actif && vehicle.PublishedAt is null;

        if (request.Status == VehicleStatus.Actif && vehicle.Price <= 0)
            return Result.Failure("Listing.PriceRequired");

        vehicle.Status = request.Status;

        switch (request.Status)
        {
            case VehicleStatus.Actif:
                vehicle.PublishedAt ??= now;
                vehicle.ReservedAt = null;
                break;

            case VehicleStatus.Reserve:
                vehicle.ReservedAt = now;
                break;

            case VehicleStatus.Vendu:
                vehicle.SoldAt = now;
                break;

            case VehicleStatus.Brouillon:
                // Volver del archivo lo devuelve a borrador: se vuelve a publicar a mano,
                // y entonces contará como novedad otra vez.
                vehicle.PublishedAt = null;
                break;
        }

        await db.SaveChangesAsync(ct);

        // «Alerte nouveaux véhicules»: al publicarse de verdad, no al crearse el borrador.
        if (isFirstPublication && newVehicleAlerts is not null)
            await newVehicleAlerts.NotifyMatchingSearchesAsync(vehicle.Id, ct);

        return Result.Success();
    }
}

public class UpdateListingPriceCommandHandler(
    IApplicationDbContext db,
    IPriceDropAlertService? priceDropAlerts = null)
    : IRequestHandler<UpdateListingPriceCommand, Result>
{
    public async Task<Result> Handle(UpdateListingPriceCommand request, CancellationToken ct)
    {
        var (vehicle, error) = await ListingWorkflow.LoadAsync(db, request.UserId, request.VehicleId, ct);
        if (error is not null) return Result.Failure(error);

        if (request.Price <= 0) return Result.Failure("Listing.InvalidPrice");

        var previous = vehicle!.Price;
        if (previous == request.Price) return Result.Success();

        // Cada cambio de precio deja su rastro: el histórico nunca se modifica ni se borra.
        db.VehiclePriceHistories.Add(new VehiclePriceHistory
        {
            VehicleId = vehicle.Id,
            Price     = request.Price,
            ChangedAt = DateTimeOffset.UtcNow
        });

        vehicle.Price = request.Price;
        await db.SaveChangesAsync(ct);

        // Después de guardar: quienes lo siguen en Favoris reciben el aviso de bajada.
        if (priceDropAlerts is not null)
            await priceDropAlerts.NotifyPriceDropAsync(vehicle.Id, previous, request.Price, ct);

        return Result.Success();
    }
}

public class UpdateListingMileageCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateListingMileageCommand, Result>
{
    public async Task<Result> Handle(UpdateListingMileageCommand request, CancellationToken ct)
    {
        var (vehicle, error) = await ListingWorkflow.LoadAsync(db, request.UserId, request.VehicleId, ct);
        if (error is not null) return Result.Failure(error);

        if (request.Mileage < 0) return Result.Failure("Listing.InvalidMileage");

        // El kilometraje solo avanza mientras el anuncio está vivo: bajarlo en un coche
        // en venta es un error de tecleo o algo peor.
        if (vehicle!.Mileage is { } current && request.Mileage < current)
            return Result.Failure("Listing.MileageWentBackwards");

        vehicle.Mileage = request.Mileage;
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

public class DuplicateListingCommandHandler(
    IApplicationDbContext db,
    IPublicReferenceGenerator references)
    : IRequestHandler<DuplicateListingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DuplicateListingCommand request, CancellationToken ct)
    {
        var original = await db.Vehicles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(v => v.Images)
            .Include(v => v.Equipments)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.DeletedAt == null, ct);

        if (original is null) return Result<Guid>.Failure("Vehicle.NotFound");
        if (original.SellerId != request.UserId) return Result<Guid>.Failure("Vehicle.NotOwner");

        var reference = await references.NextVehicleReferenceAsync(ct);

        var copy = new Vehicle
        {
            PublicReference = reference,
            Slug            = $"{StripReference(original.Slug)}-{reference.ToLowerInvariant()}",
            Title           = original.Title,
            Description     = original.Description,
            SellerId        = original.SellerId,

            MakeId               = original.MakeId,
            ModelId              = original.ModelId,
            Version              = original.Version,
            Year                 = original.Year,
            Mileage              = original.Mileage,
            Condition            = original.Condition,
            BodyType             = original.BodyType,
            FuelType             = original.FuelType,
            Transmission         = original.Transmission,
            Color                = original.Color,
            PowerCv              = original.PowerCv,
            EngineDisplacementCc = original.EngineDisplacementCc,
            Drivetrain           = original.Drivetrain,
            EngineName           = original.EngineName,
            Doors                = original.Doors,
            Seats                = original.Seats,
            CustomsStatus        = original.CustomsStatus,
            Price                = original.Price,
            PriceNegotiable      = original.PriceNegotiable,
            Region               = original.Region,
            City                 = original.City,
            District             = original.District,

            // La copia nace en borrador y sin pasado: ni el VIN, ni las visitas, ni los
            // favoritos del original tienen nada que ver con ella.
            Status = VehicleStatus.Brouillon,
            Vin    = null
        };

        db.Vehicles.Add(copy);

        foreach (var image in original.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder))
        {
            db.VehicleImages.Add(new VehicleImage
            {
                VehicleId    = copy.Id,
                Url          = image.Url,
                ThumbnailUrl = image.ThumbnailUrl,
                AltText      = image.AltText,
                IsPrimary    = image.IsPrimary,
                SortOrder    = image.SortOrder
            });
        }

        foreach (var link in original.Equipments)
        {
            db.VehicleEquipmentLinks.Add(new VehicleEquipmentLink
            {
                VehicleId   = copy.Id,
                EquipmentId = link.EquipmentId
            });
        }

        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(copy.Id);
    }

    /// <summary>Quita la referencia del slug original para no encadenar sufijos.</summary>
    private static string StripReference(string slug)
    {
        var index = slug.LastIndexOf("-yu", StringComparison.OrdinalIgnoreCase);
        return index > 0 ? slug[..index] : slug;
    }
}

public class ReorderListingImagesCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ReorderListingImagesCommand, Result>
{
    public async Task<Result> Handle(ReorderListingImagesCommand request, CancellationToken ct)
    {
        var (vehicle, error) = await ListingWorkflow.LoadAsync(db, request.UserId, request.VehicleId, ct);
        if (error is not null) return Result.Failure(error);

        var images = vehicle!.Images.ToDictionary(i => i.Id);

        // Se exige el listado completo: un orden parcial dejaría fotos con posiciones
        // repetidas y el resultado dependería de cómo las ordene la base de datos.
        if (request.ImageIds.Count != images.Count || request.ImageIds.Any(id => !images.ContainsKey(id)))
            return Result.Failure("Listing.ImageSetMismatch");

        var order = 0;
        foreach (var id in request.ImageIds)
        {
            var image = images[id];
            image.SortOrder = order;
            image.IsPrimary = order == 0;
            order++;
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
