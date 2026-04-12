using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleHistory;

public record GetVehicleHistoryQuery(Guid VehicleId) : IRequest<Result<IEnumerable<VehicleHistoryDto>>>;
