using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.ToggleFavorite;

/// <summary>Añade o quita un vehículo de los guardados de un usuario.</summary>
public record ToggleFavoriteCommand(Guid UserId, Guid VehicleId) : IRequest<Result<bool>>;
