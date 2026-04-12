using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetFeaturedVehicles;

/// <summary>
/// Devuelve los vehículos destacados para mostrar en la landing page.
/// Resultado cacheado en Redis (TTL 5 minutos).
/// </summary>
public record GetFeaturedVehiclesQuery(int Count = 6) : IRequest<Result<IEnumerable<FeaturedVehicleDto>>>;
