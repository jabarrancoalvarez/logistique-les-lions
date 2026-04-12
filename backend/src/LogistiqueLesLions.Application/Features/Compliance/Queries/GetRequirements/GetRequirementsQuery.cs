using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.GetRequirements;

/// <summary>Devuelve la normativa de importación/exportación entre dos países.</summary>
public record GetRequirementsQuery(
    string OriginCountry,
    string DestinationCountry
) : IRequest<Result<CountryRequirementDto>>;
