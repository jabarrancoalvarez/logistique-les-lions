using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.DeleteVehicle;

/// <summary>Soft-delete: cambia Status a Expired y marca DeletedAt.</summary>
public record DeleteVehicleCommand(Guid Id, Guid RequesterId) : IRequest<Result>;
