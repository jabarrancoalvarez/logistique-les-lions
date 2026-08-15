using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Requests;

/// <summary>Hacerse cargo de una solicitud, o soltarla.</summary>
public record AssignRequestCommand(Guid AdminId, Guid RequestId, bool Assign) : IRequest<Result>;

/// <summary>
/// Nouvelle demande → En recherche → Véhicule proposé → Terminée / Annulée.
/// </summary>
public record ChangeRequestStatusCommand(
    Guid AdminId, Guid RequestId, VehicleRequestStatus Status, string? Reason) : IRequest<Result>;

/// <summary>Anexar un anuncio ya publicado en Yoon u Auto.</summary>
public record AddInternalProposalCommand(
    Guid AdminId, Guid RequestId, Guid VehicleId, string? Comments) : IRequest<Result<Guid>>;

/// <summary>Datos de un vehículo encontrado fuera de Yoon u Auto.</summary>
public record ExternalProposalInput(
    string MakeModel,
    string? Version,
    int? Year,
    int? Mileage,
    FuelType? FuelType,
    TransmissionType? Transmission,
    decimal? EstimatedPrice,
    decimal? AdditionalCosts,
    string? CountryOfOrigin,
    IReadOnlyList<string>? PhotoUrls,
    string? ExternalUrl,
    string? Comments
);

public record AddExternalProposalCommand(
    Guid AdminId, Guid RequestId, ExternalProposalInput Proposal) : IRequest<Result<Guid>>;

public record RemoveProposalCommand(Guid AdminId, Guid ProposalId) : IRequest<Result>;

/// <summary>Responder al usuario dentro de su solicitud.</summary>
public record ReplyToRequestCommand(Guid AdminId, Guid RequestId, string Body) : IRequest<Result>;

// ─── Handlers ──────────────────────────────────────────────────────────────

internal static class AdminRequestWorkflow
{
    /// <summary>
    /// Transiciones admitidas.
    /// </summary>
    /// <remarks>
    /// Una solicitud terminada o anulada no se reabre: si el usuario vuelve a necesitar
    /// un coche, crea otra, y así la anterior conserva lo que se hizo por ella.
    /// </remarks>
    private static readonly Dictionary<VehicleRequestStatus, VehicleRequestStatus[]> Allowed = new()
    {
        [VehicleRequestStatus.NouvelleDemande] =
            [VehicleRequestStatus.EnRecherche, VehicleRequestStatus.Annulee],
        [VehicleRequestStatus.EnRecherche] =
            [VehicleRequestStatus.VehiculePropose, VehicleRequestStatus.Terminee,
             VehicleRequestStatus.Annulee],
        [VehicleRequestStatus.VehiculePropose] =
            [VehicleRequestStatus.EnRecherche, VehicleRequestStatus.Terminee,
             VehicleRequestStatus.Annulee],
        [VehicleRequestStatus.Terminee] = [],
        [VehicleRequestStatus.Annulee] = []
    };

    public static bool CanTransition(VehicleRequestStatus from, VehicleRequestStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static void Log(
        IApplicationDbContext db, Guid adminId, Guid requestId,
        AdminActionType type, string? reason = null)
    {
        db.AdminActions.Add(new AdminAction
        {
            AdminId    = adminId,
            TargetType = AdminTargetType.Request,
            TargetId   = requestId,
            Type       = type,
            Reason     = reason
        });
    }

    /// <summary>Aviso al usuario: «Nous avons trouvé un véhicule pour vous».</summary>
    public static UserNotification NotifyProposal(
        IApplicationDbContext db, VehicleRequest request)
    {
        var notification = new UserNotification
        {
            UserId   = request.UserId,
            Category = NotificationCategories.RequestProposal,
            Title    = "Nous avons trouvé un véhicule pour vous",
            Body     = $"Demande #{request.PublicReference} : une proposition vous attend.",
            Link     = $"/mis-pedidos/{request.Id}"
        };

        db.UserNotifications.Add(notification);
        return notification;
    }
}

public class AssignRequestCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AssignRequestCommand, Result>
{
    public async Task<Result> Handle(AssignRequestCommand request, CancellationToken ct)
    {
        var entity = await db.VehicleRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (entity is null) return Result.Failure("VehicleRequest.NotFound");

        entity.AssignedAdminId = request.Assign ? request.AdminId : null;

        AdminRequestWorkflow.Log(db, request.AdminId, entity.Id, AdminActionType.RequestAssigned,
            request.Assign ? null : "Demande libérée");

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class ChangeRequestStatusCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ChangeRequestStatusCommand, Result>
{
    public async Task<Result> Handle(ChangeRequestStatusCommand request, CancellationToken ct)
    {
        var entity = await db.VehicleRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (entity is null) return Result.Failure("VehicleRequest.NotFound");

        if (entity.Status == request.Status) return Result.Success();

        if (!AdminRequestWorkflow.CanTransition(entity.Status, request.Status))
            return Result.Failure("VehicleRequest.InvalidTransition");

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        // Anular la solicitud de otra persona hay que explicarlo.
        if (request.Status == VehicleRequestStatus.Annulee && reason is null)
            return Result.Failure("Admin.ReasonRequired");

        entity.Status = request.Status;

        if (request.Status is VehicleRequestStatus.Terminee or VehicleRequestStatus.Annulee)
            entity.ClosedAt = DateTimeOffset.UtcNow;

        AdminRequestWorkflow.Log(db, request.AdminId, entity.Id,
            AdminActionType.RequestStatusChanged, reason ?? request.Status.ToString());

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class AddInternalProposalCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : IRequestHandler<AddInternalProposalCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddInternalProposalCommand request, CancellationToken ct)
    {
        var entity = await db.VehicleRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (entity is null) return Result<Guid>.Failure("VehicleRequest.NotFound");

        // Solo se propone lo que el usuario puede abrir: un borrador o un vehículo ya
        // vendido no le sirve de nada.
        var vehicle = await db.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, ct);

        if (vehicle is null || !vehicle.IsPubliclyVisible)
            return Result<Guid>.Failure("Vehicle.NotAvailable");

        if (await db.VehicleRequestProposals
                .AnyAsync(p => p.RequestId == entity.Id && p.VehicleId == vehicle.Id, ct))
            return Result<Guid>.Failure("VehicleRequest.ProposalAlreadyExists");

        var proposal = new VehicleRequestProposal
        {
            RequestId = entity.Id,
            VehicleId = vehicle.Id,
            Comments  = string.IsNullOrWhiteSpace(request.Comments) ? null : request.Comments.Trim()
        };

        db.VehicleRequestProposals.Add(proposal);

        // Proponer un coche mueve la solicitud: es lo que el usuario estaba esperando.
        if (entity.Status is VehicleRequestStatus.NouvelleDemande or VehicleRequestStatus.EnRecherche)
            entity.Status = VehicleRequestStatus.VehiculePropose;

        AdminRequestWorkflow.Log(db, request.AdminId, entity.Id, AdminActionType.RequestProposalAdded);
        var notification = AdminRequestWorkflow.NotifyProposal(db, entity);

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result<Guid>.Success(proposal.Id);
    }
}

public class AddExternalProposalCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : IRequestHandler<AddExternalProposalCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddExternalProposalCommand request, CancellationToken ct)
    {
        var entity = await db.VehicleRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (entity is null) return Result<Guid>.Failure("VehicleRequest.NotFound");

        var p = request.Proposal;

        if (string.IsNullOrWhiteSpace(p.MakeModel))
            return Result<Guid>.Failure("VehicleRequest.MakeModelRequired");

        if (p.EstimatedPrice is < 0 || p.AdditionalCosts is < 0)
            return Result<Guid>.Failure("VehicleRequest.InvalidPrice");

        var proposal = new VehicleRequestProposal
        {
            RequestId       = entity.Id,
            MakeModel       = p.MakeModel.Trim(),
            Version         = Clean(p.Version),
            Year            = p.Year,
            Mileage         = p.Mileage,
            FuelType        = p.FuelType,
            Transmission    = p.Transmission,
            EstimatedPrice  = p.EstimatedPrice,
            AdditionalCosts = p.AdditionalCosts,
            CountryOfOrigin = Clean(p.CountryOfOrigin),
            PhotoUrls       = p.PhotoUrls is { Count: > 0 }
                ? string.Join('\n', p.PhotoUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
                : null,
            ExternalUrl     = Clean(p.ExternalUrl),
            Comments        = Clean(p.Comments)
        };

        db.VehicleRequestProposals.Add(proposal);

        if (entity.Status is VehicleRequestStatus.NouvelleDemande or VehicleRequestStatus.EnRecherche)
            entity.Status = VehicleRequestStatus.VehiculePropose;

        AdminRequestWorkflow.Log(db, request.AdminId, entity.Id, AdminActionType.RequestProposalAdded);
        var notification = AdminRequestWorkflow.NotifyProposal(db, entity);

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result<Guid>.Success(proposal.Id);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public class RemoveProposalCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RemoveProposalCommand, Result>
{
    public async Task<Result> Handle(RemoveProposalCommand request, CancellationToken ct)
    {
        var proposal = await db.VehicleRequestProposals
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId, ct);

        if (proposal is null) return Result.Failure("VehicleRequest.ProposalNotFound");

        proposal.DeletedAt = DateTimeOffset.UtcNow;

        AdminRequestWorkflow.Log(db, request.AdminId, proposal.RequestId,
            AdminActionType.RequestProposalRemoved);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class ReplyToRequestCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : IRequestHandler<ReplyToRequestCommand, Result>
{
    public async Task<Result> Handle(ReplyToRequestCommand request, CancellationToken ct)
    {
        var body = request.Body.Trim();
        if (string.IsNullOrEmpty(body)) return Result.Failure("VehicleRequest.MessageRequired");

        var entity = await db.VehicleRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (entity is null) return Result.Failure("VehicleRequest.NotFound");

        db.VehicleRequestMessages.Add(new VehicleRequestMessage
        {
            RequestId = entity.Id,
            SenderId  = request.AdminId,
            Body      = body
        });

        var notification = new UserNotification
        {
            UserId   = entity.UserId,
            Category = NotificationCategories.Message,
            Title    = "Réponse de Yoon u Auto",
            Body     = $"Demande #{entity.PublicReference} : {body}",
            Link     = $"/mis-pedidos/{entity.Id}"
        };

        db.UserNotifications.Add(notification);

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result.Success();
    }
}
