using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Countries.Queries.GetSupportedCountries;

/// <summary>
/// Devuelve los países donde la plataforma opera (con documentación disponible).
/// Resultado cacheado en Redis (TTL 1 hora).
/// </summary>
public record GetSupportedCountriesQuery : IRequest<Result<IEnumerable<SupportedCountryDto>>>;
