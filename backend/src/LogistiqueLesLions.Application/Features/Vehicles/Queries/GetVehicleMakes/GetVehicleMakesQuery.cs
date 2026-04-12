using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleMakes;

/// <summary>
/// Devuelve todas las marcas para el autocompletado del buscador.
/// Resultado cacheado en Redis (TTL 24 horas).
/// </summary>
public record GetVehicleMakesQuery(bool OnlyPopular = false) : IRequest<Result<IEnumerable<VehicleMakeDto>>>;
