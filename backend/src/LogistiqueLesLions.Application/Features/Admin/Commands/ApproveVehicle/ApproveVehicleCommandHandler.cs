using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Commands.ApproveVehicle;

public class ApproveVehicleCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ApproveVehicleCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ApproveVehicleCommand request, CancellationToken ct)
    {
        var vehicle = await db.Vehicles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, ct);

        if (vehicle is null)
            return Result<Unit>.Failure("Vehicle.NotFound");

        vehicle.Status = VehicleStatus.Active;
        await db.SaveChangesAsync(ct);
        return Result<Unit>.Success(Unit.Value);
    }
}
