using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Listings;

/// <summary>Acciones que el administrador puede ejercer sobre un anuncio.</summary>
public enum AdminListingAction
{
    /// <summary>Ocultar temporalmente del Marketplace.</summary>
    Hide = 1,
    /// <summary>Levantar la ocultación.</summary>
    Reactivate = 2,
    /// <summary>Marcar para revisión. No oculta nada.</summary>
    Flag = 3,
    /// <summary>Retirar la marca de revisión.</summary>
    Unflag = 4,
    /// <summary>Archivar.</summary>
    Archive = 5,
    /// <summary>Eliminar (soft delete).</summary>
    Delete = 6
}

/// <remarks>
/// ⚠️ Aquí no se toca la información comercial —marca, kilómetros, precio…—: pertenece a
/// quien publica. Ante un dato incorrecto se usa
/// <see cref="RequestListingCorrectionCommand"/>.
/// </remarks>
public record ApplyAdminListingActionCommand(
    Guid AdminId,
    Guid VehicleId,
    AdminListingAction Action,
    string? Reason
) : IRequest<Result>;

/// <summary>
/// «Contactar con el usuario»: pedirle que corrija su anuncio.
/// </summary>
public record RequestListingCorrectionCommand(
    Guid AdminId,
    Guid VehicleId,
    string Message
) : IRequest<Result>;

// ─── Handlers ──────────────────────────────────────────────────────────────

public class ApplyAdminListingActionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ApplyAdminListingActionCommand, Result>
{
    public async Task<Result> Handle(ApplyAdminListingActionCommand request, CancellationToken ct)
    {
        var vehicle = await db.Vehicles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.DeletedAt == null, ct);

        if (vehicle is null) return Result.Failure("Vehicle.NotFound");

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        // Toda medida que afecta a lo que el usuario ve exige explicarse: es lo que
        // queda escrito cuando reclame.
        var needsReason = request.Action is AdminListingAction.Hide
            or AdminListingAction.Archive or AdminListingAction.Delete;

        if (needsReason && reason is null) return Result.Failure("Admin.ReasonRequired");

        var now = DateTimeOffset.UtcNow;

        switch (request.Action)
        {
            case AdminListingAction.Hide:
                vehicle.AdminHiddenAt = now;
                break;

            case AdminListingAction.Reactivate:
                if (vehicle.AdminHiddenAt is null) return Result.Failure("Listing.NotHidden");
                vehicle.AdminHiddenAt = null;
                break;

            case AdminListingAction.Flag:
                vehicle.AdminFlaggedAt = now;
                break;

            case AdminListingAction.Unflag:
                vehicle.AdminFlaggedAt = null;
                break;

            case AdminListingAction.Archive:
                vehicle.Status = VehicleStatus.Archive;
                break;

            case AdminListingAction.Delete:
                vehicle.DeletedAt = now;
                break;
        }

        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.Listing,
            TargetId   = vehicle.Id,
            Type = request.Action switch
            {
                AdminListingAction.Hide       => AdminActionType.ListingHidden,
                AdminListingAction.Reactivate => AdminActionType.ListingReactivated,
                AdminListingAction.Flag       => AdminActionType.ListingFlagged,
                AdminListingAction.Unflag     => AdminActionType.ListingReactivated,
                AdminListingAction.Archive    => AdminActionType.ListingArchived,
                _                             => AdminActionType.ListingDeleted
            },
            Reason = reason
        });

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

/// <remarks>
/// La especificación lo dice sin rodeos: si hay información incorrecta, <b>lo normal es
/// solicitar su corrección</b>, no reescribirla desde el backoffice. Este comando es esa
/// vía: avisa a quien publica y deja constancia de que se le avisó.
/// </remarks>
public class RequestListingCorrectionCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : IRequestHandler<RequestListingCorrectionCommand, Result>
{
    public async Task<Result> Handle(RequestListingCorrectionCommand request, CancellationToken ct)
    {
        var message = request.Message.Trim();
        if (string.IsNullOrEmpty(message)) return Result.Failure("Admin.MessageRequired");

        var vehicle = await db.Vehicles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.DeletedAt == null, ct);

        if (vehicle is null) return Result.Failure("Vehicle.NotFound");

        var notification = new UserNotification
        {
            UserId   = vehicle.SellerId,
            Category = NotificationCategories.Admin,
            Title    = "Correction demandée",
            Body     = $"Annonce #{vehicle.PublicReference} : {message}",
            Link     = $"/mis-vehiculos"
        };

        db.UserNotifications.Add(notification);

        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.Listing,
            TargetId   = vehicle.Id,
            Type       = AdminActionType.ListingCorrectionRequested,
            Reason     = message
        });

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result.Success();
    }
}
