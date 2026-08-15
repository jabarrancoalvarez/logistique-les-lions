using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Moderation;

// ─── Lado usuario ──────────────────────────────────────────────────────────

/// <summary>Reportar un anuncio, una persona o una conversación.</summary>
public record CreateReportCommand(
    Guid ReporterId,
    ReportTargetType TargetType,
    Guid TargetId,
    ReportReason Reason,
    string? Description,
    IReadOnlyList<string>? Evidence
) : IRequest<Result<string>>;

public class CreateReportCommandHandler(
    IApplicationDbContext db,
    IPublicReferenceGenerator references)
    : IRequestHandler<CreateReportCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateReportCommand request, CancellationToken ct)
    {
        // Se resuelve a quién se está señalando en el momento de reportar: si mañana el
        // anuncio cambia de manos o desaparece, el reporte debe seguir diciéndolo.
        var (reportedUserId, error) = await ResolveTargetAsync(request, ct);
        if (error is not null) return Result<string>.Failure(error);

        if (reportedUserId == request.ReporterId)
            return Result<string>.Failure("Report.CannotReportSelf");

        // Un mismo usuario no abre dos reportes abiertos sobre lo mismo: sería ruido en
        // la bandeja, no más información.
        var duplicate = await db.Reports.AnyAsync(r =>
            r.ReporterId == request.ReporterId
            && r.TargetType == request.TargetType
            && r.TargetId == request.TargetId
            && (r.Status == ReportStatus.Nouveau || r.Status == ReportStatus.EnExamen), ct);

        if (duplicate) return Result<string>.Failure("Report.AlreadyReported");

        var report = new Report
        {
            PublicReference = await references.NextReportReferenceAsync(ct),
            ReporterId      = request.ReporterId,
            TargetType      = request.TargetType,
            TargetId        = request.TargetId,
            ReportedUserId  = reportedUserId,
            Reason          = request.Reason,
            Description     = string.IsNullOrWhiteSpace(request.Description)
                ? null : request.Description.Trim(),
            Evidence        = request.Evidence is { Count: > 0 }
                ? string.Join('\n', request.Evidence.Where(e => !string.IsNullOrWhiteSpace(e)))
                : null
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync(ct);

        return Result<string>.Success(report.PublicReference);
    }

    private async Task<(Guid? reportedUserId, string? error)> ResolveTargetAsync(
        CreateReportCommand request, CancellationToken ct)
    {
        switch (request.TargetType)
        {
            case ReportTargetType.Listing:
                var vehicle = await db.Vehicles
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(v => v.Id == request.TargetId && v.DeletedAt == null, ct);
                return vehicle is null ? (null, "Vehicle.NotFound") : (vehicle.SellerId, null);

            case ReportTargetType.User:
                var exists = await db.UserProfiles.AnyAsync(u => u.Id == request.TargetId, ct);
                return exists ? (request.TargetId, null) : (null, "User.NotFound");

            case ReportTargetType.Negotiation:
                var negotiation = await db.Negotiations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == request.TargetId, ct);

                if (negotiation is null) return (null, "Negotiation.NotFound");

                // Solo las partes pueden reportar su propia conversación, y se señala
                // a la otra.
                if (!negotiation.Involves(request.ReporterId)) return (null, "Negotiation.AccessDenied");

                return (negotiation.BuyerId == request.ReporterId
                    ? negotiation.SellerId
                    : negotiation.BuyerId, null);

            default:
                return (null, "Report.InvalidTarget");
        }
    }
}

// ─── Lado administración ───────────────────────────────────────────────────

public record GetReportsQuery(
    string? Search = null,
    ReportStatus? Status = null,
    ReportReason? Reason = null,
    ReportTargetType? TargetType = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<ReportListDto>>;

public record ReportListDto(
    int TotalCount, int Page, int PageSize,
    /// <summary>Cuántos hay en cada estado, para las pestañas.</summary>
    IReadOnlyDictionary<ReportStatus, int> CountByStatus,
    IReadOnlyList<ReportRowDto> Items);

public record ReportRowDto(
    Guid Id,
    string PublicReference,
    ReportTargetType TargetType,
    Guid TargetId,
    /// <summary>Etiqueta legible de lo reportado: título del anuncio, nombre…</summary>
    string TargetLabel,
    Guid ReporterId,
    string ReporterName,
    Guid? ReportedUserId,
    string? ReportedUserName,
    ReportReason Reason,
    string? Description,
    ReportStatus Status,
    DateTimeOffset CreatedAt
);

public class GetReportsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReportsQuery, Result<ReportListDto>>
{
    public async Task<Result<ReportListDto>> Handle(GetReportsQuery request, CancellationToken ct)
    {
        var query = db.Reports.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(r =>
                r.PublicReference.ToLower().Contains(term)
                || (r.Description != null && r.Description.ToLower().Contains(term)));
        }

        if (request.Status is { } status) query = query.Where(r => r.Status == status);
        if (request.Reason is { } reason) query = query.Where(r => r.Reason == reason);
        if (request.TargetType is { } target) query = query.Where(r => r.TargetType == target);

        var counts = await db.Reports
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var reports = await query
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            // Lo abierto primero: es la bandeja de trabajo.
            .OrderBy(r => r.Status == ReportStatus.Nouveau ? 0
                        : r.Status == ReportStatus.EnExamen ? 1 : 2)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var rows = new List<ReportRowDto>(reports.Count);
        foreach (var r in reports)
        {
            rows.Add(new ReportRowDto(
                r.Id, r.PublicReference, r.TargetType, r.TargetId,
                await LabelAsync(r, ct),
                r.ReporterId, r.Reporter?.DisplayName ?? "—",
                r.ReportedUserId, r.ReportedUser?.DisplayName,
                r.Reason, r.Description, r.Status, r.CreatedAt));
        }

        return Result<ReportListDto>.Success(new ReportListDto(
            total, page, pageSize,
            counts.ToDictionary(c => c.Status, c => c.Count),
            rows));
    }

    private async Task<string> LabelAsync(Report r, CancellationToken ct) => r.TargetType switch
    {
        ReportTargetType.Listing => await db.Vehicles
            .AsNoTracking().IgnoreQueryFilters()
            .Where(v => v.Id == r.TargetId)
            .Select(v => $"{v.Title} (#{v.PublicReference})")
            .FirstOrDefaultAsync(ct) ?? "Annonce supprimée",

        ReportTargetType.User => await db.UserProfiles
            .AsNoTracking()
            .Where(u => u.Id == r.TargetId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct) ?? "Utilisateur inconnu",

        _ => await db.Negotiations
            .AsNoTracking()
            .Where(n => n.Id == r.TargetId)
            .Select(n => $"Négociation — {n.Vehicle.Title}")
            .FirstOrDefaultAsync(ct) ?? "Négociation"
    };
}

/// <summary>Ficha del reporte.</summary>
public record GetReportQuery(Guid ReportId) : IRequest<Result<ReportDetailDto>>;

public record ReportDetailDto(
    ReportRowDto Report,
    IReadOnlyList<string> Evidence,
    string? Resolution,
    DateTimeOffset? ResolvedAt,
    string? HandledByAdminName,
    /// <summary>Otros reportes abiertos sobre lo mismo.</summary>
    int OtherOpenReports,
    IReadOnlyList<Admin.Users.AdminActionDto> Actions,
    IReadOnlyList<Admin.Users.AdminNoteDto> Notes
);

public class GetReportQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReportQuery, Result<ReportDetailDto>>
{
    public async Task<Result<ReportDetailDto>> Handle(GetReportQuery request, CancellationToken ct)
    {
        var r = await db.Reports
            .AsNoTracking()
            .Include(x => x.Reporter)
            .Include(x => x.ReportedUser)
            .Include(x => x.HandledByAdmin)
            .FirstOrDefaultAsync(x => x.Id == request.ReportId, ct);

        if (r is null) return Result<ReportDetailDto>.Failure("Report.NotFound");

        var label = r.TargetType switch
        {
            ReportTargetType.Listing => await db.Vehicles
                .AsNoTracking().IgnoreQueryFilters()
                .Where(v => v.Id == r.TargetId)
                .Select(v => $"{v.Title} (#{v.PublicReference})")
                .FirstOrDefaultAsync(ct) ?? "Annonce supprimée",
            ReportTargetType.User => r.ReportedUser?.DisplayName ?? "Utilisateur inconnu",
            _ => "Négociation"
        };

        var row = new ReportRowDto(
            r.Id, r.PublicReference, r.TargetType, r.TargetId, label,
            r.ReporterId, r.Reporter?.DisplayName ?? "—",
            r.ReportedUserId, r.ReportedUser?.DisplayName,
            r.Reason, r.Description, r.Status, r.CreatedAt);

        var others = await db.Reports.CountAsync(x =>
            x.Id != r.Id
            && x.TargetType == r.TargetType && x.TargetId == r.TargetId
            && (x.Status == ReportStatus.Nouveau || x.Status == ReportStatus.EnExamen), ct);

        var actions = await db.AdminActions
            .AsNoTracking()
            .Where(a => a.TargetType == AdminTargetType.Report && a.TargetId == r.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new Admin.Users.AdminActionDto(
                a.Id, a.Type, a.Reason, a.Admin != null ? a.Admin.DisplayName : "—", a.CreatedAt))
            .ToListAsync(ct);

        var notes = await db.AdminNotes
            .AsNoTracking()
            .Where(n => n.TargetType == AdminTargetType.Report && n.TargetId == r.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new Admin.Users.AdminNoteDto(
                n.Id, n.Body, n.Admin != null ? n.Admin.DisplayName : "—", n.CreatedAt))
            .ToListAsync(ct);

        return Result<ReportDetailDto>.Success(new ReportDetailDto(
            row,
            string.IsNullOrWhiteSpace(r.Evidence)
                ? []
                : r.Evidence.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            r.Resolution, r.ResolvedAt, r.HandledByAdmin?.DisplayName,
            others, actions, notes));
    }
}

/// <summary>Cambiar el estado del reporte. Cerrarlo exige explicar qué se ha decidido.</summary>
public record ChangeReportStatusCommand(
    Guid AdminId, Guid ReportId, ReportStatus Status, string? Resolution) : IRequest<Result>;

/// <summary>«Advertir» al usuario señalado.</summary>
public record WarnReportedUserCommand(Guid AdminId, Guid ReportId, string Message) : IRequest<Result>;

/// <summary>«Solicitar información» a quien reporta.</summary>
public record RequestReportInfoCommand(Guid AdminId, Guid ReportId, string Message) : IRequest<Result>;

public class ChangeReportStatusCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ChangeReportStatusCommand, Result>
{
    public async Task<Result> Handle(ChangeReportStatusCommand request, CancellationToken ct)
    {
        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);
        if (report is null) return Result.Failure("Report.NotFound");

        var resolution = string.IsNullOrWhiteSpace(request.Resolution) ? null : request.Resolution.Trim();

        // Cerrar un reporte sin decir qué se decidió deja a quien lo abrió sin respuesta
        // y al equipo sin memoria.
        if (request.Status is ReportStatus.Resolu or ReportStatus.Rejete && resolution is null)
            return Result.Failure("Report.ResolutionRequired");

        report.Status = request.Status;

        if (request.Status is ReportStatus.Resolu or ReportStatus.Rejete)
        {
            report.Resolution = resolution;
            report.ResolvedAt = DateTimeOffset.UtcNow;
            report.HandledByAdminId = request.AdminId;
        }
        else
        {
            report.ResolvedAt = null;
        }

        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.Report,
            TargetId   = report.Id,
            Type       = AdminActionType.ReportResolved,
            Reason     = resolution ?? request.Status.ToString()
        });

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class WarnReportedUserCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : IRequestHandler<WarnReportedUserCommand, Result>
{
    public async Task<Result> Handle(WarnReportedUserCommand request, CancellationToken ct)
    {
        var message = request.Message?.Trim();
        if (string.IsNullOrEmpty(message)) return Result.Failure("Admin.MessageRequired");

        var report = await db.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);

        if (report is null) return Result.Failure("Report.NotFound");
        if (report.ReportedUserId is not { } userId) return Result.Failure("Report.NoReportedUser");

        var notification = new UserNotification
        {
            UserId   = userId,
            Category = NotificationCategories.Admin,
            Title    = "Avertissement",
            Body     = message,
            Link     = "/ajustes"
        };

        db.UserNotifications.Add(notification);

        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.Report,
            TargetId   = report.Id,
            Type       = AdminActionType.UserWarned,
            Reason     = message
        });

        // La advertencia también cuenta en la ficha de quien la recibe.
        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.User,
            TargetId   = userId,
            Type       = AdminActionType.UserWarned,
            Reason     = $"#{report.PublicReference} — {message}"
        });

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result.Success();
    }
}

public class RequestReportInfoCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : IRequestHandler<RequestReportInfoCommand, Result>
{
    public async Task<Result> Handle(RequestReportInfoCommand request, CancellationToken ct)
    {
        var message = request.Message?.Trim();
        if (string.IsNullOrEmpty(message)) return Result.Failure("Admin.MessageRequired");

        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);
        if (report is null) return Result.Failure("Report.NotFound");

        // Pedir información mueve el reporte a examen: alguien lo está mirando.
        if (report.Status == ReportStatus.Nouveau) report.Status = ReportStatus.EnExamen;

        var notification = new UserNotification
        {
            UserId   = report.ReporterId,
            Category = NotificationCategories.Admin,
            Title    = $"Signalement #{report.PublicReference}",
            Body     = message,
            Link     = "/ajustes"
        };

        db.UserNotifications.Add(notification);

        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.Report,
            TargetId   = report.Id,
            Type       = AdminActionType.ReportInfoRequested,
            Reason     = message
        });

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result.Success();
    }
}
