using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.VehicleRequests;

/// <summary>Mes recherches → Mes demandes.</summary>
public record GetMyVehicleRequestsQuery(Guid UserId) : IRequest<Result<List<VehicleRequestSummaryDto>>>;

/// <summary>Tarjeta de seguimiento de una solicitud.</summary>
public record VehicleRequestSummaryDto(
    Guid Id,
    string PublicReference,
    string MakeName,
    string? ModelName,
    int? YearFrom,
    int? YearTo,
    int? MaxMileage,
    decimal? MaxBudget,
    VehicleRequestOrigin Origin,
    VehicleRequestStatus Status,
    /// <summary>Propuestas que el usuario aún no ha visto.</summary>
    int UnseenProposals,
    int ProposalsCount,
    DateTimeOffset CreatedAt
);

public class GetMyVehicleRequestsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetMyVehicleRequestsQuery, Result<List<VehicleRequestSummaryDto>>>
{
    public async Task<Result<List<VehicleRequestSummaryDto>>> Handle(
        GetMyVehicleRequestsQuery request, CancellationToken ct)
    {
        var items = await context.VehicleRequests
            .AsNoTracking()
            .Where(r => r.UserId == request.UserId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new VehicleRequestSummaryDto(
                r.Id,
                r.PublicReference,
                r.MakeName,
                r.ModelName,
                r.YearFrom,
                r.YearTo,
                r.MaxMileage,
                r.MaxBudget,
                r.Origin,
                r.Status,
                r.Proposals.Count(p => !p.IsSeenByUser),
                r.Proposals.Count,
                r.CreatedAt))
            .ToListAsync(ct);

        return Result<List<VehicleRequestSummaryDto>>.Success(items);
    }
}
