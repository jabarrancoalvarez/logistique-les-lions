using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.UpdateVehicle;

public class UpdateVehicleCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateVehicleCommand, Result>
{
    public async Task<Result> Handle(
        UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (vehicle is null)
            return Result.Failure("Vehículo no encontrado.");

        vehicle.Title          = request.Title;
        vehicle.DescriptionEs  = request.DescriptionEs;
        vehicle.DescriptionEn  = request.DescriptionEn;
        vehicle.MakeId         = request.MakeId;
        vehicle.ModelId        = request.ModelId;
        vehicle.Year           = request.Year;
        vehicle.Mileage        = request.Mileage;
        vehicle.Condition      = request.Condition;
        vehicle.BodyType       = request.BodyType;
        vehicle.FuelType       = request.FuelType;
        vehicle.Transmission   = request.Transmission;
        vehicle.Color          = request.Color;
        vehicle.Vin            = request.Vin;
        vehicle.Price          = request.Price;
        vehicle.Currency       = request.Currency;
        vehicle.PriceNegotiable= request.PriceNegotiable;
        vehicle.CountryOrigin  = request.CountryOrigin;
        vehicle.City           = request.City;
        vehicle.PostalCode     = request.PostalCode;
        vehicle.IsExportReady  = request.IsExportReady;
        vehicle.Specs          = request.Specs;
        vehicle.Features       = request.Features;

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
