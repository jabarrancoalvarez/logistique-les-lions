using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.GenerateVehicleDescription;

public class GenerateVehicleDescriptionCommandHandler(
    IApplicationDbContext db,
    IAiContentService ai)
    : IRequestHandler<GenerateVehicleDescriptionCommand, Result<GenerateVehicleDescriptionResult>>
{
    public async Task<Result<GenerateVehicleDescriptionResult>> Handle(
        GenerateVehicleDescriptionCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles
            .Include(v => v.Make)
            .Include(v => v.Model)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken);

        if (vehicle is null)
            return Result<GenerateVehicleDescriptionResult>.Failure("Vehicle.NotFound");

        var context = new VehicleAiContext(
            Make:          vehicle.Make?.Name ?? "—",
            Model:         vehicle.Model?.Name,
            Year:          vehicle.Year,
            Mileage:       vehicle.Mileage,
            FuelType:      vehicle.FuelType?.ToString(),
            Transmission:  vehicle.Transmission?.ToString(),
            BodyType:      vehicle.BodyType?.ToString(),
            Color:         vehicle.Color,
            Condition:     vehicle.Condition.ToString(),
            Price:         vehicle.Price,
            Currency:      vehicle.Currency,
            CountryOrigin: vehicle.CountryOrigin,
            IsExportReady: vehicle.IsExportReady);

        var generated = await ai.GenerateVehicleDescriptionAsync(context, cancellationToken);

        if (request.Overwrite || string.IsNullOrWhiteSpace(vehicle.DescriptionEs))
            vehicle.DescriptionEs = generated.DescriptionEs;
        if (request.Overwrite || string.IsNullOrWhiteSpace(vehicle.DescriptionEn))
            vehicle.DescriptionEn = generated.DescriptionEn;

        await db.SaveChangesAsync(cancellationToken);

        return Result<GenerateVehicleDescriptionResult>.Success(
            new GenerateVehicleDescriptionResult(vehicle.DescriptionEs ?? "", vehicle.DescriptionEn ?? ""));
    }
}
