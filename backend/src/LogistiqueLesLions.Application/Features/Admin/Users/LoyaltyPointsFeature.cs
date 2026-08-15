using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Users;

/// <summary>
/// Puntos de fidelización de un usuario: saldo y libro de movimientos.
/// </summary>
/// <remarks>
/// La especificación pide poder consultar «saldo, origen, fecha y movimiento», y pone
/// como ejemplo justo los dos casos que existen: la venta verificada, que los genera
/// sola, y el ajuste del administrador, que exige motivo.
/// </remarks>
public record GetUserPointsQuery(Guid UserId) : IRequest<Result<UserPointsDto>>;

public record UserPointsDto(
    Guid UserId,
    string DisplayName,
    int Balance,
    int VerifiedSalesCount,
    IReadOnlyList<PointEntryDto> Entries
);

/// <param name="ContractReference">
/// Referencia del contrato (<c>YC00125</c>) cuando el movimiento viene de una venta.
/// </param>
public record PointEntryDto(
    Guid Id,
    int Points,
    LoyaltyPointOrigin Origin,
    string? ContractReference,
    string? AdminName,
    string? Note,
    DateTimeOffset At
);

public class GetUserPointsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUserPointsQuery, Result<UserPointsDto>>
{
    public async Task<Result<UserPointsDto>> Handle(
        GetUserPointsQuery request, CancellationToken ct)
    {
        var user = await db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null) return Result<UserPointsDto>.Failure("User.NotFound");

        var entries = await db.LoyaltyPointEntries
            .AsNoTracking()
            .Where(e => e.UserId == request.UserId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new PointEntryDto(
                e.Id, e.Points, e.Origin, e.ContractReference,
                e.Admin != null ? e.Admin.DisplayName : null,
                e.Note, e.CreatedAt))
            .ToListAsync(ct);

        return Result<UserPointsDto>.Success(new UserPointsDto(
            user.Id, user.DisplayName, user.LoyaltyPoints, user.VerifiedSalesCount, entries));
    }
}

// ─── Ajuste manual ──────────────────────────────────────────────────────────

public record AdjustUserPointsCommand(Guid AdminId, Guid UserId, int Points, string Reason)
    : IRequest<Result>;

public class AdjustUserPointsCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AdjustUserPointsCommand, Result>
{
    public async Task<Result> Handle(AdjustUserPointsCommand request, CancellationToken ct)
    {
        var reason = request.Reason?.Trim();

        // Un ajuste sin motivo es un número que aparece de la nada en el saldo de una
        // persona: la trazabilidad es toda la razón de ser del libro.
        if (string.IsNullOrEmpty(reason)) return Result.Failure("Admin.ReasonRequired");
        if (request.Points == 0) return Result.Failure("Points.ZeroAdjustment");

        var user = await db.UserProfiles.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null) return Result.Failure("User.NotFound");

        var before = user.LoyaltyPoints;

        LoyaltyPointsService.Add(db, user, request.Points,
            LoyaltyPointOrigin.AjustementAdministrateur,
            adminId: request.AdminId, note: reason);

        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.User,
            TargetId   = user.Id,
            Type       = AdminActionType.PointsAdjusted,
            Reason     = reason,
            OldValue   = before.ToString(),
            NewValue   = user.LoyaltyPoints.ToString()
        });

        db.UserNotifications.Add(new UserNotification
        {
            UserId   = user.Id,
            Category = NotificationCategories.Admin,
            Title    = request.Points > 0 ? "Points ajoutés" : "Points retirés",
            Body     = $"{(request.Points > 0 ? "+" : "")}{request.Points} points : {reason}",
            Link     = "/perfil"
        });

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
