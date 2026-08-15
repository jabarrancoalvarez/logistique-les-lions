using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.CreateVehicle;

public class CreateVehicleCommandHandler(
    IApplicationDbContext context,
    IPublicReferenceGenerator references,
    INewVehicleAlertService newVehicleAlerts)
    : IRequestHandler<CreateVehicleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateVehicleCommand request, CancellationToken ct)
    {
        var makeExists = await context.VehicleMakes
            .AnyAsync(m => m.Id == request.MakeId, ct);
        if (!makeExists)
            return Result<Guid>.Failure("Vehicle.MakeNotFound");

        if (request.ModelId.HasValue)
        {
            var modelExists = await context.VehicleModels
                .AnyAsync(m => m.Id == request.ModelId.Value && m.MakeId == request.MakeId, ct);
            if (!modelExists)
                return Result<Guid>.Failure("Vehicle.ModelNotFound");
        }

        // Solo se aceptan códigos del catálogo: así el equipamiento del anuncio y el del
        // filtro del Marketplace hablan siempre el mismo idioma.
        var equipmentIds = request.EquipmentIds.Distinct().ToList();
        if (equipmentIds.Count > 0)
        {
            var known = await context.VehicleEquipments
                .Where(e => equipmentIds.Contains(e.Id) && e.IsActive)
                .Select(e => e.Id)
                .ToListAsync(ct);

            if (known.Count != equipmentIds.Count)
                return Result<Guid>.Failure("Vehicle.UnknownEquipment");
        }

        var reference = await references.NextVehicleReferenceAsync(ct);
        var slug = await GenerateSlugAsync(request, reference, ct);
        var now = DateTimeOffset.UtcNow;

        var vehicle = new Vehicle
        {
            PublicReference      = reference,
            Slug                 = slug,
            Title                = request.Title,
            Description          = request.Description,
            MakeId               = request.MakeId,
            ModelId              = request.ModelId,
            Version              = request.Version,
            Year                 = request.Year,
            Mileage              = request.Mileage,
            Condition            = request.Condition,
            BodyType             = request.BodyType,
            FuelType             = request.FuelType,
            Transmission         = request.Transmission,
            Color                = request.Color,
            Doors                = request.Doors,
            Seats                = request.Seats,
            Vin                  = request.Vin,
            PowerCv              = request.PowerCv,
            EngineDisplacementCc = request.EngineDisplacementCc,
            Drivetrain           = request.Drivetrain,
            EngineName           = request.EngineName,
            CustomsStatus        = request.CustomsStatus,
            Price                = request.Price,
            PriceNegotiable      = request.PriceNegotiable,
            Region               = request.Region,
            City                 = request.City,
            District             = request.District,
            SellerId             = request.SellerId,
            Status               = request.Publish ? VehicleStatus.Actif : VehicleStatus.Brouillon,
            PublishedAt          = request.Publish ? now : null,
            ExpiresAt            = request.Publish ? now.AddDays(90) : null
        };

        foreach (var equipmentId in equipmentIds)
            vehicle.Equipments.Add(new VehicleEquipmentLink { EquipmentId = equipmentId });

        // Primer punto del histórico: sin él no habría "Prix initial" con el que comparar.
        vehicle.PriceHistory.Add(new VehiclePriceHistory
        {
            Price     = request.Price,
            ChangedAt = now
        });

        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync(ct);

        // «Alerte nouveaux véhicules»: solo cuando el anuncio se publica de verdad.
        // Un borrador no es una novedad para nadie.
        if (request.Publish)
            await newVehicleAlerts.NotifyMatchingSearchesAsync(vehicle.Id, ct);

        return Result<Guid>.Success(vehicle.Id);
    }

    /// <summary>
    /// El slug incorpora la referencia pública para ser único sin necesidad de sondear
    /// la tabla con sufijos incrementales.
    /// </summary>
    private async Task<string> GenerateSlugAsync(CreateVehicleCommand r, string reference, CancellationToken ct)
    {
        var makeName = await context.VehicleMakes
            .Where(m => m.Id == r.MakeId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync(ct) ?? "vehicule";

        var modelName = r.ModelId.HasValue
            ? await context.VehicleModels
                .Where(m => m.Id == r.ModelId.Value)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        return Slugify($"{makeName} {modelName} {r.Year} {reference}");
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace("à", "a").Replace("â", "a").Replace("ä", "a")
            .Replace("é", "e").Replace("è", "e").Replace("ê", "e").Replace("ë", "e")
            .Replace("î", "i").Replace("ï", "i")
            .Replace("ô", "o").Replace("ö", "o")
            .Replace("ù", "u").Replace("û", "u").Replace("ü", "u")
            .Replace("ç", "c");

        var chars = normalized
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();

        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
