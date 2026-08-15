using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Garage;

/// <summary>
/// «Vendre ce véhicule»: crea un <b>borrador</b> de anuncio con lo que ya hay en
/// Mon Garage.
/// </summary>
/// <remarks>
/// ⚠️ No se publica nada automáticamente. El anuncio nace en <c>Brouillon</c> y su dueño
/// debe revisar precio, kilometraje, fotografías, estado, descripción, equipamiento,
/// situación aduanera y ubicación antes de publicarlo.
/// </remarks>
public record CreateListingFromGarageCommand(Guid UserId, Guid GarageVehicleId)
    : IRequest<Result<CreateListingResultDto>>;

/// <param name="SuggestedPrice">
/// Valor estimado del vehículo, si lo hay. Es una <b>sugerencia para la pantalla</b>: el
/// anuncio se crea sin precio, porque inventarlo sería poner en boca de quien vende una
/// cifra que no ha decidido.
/// </param>
public record CreateListingResultDto(
    Guid VehicleId,
    string Slug,
    string PublicReference,
    decimal? SuggestedPrice,
    int CopiedImages
);

public class CreateListingFromGarageCommandHandler(
    IApplicationDbContext db,
    IPublicReferenceGenerator references,
    IVehicleValuationService? valuation = null)
    : IRequestHandler<CreateListingFromGarageCommand, Result<CreateListingResultDto>>
{
    public async Task<Result<CreateListingResultDto>> Handle(
        CreateListingFromGarageCommand request, CancellationToken ct)
    {
        var vehicle = await db.GarageVehicles
            .Include(v => v.Make)
            .Include(v => v.Model)
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == request.GarageVehicleId, ct);

        if (vehicle is null)
            return Result<CreateListingResultDto>.Failure("GarageVehicle.NotFound");

        if (vehicle.UserId != request.UserId)
            return Result<CreateListingResultDto>.Failure("GarageVehicle.AccessDenied");

        // Un coche no puede estar dos veces a la venta. Si el anuncio anterior se
        // archivó o se vendió, el enlace se suelta y puede volver a ponerse a la venta.
        if (vehicle.ListedVehicleId is { } existingId)
        {
            var existing = await db.Vehicles
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.Id == existingId, ct);

            if (existing is not null && existing.DeletedAt is null
                && existing.Status is not (VehicleStatus.Vendu or VehicleStatus.Archive))
                return Result<CreateListingResultDto>.Failure("GarageVehicle.AlreadyListed");
        }

        var reference = await references.NextVehicleReferenceAsync(ct);
        var title = BuildTitle(vehicle);

        var listing = new Vehicle
        {
            PublicReference = reference,
            Slug            = BuildSlug(title, reference),
            Title           = title,
            SellerId        = vehicle.UserId,

            MakeId               = vehicle.MakeId,
            ModelId              = vehicle.ModelId,
            Version              = vehicle.Version,
            Year                 = vehicle.Year,
            Mileage              = vehicle.Mileage,
            FuelType             = vehicle.FuelType,
            Transmission         = vehicle.Transmission,
            BodyType             = vehicle.BodyType,
            PowerCv              = vehicle.PowerCv,
            EngineDisplacementCc = vehicle.EngineDisplacementCc,
            Color                = vehicle.Color,
            Vin                  = vehicle.Vin,

            // Nada de esto se hereda: son las decisiones que el documento pide revisar
            // expresamente antes de publicar.
            Price          = 0m,
            Status         = VehicleStatus.Brouillon,
            CustomsStatus  = null,
            Description    = null
        };

        db.Vehicles.Add(listing);

        // Las fotografías del garaje ya están en el almacenamiento público, así que el
        // anuncio reutiliza las mismas URL en lugar de duplicar archivos.
        var order = 0;
        foreach (var image in vehicle.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder))
        {
            db.VehicleImages.Add(new VehicleImage
            {
                VehicleId    = listing.Id,
                Url          = image.Url,
                ThumbnailUrl = image.ThumbnailUrl,
                IsPrimary    = order == 0,
                SortOrder    = order++
            });
        }

        // La transparencia nace apagada: nada del historial privado se publica solo.
        db.VehicleTransparencies.Add(new VehicleTransparency
        {
            VehicleId       = listing.Id,
            GarageVehicleId = vehicle.Id
        });

        vehicle.ListedVehicleId = listing.Id;

        await db.SaveChangesAsync(ct);

        decimal? suggested = null;
        if (valuation is not null)
        {
            var estimate = await valuation.EstimateAsync(vehicle.Id, ct);
            suggested = estimate.EstimatedValue;
        }

        return Result<CreateListingResultDto>.Success(new CreateListingResultDto(
            listing.Id, listing.Slug, listing.PublicReference, suggested, order));
    }

    private static string BuildTitle(GarageVehicle v) =>
        string.Join(' ', new[] { v.Make.Name, v.Model?.Name, v.Version, v.Year.ToString() }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>
    /// Slug con la referencia al final: dos coches idénticos no pueden chocar.
    /// </summary>
    private static string BuildSlug(string title, string reference)
    {
        var normalized = new string(title
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        while (normalized.Contains("--")) normalized = normalized.Replace("--", "-");

        return $"{normalized.Trim('-')}-{reference.ToLowerInvariant()}";
    }
}
