using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Admin.Queries.GetVehiclesAdmin;

public record GetVehiclesAdminQuery(
    VehicleStatus? Status,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<VehicleAdminDto>>>;
