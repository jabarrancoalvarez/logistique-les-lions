using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Negotiations;

/// <summary>Detalle de una negociación: anuncio, partes y cronología.</summary>
public record GetNegotiationQuery(Guid UserId, Guid NegotiationId)
    : IRequest<Result<NegotiationDetailDto>>;

public record NegotiationDetailDto(
    Guid Id,
    NegotiationStatus Status,
    bool IsBuyer,

    Guid VehicleId,
    string VehicleSlug,
    string VehicleTitle,
    string VehiclePublicReference,
    decimal VehiclePrice,
    VehicleStatus VehicleStatus,
    string? VehicleThumbnailUrl,
    /// <summary>Un anuncio vendido deja de admitir ofertas y nuevos contactos.</summary>
    bool AcceptsNegotiation,

    Guid OtherUserId,
    string OtherUserName,
    string? OtherUserAvatarUrl,

    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActivityAt,
    /// <summary>Cronología única de la operación.</summary>
    IReadOnlyList<NegotiationEventDto> Timeline,
    /// <summary>Historial de ofertas, de la más reciente a la más antigua.</summary>
    IReadOnlyList<OfferDto> Offers,
    /// <summary>Oferta viva, si la hay. Es la única que admite respuesta.</summary>
    OfferDto? PendingOffer
);

/// <param name="ByMe">La hizo el usuario que consulta.</param>
/// <param name="CanRespond">
/// El usuario puede aceptarla, rechazarla o contraofertar: está pendiente y la hizo
/// la otra parte.
/// </param>
public record OfferDto(
    Guid Id,
    decimal Amount,
    decimal ListedPrice,
    string? Message,
    OfferStatus Status,
    bool ByMe,
    bool CanRespond,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt
);

public record NegotiationEventDto(
    Guid Id,
    NegotiationEventType Type,
    decimal? Amount,
    bool ByMe,
    DateTimeOffset CreatedAt
);

public class GetNegotiationQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetNegotiationQuery, Result<NegotiationDetailDto>>
{
    public async Task<Result<NegotiationDetailDto>> Handle(
        GetNegotiationQuery request, CancellationToken ct)
    {
        var n = await db.Negotiations
            .AsNoTracking()
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Include(x => x.Vehicle).ThenInclude(v => v.Images)
            .Include(x => x.Events)
            .Include(x => x.Offers)
            .FirstOrDefaultAsync(x => x.Id == request.NegotiationId, ct);

        if (n is null)
            return Result<NegotiationDetailDto>.Failure("Negotiation.NotFound");

        // Solo las dos partes pueden abrir la negociación.
        if (!n.Involves(request.UserId))
            return Result<NegotiationDetailDto>.Failure("Negotiation.AccessDenied");

        var isBuyer = n.BuyerId == request.UserId;
        var other = isBuyer ? n.Seller : n.Buyer;
        var thumb = n.Vehicle.Images.FirstOrDefault(i => i.IsPrimary)?.ThumbnailUrl
                 ?? n.Vehicle.Images.FirstOrDefault()?.ThumbnailUrl;

        // Se ordena por Sequence: varios hitos de una misma operación comparten
        // marca de tiempo y CreatedAt no los distingue.
        var timeline = n.Events
            .OrderBy(e => e.Sequence)
            .Select(e => new NegotiationEventDto(
                e.Id, e.Type, e.Amount, e.ActorId == request.UserId, e.CreatedAt))
            .ToList();

        var offers = n.Offers
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OfferDto(
                o.Id, o.Amount, o.ListedPrice, o.Message, o.Status,
                o.FromUserId == request.UserId,
                // Solo la otra parte responde, y solo mientras la oferta siga viva y el
                // anuncio siga admitiendo negociación.
                o.IsPending && o.FromUserId != request.UserId && n.Vehicle.AcceptsNegotiation,
                o.CreatedAt, o.RespondedAt))
            .ToList();

        var dto = new NegotiationDetailDto(
            n.Id, n.Status, isBuyer,
            n.VehicleId, n.Vehicle.Slug, n.Vehicle.Title, n.Vehicle.PublicReference,
            n.Vehicle.Price, n.Vehicle.Status, thumb, n.Vehicle.AcceptsNegotiation,
            other.Id, other.DisplayName, other.AvatarUrl,
            n.CreatedAt, n.LastActivityAt ?? n.LastMessageAt,
            timeline,
            offers,
            offers.FirstOrDefault(o => o.Status == OfferStatus.EnAttente));

        return Result<NegotiationDetailDto>.Success(dto);
    }
}
