using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.RegisterVehicleView;

/// <summary>
/// Suma una visualización al anuncio. Alimenta el contador que el administrador
/// consulta en la ficha administrativa y las métricas de conversión.
/// </summary>
public record RegisterVehicleViewCommand(Guid VehicleId) : IRequest<Result>;
