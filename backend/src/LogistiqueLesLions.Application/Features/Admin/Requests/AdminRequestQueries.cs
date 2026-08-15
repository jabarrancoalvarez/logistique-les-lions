using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Requests;

/// <summary>
/// «Demandes de véhicules» — las solicitudes «Trouvez-moi cette voiture».
/// </summary>
/// <remarks>
/// Aquí el administrador deja de ser moderador y presta un servicio: busca coches para
/// quien los pide.
/// </remarks>
public record GetAdminRequestsQuery(
    string? Search = null,
    VehicleRequestStatus? Status = null,
    VehicleRequestOrigin? Origin = null,
    Guid? AssignedAdminId = null,
    bool? Unassigned = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<AdminRequestListDto>>;

public record AdminRequestListDto(
    int TotalCount, int Page, int PageSize, IReadOnlyList<AdminRequestRowDto> Items);

public record AdminRequestRowDto(
    Guid Id,
    string PublicReference,
    Guid UserId,
    string UserName,
    string UserPhone,
    string MakeName,
    string? ModelName,
    int? YearFrom,
    int? YearTo,
    int? MaxMileage,
    decimal? MaxBudget,
    VehicleRequestOrigin Origin,
    VehicleRequestStatus Status,
    Guid? AssignedAdminId,
    string? AssignedAdminName,
    int ProposalsCount,
    DateTimeOffset CreatedAt
);

public class GetAdminRequestsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminRequestsQuery, Result<AdminRequestListDto>>
{
    public async Task<Result<AdminRequestListDto>> Handle(
        GetAdminRequestsQuery request, CancellationToken ct)
    {
        var query = db.VehicleRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(r =>
                r.PublicReference.ToLower().Contains(term)
                || r.MakeName.ToLower().Contains(term)
                || (r.ModelName != null && r.ModelName.ToLower().Contains(term)));
        }

        if (request.Status is { } status) query = query.Where(r => r.Status == status);
        if (request.Origin is { } origin) query = query.Where(r => r.Origin == origin);

        if (request.AssignedAdminId is { } adminId)
            query = query.Where(r => r.AssignedAdminId == adminId);

        // «Sin responsable» es la cola de trabajo real del equipo.
        if (request.Unassigned == true) query = query.Where(r => r.AssignedAdminId == null);

        var total = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var rows = await query
            .Include(r => r.User)
            .Include(r => r.AssignedAdmin)
            .Include(r => r.Proposals)
            // Lo que espera respuesta primero; dentro de eso, lo más antiguo, que es lo
            // que lleva más tiempo esperando.
            .OrderBy(r => r.Status == VehicleRequestStatus.Terminee
                       || r.Status == VehicleRequestStatus.Annulee ? 1 : 0)
            .ThenBy(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminRequestRowDto(
                r.Id, r.PublicReference, r.UserId,
                r.User != null ? r.User.DisplayName : "—",
                r.User != null ? r.User.Phone : "—",
                r.MakeName, r.ModelName, r.YearFrom, r.YearTo, r.MaxMileage, r.MaxBudget,
                r.Origin, r.Status, r.AssignedAdminId,
                r.AssignedAdmin != null ? r.AssignedAdmin.DisplayName : null,
                r.Proposals.Count, r.CreatedAt))
            .ToListAsync(ct);

        return Result<AdminRequestListDto>.Success(
            new AdminRequestListDto(total, page, pageSize, rows));
    }
}

/// <summary>Ficha administrativa de una solicitud.</summary>
public record GetAdminRequestQuery(Guid RequestId) : IRequest<Result<AdminRequestDetailDto>>;

public record AdminRequestDetailDto(
    AdminRequestRowDto Request,
    AdminRequestCriteriaDto Criteria,
    IReadOnlyList<AdminRequestProposalDto> Proposals,
    IReadOnlyList<AdminRequestMessageDto> Messages,
    IReadOnlyList<Users.AdminActionDto> Actions,
    IReadOnlyList<Users.AdminNoteDto> Notes
);

public record AdminRequestCriteriaDto(
    string? Version,
    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    string? Color,
    string? ImportantEquipment,
    string? Notes
);

public record AdminRequestProposalDto(
    Guid Id,
    bool IsInternal,
    Guid? VehicleId,
    string? VehicleSlug,
    string? VehicleTitle,
    decimal? VehiclePrice,
    string? MakeModel,
    string? Version,
    int? Year,
    int? Mileage,
    FuelType? FuelType,
    TransmissionType? Transmission,
    decimal? EstimatedPrice,
    decimal? AdditionalCosts,
    string? CountryOfOrigin,
    IReadOnlyList<string> PhotoUrls,
    string? ExternalUrl,
    string? Comments,
    bool IsSeenByUser,
    DateTimeOffset CreatedAt
);

public record AdminRequestMessageDto(Guid Id, string Body, bool FromAdmin, DateTimeOffset CreatedAt);

public class GetAdminRequestQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminRequestQuery, Result<AdminRequestDetailDto>>
{
    public async Task<Result<AdminRequestDetailDto>> Handle(
        GetAdminRequestQuery request, CancellationToken ct)
    {
        var r = await db.VehicleRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.AssignedAdmin)
            .Include(x => x.Proposals).ThenInclude(p => p.Vehicle)
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == request.RequestId, ct);

        if (r is null) return Result<AdminRequestDetailDto>.Failure("VehicleRequest.NotFound");

        var row = new AdminRequestRowDto(
            r.Id, r.PublicReference, r.UserId,
            r.User?.DisplayName ?? "—", r.User?.Phone ?? "—",
            r.MakeName, r.ModelName, r.YearFrom, r.YearTo, r.MaxMileage, r.MaxBudget,
            r.Origin, r.Status, r.AssignedAdminId, r.AssignedAdmin?.DisplayName,
            r.Proposals.Count, r.CreatedAt);

        var criteria = new AdminRequestCriteriaDto(
            r.Version, r.FuelType, r.Transmission, r.BodyType, r.Color,
            r.ImportantEquipment, r.Notes);

        var proposals = r.Proposals
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AdminRequestProposalDto(
                p.Id, p.IsInternal, p.VehicleId,
                p.Vehicle?.Slug, p.Vehicle?.Title, p.Vehicle?.Price,
                p.MakeModel, p.Version, p.Year, p.Mileage, p.FuelType, p.Transmission,
                p.EstimatedPrice, p.AdditionalCosts, p.CountryOfOrigin,
                SplitPhotos(p.PhotoUrls), p.ExternalUrl, p.Comments,
                p.IsSeenByUser, p.CreatedAt))
            .ToList();

        var messages = r.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new AdminRequestMessageDto(
                m.Id, m.Body, m.SenderId != r.UserId, m.CreatedAt))
            .ToList();

        var actions = await db.AdminActions
            .AsNoTracking()
            .Where(a => a.TargetType == AdminTargetType.Request && a.TargetId == r.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new Users.AdminActionDto(
                a.Id, a.Type, a.Reason, a.Admin != null ? a.Admin.DisplayName : "—", a.CreatedAt))
            .ToListAsync(ct);

        var notes = await db.AdminNotes
            .AsNoTracking()
            .Where(n => n.TargetType == AdminTargetType.Request && n.TargetId == r.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new Users.AdminNoteDto(
                n.Id, n.Body, n.Admin != null ? n.Admin.DisplayName : "—", n.CreatedAt))
            .ToListAsync(ct);

        return Result<AdminRequestDetailDto>.Success(
            new AdminRequestDetailDto(row, criteria, proposals, messages, actions, notes));
    }

    private static IReadOnlyList<string> SplitPhotos(string? photoUrls) =>
        string.IsNullOrWhiteSpace(photoUrls)
            ? []
            : photoUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
