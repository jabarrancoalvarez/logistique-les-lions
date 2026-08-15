using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Configuration;

/// <summary>
/// «Registro de actividad administrativa».
/// </summary>
/// <remarks>
/// El documento lo justifica en una línea: «esto será muy útil cuando haya varios
/// administradores». Es la lectura de <c>admin_actions</c>, que ya venía llenándose
/// desde P27; aquí solo se consulta.
///
/// ❌ Es de solo lectura por definición: no hay comando que borre ni edite una fila.
/// </remarks>
public record GetActivityLogQuery(
    Guid? AdminId = null,
    AdminTargetType? TargetType = null,
    AdminActionType? Type = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 30
) : IRequest<Result<ActivityLogDto>>;

public record ActivityLogDto(
    int TotalCount, int Page, int PageSize,
    IReadOnlyList<ActivityLogRowDto> Items,
    IReadOnlyList<ActivityAdminDto> Admins
);

public record ActivityLogRowDto(
    Guid Id,
    string AdminName,
    AdminActionType Type,
    AdminTargetType TargetType,
    Guid TargetId,
    string? Reason,
    string? OldValue,
    string? NewValue,
    DateTimeOffset At
);

/// <summary>Administradores que han dejado alguna acción, para el desplegable del filtro.</summary>
public record ActivityAdminDto(Guid Id, string Name);

public class GetActivityLogQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetActivityLogQuery, Result<ActivityLogDto>>
{
    public async Task<Result<ActivityLogDto>> Handle(
        GetActivityLogQuery request, CancellationToken ct)
    {
        var query = db.AdminActions.AsNoTracking();

        if (request.AdminId is { } adminId) query = query.Where(a => a.AdminId == adminId);
        if (request.TargetType is { } target) query = query.Where(a => a.TargetType == target);
        if (request.Type is { } type) query = query.Where(a => a.Type == type);
        if (request.From is { } from) query = query.Where(a => a.CreatedAt >= from);
        // Hasta el final del día indicado: quien filtra «hasta el 8» espera ver el 8.
        if (request.To is { } to) query = query.Where(a => a.CreatedAt < to.AddDays(1));

        var total = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var rows = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityLogRowDto(
                a.Id,
                a.Admin != null ? a.Admin.DisplayName : "—",
                a.Type, a.TargetType, a.TargetId,
                a.Reason, a.OldValue, a.NewValue, a.CreatedAt))
            .ToListAsync(ct);

        var admins = await db.AdminActions
            .AsNoTracking()
            .Where(a => a.Admin != null)
            .Select(a => new ActivityAdminDto(a.AdminId, a.Admin!.DisplayName))
            .Distinct()
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

        return Result<ActivityLogDto>.Success(
            new ActivityLogDto(total, page, pageSize, rows, admins));
    }
}
