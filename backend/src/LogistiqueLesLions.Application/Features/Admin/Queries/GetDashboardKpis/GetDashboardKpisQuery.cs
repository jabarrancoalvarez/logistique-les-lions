using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Admin.Queries.GetDashboardKpis;

public record GetDashboardKpisQuery() : IRequest<Result<DashboardKpisDto>>;
