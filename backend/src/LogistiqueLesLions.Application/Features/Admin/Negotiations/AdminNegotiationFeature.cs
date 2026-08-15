using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Negotiations;

/// <summary>
/// «Gestion des négociations» — datos <b>estructurales</b> de las negociaciones.
/// </summary>
/// <remarks>
/// El administrador no participa en negociaciones privadas entre usuarios. Esta pantalla
/// enseña quién habla con quién, sobre qué anuncio y en qué punto está la operación,
/// pero <b>no el contenido de las conversaciones</b>: para eso hace falta un motivo, y
/// queda registrado.
/// </remarks>
public record GetAdminNegotiationsQuery(
    string? Search = null,
    NegotiationStatus? Status = null,
    Guid? VehicleId = null,
    Guid? UserId = null,
    bool? WithContract = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<AdminNegotiationListDto>>;

public record AdminNegotiationListDto(
    int TotalCount, int Page, int PageSize, IReadOnlyList<AdminNegotiationRowDto> Items);

public record AdminNegotiationRowDto(
    Guid Id,
    Guid VehicleId,
    string VehicleReference,
    string VehicleTitle,
    Guid BuyerId,
    string BuyerName,
    Guid SellerId,
    string SellerName,
    NegotiationStatus Status,
    int OffersCount,
    int MessagesCount,
    Guid? ContractId,
    string? ContractReference,
    ContractStatus? ContractStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActivityAt
);

public class GetAdminNegotiationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminNegotiationsQuery, Result<AdminNegotiationListDto>>
{
    public async Task<Result<AdminNegotiationListDto>> Handle(
        GetAdminNegotiationsQuery request, CancellationToken ct)
    {
        var query = db.Negotiations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(n =>
                n.Vehicle.PublicReference.ToLower().Contains(term)
                || n.Vehicle.Title.ToLower().Contains(term));
        }

        if (request.Status is { } status) query = query.Where(n => n.Status == status);
        if (request.VehicleId is { } vehicleId) query = query.Where(n => n.VehicleId == vehicleId);

        if (request.UserId is { } userId)
            query = query.Where(n => n.BuyerId == userId || n.SellerId == userId);

        var contracts = db.Contracts.AsNoTracking();

        if (request.WithContract is { } withContract)
        {
            query = withContract
                ? query.Where(n => contracts.Any(c => c.NegotiationId == n.Id))
                : query.Where(n => !contracts.Any(c => c.NegotiationId == n.Id));
        }

        var total = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var rows = await query
            .Include(n => n.Vehicle)
            .Include(n => n.Buyer)
            .Include(n => n.Seller)
            .Include(n => n.Offers)
            .Include(n => n.Messages)
            .OrderByDescending(n => n.LastActivityAt ?? n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new AdminNegotiationRowDto(
                n.Id, n.VehicleId, n.Vehicle.PublicReference, n.Vehicle.Title,
                n.BuyerId, n.Buyer.DisplayName,
                n.SellerId, n.Seller.DisplayName,
                n.Status,
                n.Offers.Count,
                // Cuántos mensajes hay, no lo que dicen.
                n.Messages.Count,
                contracts.Where(c => c.NegotiationId == n.Id)
                    .Select(c => (Guid?)c.Id).FirstOrDefault(),
                contracts.Where(c => c.NegotiationId == n.Id)
                    .Select(c => c.PublicReference).FirstOrDefault(),
                contracts.Where(c => c.NegotiationId == n.Id)
                    .Select(c => (ContractStatus?)c.Status).FirstOrDefault(),
                n.CreatedAt, n.LastActivityAt))
            .ToListAsync(ct);

        return Result<AdminNegotiationListDto>.Success(
            new AdminNegotiationListDto(total, page, pageSize, rows));
    }
}

/// <summary>Ficha estructural de una negociación: sin el contenido de los mensajes.</summary>
public record GetAdminNegotiationQuery(Guid NegotiationId)
    : IRequest<Result<AdminNegotiationDetailDto>>;

public record AdminNegotiationDetailDto(
    AdminNegotiationRowDto Negotiation,
    IReadOnlyList<AdminOfferDto> Offers,
    IReadOnlyList<AdminTimelineDto> Timeline,
    /// <summary>Accesos al contenido, si los ha habido.</summary>
    IReadOnlyList<Users.AdminActionDto> Actions
);

public record AdminOfferDto(
    Guid Id, decimal Amount, decimal ListedPrice, OfferStatus Status,
    bool FromBuyer, DateTimeOffset CreatedAt);

public record AdminTimelineDto(NegotiationEventType Type, decimal? Amount, DateTimeOffset CreatedAt);

public class GetAdminNegotiationQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminNegotiationQuery, Result<AdminNegotiationDetailDto>>
{
    public async Task<Result<AdminNegotiationDetailDto>> Handle(
        GetAdminNegotiationQuery request, CancellationToken ct)
    {
        var n = await db.Negotiations
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Include(x => x.Offers)
            .Include(x => x.Events)
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == request.NegotiationId, ct);

        if (n is null) return Result<AdminNegotiationDetailDto>.Failure("Negotiation.NotFound");

        var contract = await db.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.NegotiationId == n.Id, ct);

        var row = new AdminNegotiationRowDto(
            n.Id, n.VehicleId, n.Vehicle.PublicReference, n.Vehicle.Title,
            n.BuyerId, n.Buyer.DisplayName, n.SellerId, n.Seller.DisplayName,
            n.Status, n.Offers.Count, n.Messages.Count,
            contract?.Id, contract?.PublicReference, contract?.Status,
            n.CreatedAt, n.LastActivityAt);

        // Las ofertas y la cronología sí son estructura: importes y hitos, no lo que se
        // dijeron por chat.
        var offers = n.Offers
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new AdminOfferDto(
                o.Id, o.Amount, o.ListedPrice, o.Status, o.FromUserId == n.BuyerId, o.CreatedAt))
            .ToList();

        var timeline = n.Events
            .OrderBy(e => e.Sequence)
            .Select(e => new AdminTimelineDto(e.Type, e.Amount, e.CreatedAt))
            .ToList();

        var actions = await db.AdminActions
            .AsNoTracking()
            .Where(a => a.TargetType == AdminTargetType.Negotiation && a.TargetId == n.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new Users.AdminActionDto(
                a.Id, a.Type, a.Reason, a.Admin != null ? a.Admin.DisplayName : "—", a.CreatedAt))
            .ToListAsync(ct);

        return Result<AdminNegotiationDetailDto>.Success(
            new AdminNegotiationDetailDto(row, offers, timeline, actions));
    }
}

/// <summary>
/// Leer el contenido de una negociación privada, con motivo justificado.
/// </summary>
/// <remarks>
/// No hay otra vía de acceder a los mensajes desde el backoffice: obtenerlos y dejar
/// constancia de que se han leído ocurren en la misma operación, para que no pueda
/// hacerse lo uno sin lo otro.
/// </remarks>
public record AccessNegotiationContentCommand(
    Guid AdminId,
    Guid NegotiationId,
    ContentAccessReason Reason,
    string? Details
) : IRequest<Result<IReadOnlyList<AdminMessageDto>>>;

public record AdminMessageDto(Guid Id, string Body, bool FromBuyer, DateTimeOffset CreatedAt);

public class AccessNegotiationContentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AccessNegotiationContentCommand, Result<IReadOnlyList<AdminMessageDto>>>
{
    public async Task<Result<IReadOnlyList<AdminMessageDto>>> Handle(
        AccessNegotiationContentCommand request, CancellationToken ct)
    {
        var negotiation = await db.Negotiations
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == request.NegotiationId, ct);

        if (negotiation is null)
            return Result<IReadOnlyList<AdminMessageDto>>.Failure("Negotiation.NotFound");

        var details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim();

        // El motivo enumerado dice en qué supuesto encaja; el texto, por qué esta
        // conversación concreta. Sin lo segundo, el registro no sirve de nada.
        if (details is null)
            return Result<IReadOnlyList<AdminMessageDto>>.Failure("Admin.ReasonRequired");

        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.Negotiation,
            TargetId   = negotiation.Id,
            Type       = AdminActionType.NegotiationContentAccessed,
            Reason     = $"{request.Reason} — {details}"
        });

        await db.SaveChangesAsync(ct);

        var messages = await db.Messages
            .AsNoTracking()
            .Where(m => m.NegotiationId == negotiation.Id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new AdminMessageDto(
                m.Id, m.Body, m.SenderId == negotiation.BuyerId, m.CreatedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<AdminMessageDto>>.Success(messages);
    }
}
