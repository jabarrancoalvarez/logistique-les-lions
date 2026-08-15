using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.CountVehicles;

/// <summary>
/// Número de resultados que producen unos filtros, sin traer los anuncios.
/// </summary>
/// <remarks>
/// La especificación exige indicar siempre cuántos resultados dan los filtros
/// <b>antes o inmediatamente después</b> de aplicarlos. Contar sin paginar permite
/// mostrar «Voir les 23 résultats» en el propio panel de filtros.
/// </remarks>
public record CountVehiclesQuery(GetVehiclesQuery Filters) : IRequest<Result<int>>;
