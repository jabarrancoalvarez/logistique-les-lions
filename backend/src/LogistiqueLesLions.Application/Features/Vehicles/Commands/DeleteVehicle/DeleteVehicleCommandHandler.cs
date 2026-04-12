using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.DeleteVehicle;

public class DeleteVehicleCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteVehicleCommand, Result>
{
    public async Task<Result> Handle(
        DeleteVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        if (vehicle is null)
            return Result.Failure("Vehículo no encontrado.");

        if (vehicle.SellerId != request.RequesterId)
            return Result.Failure("No tienes permiso para eliminar este vehículo.");

        // El AuditInterceptor convierte EntityState.Deleted → soft delete
        context.Vehicles.Remove(vehicle);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
