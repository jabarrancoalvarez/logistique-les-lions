using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetMyFavorites;

public record GetMyFavoritesQuery(Guid UserId) : IRequest<Result<List<VehicleListDto>>>;
