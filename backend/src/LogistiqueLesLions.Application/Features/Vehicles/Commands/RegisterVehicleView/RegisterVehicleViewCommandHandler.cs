using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.RegisterVehicleView;

public class RegisterVehicleViewCommandHandler(IApplicationDbContext context)
    : IRequestHandler<RegisterVehicleViewCommand, Result>
{
    public async Task<Result> Handle(RegisterVehicleViewCommand request, CancellationToken ct)
    {
        // Incremento en la propia base de datos: si se leyera el contador y se
        // reescribiera, dos visitas simultáneas se pisarían y solo contaría una.
        var updated = await context.Vehicles
            .Where(v => v.Id == request.VehicleId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.ViewsCount, v => v.ViewsCount + 1), ct);

        return updated > 0 ? Result.Success() : Result.Failure("Vehicle.NotFound");
    }
}
