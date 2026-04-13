using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.CreateVehicle;

public class CreateVehicleCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreateVehicleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        var makeExists = await context.VehicleMakes
            .AnyAsync(m => m.Id == request.MakeId, cancellationToken);
        if (!makeExists)
            return Result<Guid>.Failure("Marca de vehículo no encontrada.");

        if (request.ModelId.HasValue)
        {
            var modelExists = await context.VehicleModels
                .AnyAsync(m => m.Id == request.ModelId.Value && m.MakeId == request.MakeId, cancellationToken);
            if (!modelExists)
                return Result<Guid>.Failure("Modelo de vehículo no encontrado para esta marca.");
        }

        var slug = await GenerateSlugAsync(request, cancellationToken);

        var vehicle = new Vehicle
        {
            Slug           = slug,
            Title          = request.Title,
            DescriptionEs  = request.DescriptionEs,
            DescriptionEn  = request.DescriptionEn,
            MakeId         = request.MakeId,
            ModelId        = request.ModelId,
            Year           = request.Year,
            Mileage        = request.Mileage,
            Condition      = request.Condition,
            BodyType       = request.BodyType,
            FuelType       = request.FuelType,
            Transmission   = request.Transmission,
            Color          = request.Color,
            Vin            = request.Vin,
            Price          = request.Price,
            Currency       = request.Currency,
            PriceNegotiable= request.PriceNegotiable,
            CountryOrigin  = request.CountryOrigin,
            City           = request.City,
            PostalCode     = request.PostalCode,
            IsExportReady  = request.IsExportReady,
            SellerId       = request.SellerId,
            DealerId       = request.DealerId,
            Specs          = request.Specs,
            Features       = request.Features,
            Status         = VehicleStatus.Active,
            ExpiresAt      = DateTimeOffset.UtcNow.AddDays(90)
        };

        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(vehicle.Id);
    }

    private async Task<string> GenerateSlugAsync(CreateVehicleCommand r, CancellationToken ct)
    {
        var makeName = await context.VehicleMakes
            .Where(m => m.Id == r.MakeId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync(ct) ?? "vehicle";

        var baseSlug = $"{makeName}-{r.Year}"
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("á","a").Replace("é","e").Replace("í","i").Replace("ó","o").Replace("ú","u");

        var slug = baseSlug;
        var suffix = 1;
        while (await context.Vehicles.AnyAsync(v => v.Slug == slug, ct))
        {
            slug = $"{baseSlug}-{suffix++}";
        }
        return slug;
    }
}
