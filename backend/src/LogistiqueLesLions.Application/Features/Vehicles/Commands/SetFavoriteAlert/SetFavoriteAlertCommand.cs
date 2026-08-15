using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.SetFavoriteAlert;

/// <summary>Activa o desactiva la alerta de bajada de precio de un favorito concreto.</summary>
public record SetFavoriteAlertCommand(Guid UserId, Guid VehicleId, bool Enabled) : IRequest<Result>;

/// <summary>
/// Interruptor general: todos los favoritos reciben alertas.
/// </summary>
/// <remarks>
/// Al activarlo se restablecen también las alertas individuales, para que al volver a
/// desactivarlo el usuario parta de un estado coherente en lugar de encontrarse
/// silenciados los vehículos que apagó hace meses.
/// </remarks>
public record SetAllFavoriteAlertsCommand(Guid UserId, bool Enabled) : IRequest<Result>;
