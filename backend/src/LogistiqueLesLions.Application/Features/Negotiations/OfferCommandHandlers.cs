using System.Globalization;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Negotiations;

/// <summary>
/// Piezas compartidas por los comandos de oferta.
/// </summary>
internal static class OfferWorkflow
{
    /// <summary>Formato del documento: 8.300.000</summary>
    public static string Format(decimal amount) =>
        amount.ToString("N0", CultureInfo.GetCultureInfo("de-DE"));

    /// <summary>
    /// Deja la oferta pendiente anterior como superada. Solo puede haber una viva en
    /// cada negociación: si no, aceptar una dejaría las otras en un limbo.
    /// </summary>
    public static async Task SupersedePendingAsync(
        IApplicationDbContext db, Guid negotiationId, CancellationToken ct)
    {
        var pending = await db.Offers
            .Where(o => o.NegotiationId == negotiationId && o.Status == OfferStatus.EnAttente)
            .ToListAsync(ct);

        foreach (var offer in pending)
        {
            offer.Status = OfferStatus.ContreOfferte;
            offer.RespondedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Abre la cronología de una negociación para añadirle hitos en orden.
    /// </summary>
    public static async Task<Timeline> OpenTimelineAsync(
        IApplicationDbContext db, Guid negotiationId, CancellationToken ct)
    {
        var last = await db.NegotiationEvents
            .Where(e => e.NegotiationId == negotiationId)
            .MaxAsync(e => (int?)e.Sequence, ct) ?? 0;

        return new Timeline(db, negotiationId, last);
    }

    /// <summary>
    /// Añade hitos numerándolos consecutivamente. Lleva la cuenta en memoria porque
    /// varios hitos de una misma operación se guardan a la vez y no pueden consultarse
    /// entre sí en la base de datos.
    /// </summary>
    public sealed class Timeline(IApplicationDbContext db, Guid negotiationId, int lastSequence)
    {
        private int _sequence = lastSequence;

        public void Add(NegotiationEventType type, Guid actorId, decimal? amount = null)
        {
            db.NegotiationEvents.Add(new NegotiationEvent
            {
                NegotiationId = negotiationId,
                Sequence      = ++_sequence,
                Type          = type,
                ActorId       = actorId,
                Amount        = amount
            });
        }
    }

    /// <summary>
    /// Deja la notificación persistida y la devuelve para poder empujarla en vivo
    /// <b>después</b> de guardar.
    /// </summary>
    public static UserNotification Notify(
        IApplicationDbContext db, Guid userId, string title, string body, string link)
    {
        var notification = new UserNotification
        {
            UserId   = userId,
            Category = NotificationCategories.Offer,
            Title    = title,
            Body     = body,
            Link     = link
        };
        db.UserNotifications.Add(notification);
        return notification;
    }
}

public class MakeOfferCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : IRequestHandler<MakeOfferCommand, Result<MakeOfferResultDto>>
{
    public async Task<Result<MakeOfferResultDto>> Handle(MakeOfferCommand request, CancellationToken ct)
    {
        var vehicle = await db.Vehicles
            .Include(v => v.Make)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, ct);

        if (vehicle is null) return Result<MakeOfferResultDto>.Failure("Vehicle.NotFound");

        if (!vehicle.AcceptsNegotiation)
            return Result<MakeOfferResultDto>.Failure("Vehicle.NotOpenForNegotiation");

        if (vehicle.SellerId == request.UserId)
            return Result<MakeOfferResultDto>.Failure("Offer.CannotOfferOnOwnVehicle");

        if (request.Amount is <= 0)
            return Result<MakeOfferResultDto>.Failure("Offer.InvalidAmount");

        var now = DateTimeOffset.UtcNow;

        var negotiation = await db.Negotiations
            .FirstOrDefaultAsync(n => n.BuyerId == request.UserId
                                   && n.SellerId == vehicle.SellerId
                                   && n.VehicleId == vehicle.Id, ct);

        var isNew = negotiation is null;

        if (negotiation is null)
        {
            negotiation = new Negotiation
            {
                BuyerId   = request.UserId,
                SellerId  = vehicle.SellerId,
                VehicleId = vehicle.Id,
                Status    = NegotiationStatus.EnCours
            };
            db.Negotiations.Add(negotiation);
        }

        var timeline = isNew
            ? new OfferWorkflow.Timeline(db, negotiation.Id, 0)
            : await OfferWorkflow.OpenTimelineAsync(db, negotiation.Id, ct);

        if (isNew)
            timeline.Add(NegotiationEventType.ConversationStarted, request.UserId);

        // El mensaje, si lo hay, entra en el hilo como cualquier otro.
        if (!string.IsNullOrWhiteSpace(request.Message))
        {
            db.Messages.Add(new Message
            {
                NegotiationId = negotiation.Id,
                SenderId      = request.UserId,
                Body          = request.Message.Trim()
            });
            negotiation.LastMessageAt = now;
        }

        Guid? offerId = null;
        UserNotification? notification = null;

        // Sin importe no hay oferta: la especificación lo marca como opcional, y el
        // formulario sirve entonces solo para iniciar la conversación.
        if (request.Amount is { } amount)
        {
            await OfferWorkflow.SupersedePendingAsync(db, negotiation.Id, ct);

            var offer = new Offer
            {
                NegotiationId = negotiation.Id,
                FromUserId    = request.UserId,
                Amount        = amount,
                ListedPrice   = vehicle.Price,
                Message       = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim()
            };
            db.Offers.Add(offer);
            offerId = offer.Id;

            timeline.Add(NegotiationEventType.OfferMade, request.UserId, amount);

            // Mientras la oferta espera respuesta, la negociación queda a la espera.
            negotiation.Status = NegotiationStatus.EnAttente;

            notification = OfferWorkflow.Notify(db, vehicle.SellerId,
                "Nouvelle offre",
                $"Vous avez reçu une offre de {OfferWorkflow.Format(amount)} FCFA pour votre {vehicle.Make.Name}.",
                $"/mis-negociaciones/{negotiation.Id}");
        }

        negotiation.LastActivityAt = now;
        await db.SaveChangesAsync(ct);

        await pusher.PushAsync(notification is null ? [] : [notification], ct);

        return Result<MakeOfferResultDto>.Success(new MakeOfferResultDto(negotiation.Id, offerId));
    }
}

public class CounterOfferCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : IRequestHandler<CounterOfferCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CounterOfferCommand request, CancellationToken ct)
    {
        if (request.Amount <= 0) return Result<Guid>.Failure("Offer.InvalidAmount");

        var negotiation = await db.Negotiations
            .Include(n => n.Vehicle)
            .FirstOrDefaultAsync(n => n.Id == request.NegotiationId, ct);

        if (negotiation is null) return Result<Guid>.Failure("Negotiation.NotFound");
        if (!negotiation.Involves(request.UserId)) return Result<Guid>.Failure("Negotiation.AccessDenied");

        if (!negotiation.Vehicle.AcceptsNegotiation)
            return Result<Guid>.Failure("Vehicle.NotOpenForNegotiation");

        var pending = await db.Offers
            .Where(o => o.NegotiationId == negotiation.Id && o.Status == OfferStatus.EnAttente)
            .FirstOrDefaultAsync(ct);

        // Contraofertar la oferta propia no tiene sentido: sería negociar consigo mismo.
        if (pending is not null && pending.FromUserId == request.UserId)
            return Result<Guid>.Failure("Offer.AlreadyYourTurn");

        await OfferWorkflow.SupersedePendingAsync(db, negotiation.Id, ct);

        var counter = new Offer
        {
            NegotiationId    = negotiation.Id,
            FromUserId       = request.UserId,
            Amount           = request.Amount,
            ListedPrice      = negotiation.Vehicle.Price,
            Message          = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            RepliesToOfferId = pending?.Id
        };
        db.Offers.Add(counter);

        var timeline = await OfferWorkflow.OpenTimelineAsync(db, negotiation.Id, ct);
        timeline.Add(
            pending is null ? NegotiationEventType.OfferMade : NegotiationEventType.CounterOffer,
            request.UserId, request.Amount);

        negotiation.Status         = NegotiationStatus.EnAttente;
        negotiation.LastActivityAt = DateTimeOffset.UtcNow;

        var recipientId = negotiation.BuyerId == request.UserId ? negotiation.SellerId : negotiation.BuyerId;
        var notification = OfferWorkflow.Notify(db, recipientId,
            pending is null ? "Nouvelle offre" : "Contre-offre",
            $"{OfferWorkflow.Format(request.Amount)} FCFA pour {negotiation.Vehicle.Title}.",
            $"/mis-negociaciones/{negotiation.Id}");

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result<Guid>.Success(counter.Id);
    }
}

/// <summary>Aceptar y rechazar comparten casi todas las comprobaciones.</summary>
public abstract class OfferResponseHandlerBase(IApplicationDbContext db)
{
    protected async Task<(Offer? offer, Negotiation? negotiation, string? error)> LoadAsync(
        Guid userId, Guid offerId, CancellationToken ct)
    {
        var offer = await db.Offers
            .Include(o => o.Negotiation).ThenInclude(n => n.Vehicle)
            .FirstOrDefaultAsync(o => o.Id == offerId, ct);

        if (offer is null) return (null, null, "Offer.NotFound");

        var negotiation = offer.Negotiation;
        if (!negotiation.Involves(userId)) return (null, null, "Negotiation.AccessDenied");

        if (!offer.IsPending) return (null, null, "Offer.NotPending");

        // Responder a la propia oferta no es una negociación: la respuesta corresponde
        // siempre a la otra parte.
        if (offer.FromUserId == userId) return (null, null, "Offer.CannotRespondToOwn");

        return (offer, negotiation, null);
    }
}

public class AcceptOfferCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : OfferResponseHandlerBase(db), IRequestHandler<AcceptOfferCommand, Result>
{
    public async Task<Result> Handle(AcceptOfferCommand request, CancellationToken ct)
    {
        var (offer, negotiation, error) = await LoadAsync(request.UserId, request.OfferId, ct);
        if (error is not null) return Result.Failure(error);

        var now = DateTimeOffset.UtcNow;

        offer!.Status      = OfferStatus.Acceptee;
        offer.RespondedAt  = now;

        var timeline = await OfferWorkflow.OpenTimelineAsync(db, negotiation!.Id, ct);
        timeline.Add(NegotiationEventType.OfferAccepted, request.UserId, offer.Amount);

        // Aceptada la oferta, la negociación vuelve a estar en curso: toca el contrato.
        negotiation.Status         = NegotiationStatus.EnCours;
        negotiation.LastActivityAt = now;

        var notification = OfferWorkflow.Notify(db, offer.FromUserId,
            "Offre acceptée",
            $"Votre offre de {OfferWorkflow.Format(offer.Amount)} FCFA a été acceptée.",
            $"/mis-negociaciones/{negotiation.Id}");

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result.Success();
    }
}

public class RejectOfferCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : OfferResponseHandlerBase(db), IRequestHandler<RejectOfferCommand, Result>
{
    public async Task<Result> Handle(RejectOfferCommand request, CancellationToken ct)
    {
        var (offer, negotiation, error) = await LoadAsync(request.UserId, request.OfferId, ct);
        if (error is not null) return Result.Failure(error);

        var now = DateTimeOffset.UtcNow;

        offer!.Status     = OfferStatus.Refusee;
        offer.RespondedAt = now;

        var timeline = await OfferWorkflow.OpenTimelineAsync(db, negotiation!.Id, ct);
        timeline.Add(NegotiationEventType.OfferRejected, request.UserId, offer.Amount);

        // Rechazar no cierra la negociación: se puede seguir hablando o contraofertar.
        negotiation.Status         = NegotiationStatus.EnCours;
        negotiation.LastActivityAt = now;

        var notification = OfferWorkflow.Notify(db, offer.FromUserId,
            "Offre refusée",
            $"Votre offre de {OfferWorkflow.Format(offer.Amount)} FCFA a été refusée.",
            $"/mis-negociaciones/{negotiation.Id}");

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync([notification], ct);

        return Result.Success();
    }
}
