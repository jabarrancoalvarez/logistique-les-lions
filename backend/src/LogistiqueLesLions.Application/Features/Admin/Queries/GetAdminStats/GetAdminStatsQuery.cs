using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Admin.Queries.GetAdminStats;

public record GetAdminStatsQuery : IRequest<Result<AdminStatsDto>>;
