using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleBySlug;

public record GetVehicleBySlugQuery(string Slug) : IRequest<Result<VehicleDetailDto>>;
