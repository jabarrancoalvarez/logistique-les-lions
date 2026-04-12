using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.EstimateCost;

/// <summary>Calcula el coste estimado de importación/exportación para un vehículo.</summary>
public record EstimateCostQuery(
    Guid VehicleId,
    string OriginCountry,
    string DestinationCountry
) : IRequest<Result<CostEstimateDto>>;
