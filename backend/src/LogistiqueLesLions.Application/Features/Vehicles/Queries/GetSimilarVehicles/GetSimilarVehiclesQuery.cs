using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetSimilarVehicles;

/// <summary>
/// «Véhicules similaires» del final de la ficha.
/// </summary>
/// <remarks>
/// Evita el callejón sin salida: si el coche no convence, el usuario puede seguir
/// buscando sin volver atrás. Se resuelve con reglas de base de datos, sin IA.
/// </remarks>
public record GetSimilarVehiclesQuery(Guid VehicleId, int Take = 8)
    : IRequest<Result<List<VehicleListDto>>>;
