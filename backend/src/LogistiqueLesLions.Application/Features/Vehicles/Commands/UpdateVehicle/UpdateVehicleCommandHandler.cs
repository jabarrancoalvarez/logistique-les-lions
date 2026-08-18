using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.UpdateVehicle;

public class UpdateVehicleCommandHandler(
    IApplicationDbContext context,
    IPriceDropAlertService priceDropAlerts)
    : IRequestHandler<UpdateVehicleCommand, Result>
{
    public async Task<Result> Handle(UpdateVehicleCommand request, CancellationToken ct)
    {
        var vehicle = await context.Vehicles
            .Include(v => v.Equipments)
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (vehicle is null)
            return Result.Failure("Vehicle.NotFound");

        // La información comercial pertenece a quien publica el anuncio.
        if (vehicle.SellerId != request.RequesterId)
            return Result.Failure("Vehicle.NotOwner");

        var equipmentIds = request.EquipmentIds.Distinct().ToList();
        if (equipmentIds.Count > 0)
        {
            var known = await context.VehicleEquipments
                .Where(e => equipmentIds.Contains(e.Id) && e.IsActive)
                .Select(e => e.Id)
                .ToListAsync(ct);

            if (known.Count != equipmentIds.Count)
                return Result.Failure("Vehicle.UnknownEquipment");
        }

        // El histórico solo crece cuando el precio cambia realmente; así "Évolution du
        // prix" y las alertas de bajada no se llenan de entradas idénticas.
        var previousPrice = vehicle.Price;
        var priceChanged = previousPrice != request.Price;

        if (priceChanged)
        {
            context.VehiclePriceHistories.Add(new VehiclePriceHistory
            {
                VehicleId = vehicle.Id,
                Price     = request.Price,
                ChangedAt = DateTimeOffset.UtcNow
            });
        }

        vehicle.Title                = request.Title;
        vehicle.Description          = request.Description;
        vehicle.MakeId               = request.MakeId;
        vehicle.ModelId              = request.ModelId;
        vehicle.Version              = request.Version;
        vehicle.Year                 = request.Year;
        vehicle.Mileage              = request.Mileage;
        vehicle.Condition            = request.Condition;
        vehicle.BodyType             = request.BodyType;
        vehicle.FuelType             = request.FuelType;
        vehicle.Transmission         = request.Transmission;
        vehicle.Color                = request.Color;
        vehicle.Doors                = request.Doors;
        vehicle.Seats                = request.Seats;
        vehicle.Vin                  = request.Vin;
        vehicle.PowerCv              = request.PowerCv;
        vehicle.EngineDisplacementCc = request.EngineDisplacementCc;
        vehicle.Drivetrain           = request.Drivetrain;
        vehicle.EngineName           = request.EngineName;
        vehicle.Price                = request.Price;
        vehicle.PriceNegotiable      = request.PriceNegotiable;
        vehicle.Region               = request.Region;
        vehicle.City                 = request.City;
        vehicle.District             = request.District;

        SyncEquipments(vehicle, equipmentIds);

        await context.SaveChangesAsync(ct);

        // Después de guardar: quienes siguen el anuncio en Favoris reciben el aviso de
        // bajada. Se hace aquí y no antes para no notificar un precio que no llegó a
        // persistirse.
        if (priceChanged)
            await priceDropAlerts.NotifyPriceDropAsync(vehicle.Id, previousPrice, request.Price, ct);

        return Result.Success();
    }

    private static void SyncEquipments(Vehicle vehicle, IReadOnlyCollection<Guid> equipmentIds)
    {
        var toRemove = vehicle.Equipments
            .Where(l => !equipmentIds.Contains(l.EquipmentId))
            .ToList();

        foreach (var link in toRemove)
            vehicle.Equipments.Remove(link);

        var existing = vehicle.Equipments.Select(l => l.EquipmentId).ToHashSet();

        foreach (var id in equipmentIds.Where(id => !existing.Contains(id)))
            vehicle.Equipments.Add(new VehicleEquipmentLink { VehicleId = vehicle.Id, EquipmentId = id });
    }
}
