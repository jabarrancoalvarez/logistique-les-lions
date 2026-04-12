using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleStats;

/// <summary>
/// Devuelve contadores para los badges animados de la landing page.
/// Resultado cacheado en Redis (TTL 5 minutos).
/// </summary>
public record GetVehicleStatsQuery : IRequest<Result<VehicleStatsDto>>;
