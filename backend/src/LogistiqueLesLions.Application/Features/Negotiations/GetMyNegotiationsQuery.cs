using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Negotiations;

/// <summary>
/// Mes négociations. Centraliza todo lo que sucede desde que hay interés real por un
/// vehículo hasta que la compraventa termina.
/// </summary>
/// <param name="Status">Filtro por pestaña: En cours · En attente · Terminées.</param>
public record GetMyNegotiationsQuery(Guid UserId, NegotiationStatus? Status = null)
    : IRequest<Result<List<NegotiationSummaryDto>>>;

/// <param name="IsBuyer">El usuario es el interesado; si no, es quien publica.</param>
public record NegotiationSummaryDto(
    Guid Id,
    NegotiationStatus Status,
    bool IsBuyer,

    // ─── Anuncio ───────────────────────────────────────────────────────────
    Guid VehicleId,
    string VehicleSlug,
    string VehicleTitle,
    string VehiclePublicReference,
    decimal VehiclePrice,
    VehicleStatus VehicleStatus,
    string? VehicleThumbnailUrl,

    // ─── La otra parte ─────────────────────────────────────────────────────
    Guid OtherUserId,
    string OtherUserName,
    string? OtherUserAvatarUrl,

    // ─── Actividad ─────────────────────────────────────────────────────────
    string? LastMessage,
    DateTimeOffset? LastActivityAt,
    int UnreadCount
);

public class GetMyNegotiationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMyNegotiationsQuery, Result<List<NegotiationSummaryDto>>>
{
    public async Task<Result<List<NegotiationSummaryDto>>> Handle(
        GetMyNegotiationsQuery request, CancellationToken ct)
    {
        var query = db.Negotiations
            .AsNoTracking()
            .Where(n => n.BuyerId == request.UserId || n.SellerId == request.UserId);

        if (request.Status.HasValue)
            query = query.Where(n => n.Status == request.Status.Value);

        var negotiations = await query
            .Include(n => n.Buyer)
            .Include(n => n.Seller)
            .Include(n => n.Vehicle).ThenInclude(v => v.Images)
            .Include(n => n.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .OrderByDescending(n => n.LastActivityAt ?? n.LastMessageAt ?? n.CreatedAt)
            .ToListAsync(ct);

        var unreadByNegotiation = await db.Messages
            .AsNoTracking()
            .Where(m => !m.IsRead && m.SenderId != request.UserId)
            .GroupBy(m => m.NegotiationId)
            .Select(g => new { NegotiationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.NegotiationId, x => x.Count, ct);

        var items = negotiations.Select(n =>
        {
            var isBuyer = n.BuyerId == request.UserId;
            var other = isBuyer ? n.Seller : n.Buyer;
            var thumb = n.Vehicle.Images.FirstOrDefault(i => i.IsPrimary)?.ThumbnailUrl
                     ?? n.Vehicle.Images.FirstOrDefault()?.ThumbnailUrl;

            return new NegotiationSummaryDto(
                n.Id,
                n.Status,
                isBuyer,
                n.VehicleId,
                n.Vehicle.Slug,
                n.Vehicle.Title,
                n.Vehicle.PublicReference,
                n.Vehicle.Price,
                n.Vehicle.Status,
                thumb,
                other.Id,
                other.DisplayName,
                other.AvatarUrl,
                n.Messages.FirstOrDefault()?.Body,
                n.LastActivityAt ?? n.LastMessageAt,
                unreadByNegotiation.GetValueOrDefault(n.Id));
        }).ToList();

        return Result<List<NegotiationSummaryDto>>.Success(items);
    }
}
