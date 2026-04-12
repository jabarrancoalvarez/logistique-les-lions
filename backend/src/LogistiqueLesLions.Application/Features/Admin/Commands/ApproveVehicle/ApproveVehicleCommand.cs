using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Admin.Commands.ApproveVehicle;

public record ApproveVehicleCommand(Guid VehicleId) : IRequest<Result<Unit>>;
