using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Application.Services;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Admin.Contracts;

/// <summary>
/// «Gestion des contrats et ventes».
/// </summary>
/// <remarks>
/// Permite controlar el flujo contractual <b>sin alterar arbitrariamente</b> contratos
/// entre usuarios. La regla que manda: el administrador <b>no puede validar</b> un
/// contrato en nombre de nadie — la validación pertenece a las partes.
/// </remarks>
public record GetAdminContractsQuery(
    string? Search = null,
    ContractStatus? Status = null,
    Guid? UserId = null,
    bool? VerifiedSalesOnly = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<AdminContractListDto>>;

public record AdminContractListDto(
    int TotalCount, int Page, int PageSize, IReadOnlyList<AdminContractRowDto> Items);

public record AdminContractRowDto(
    Guid Id,
    string PublicReference,
    Guid NegotiationId,
    Guid VehicleId,
    string VehicleReference,
    string VehicleLabel,
    Guid SellerId,
    string SellerName,
    Guid BuyerId,
    string BuyerName,
    decimal AgreedPrice,
    ContractStatus Status,
    DateTimeOffset SaleDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? ValidatedAt,
    DateTimeOffset? CancelledAt,
    /// <summary>Existe venta verificada asociada.</summary>
    bool IsVerifiedSale
);

public class GetAdminContractsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminContractsQuery, Result<AdminContractListDto>>
{
    public async Task<Result<AdminContractListDto>> Handle(
        GetAdminContractsQuery request, CancellationToken ct)
    {
        var query = db.Contracts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.PublicReference.ToLower().Contains(term)
                || c.VehicleReference.ToLower().Contains(term)
                || c.SellerLegalName.ToLower().Contains(term)
                || c.BuyerLegalName.ToLower().Contains(term));
        }

        if (request.Status is { } status) query = query.Where(c => c.Status == status);

        if (request.UserId is { } userId)
            query = query.Where(c => c.SellerId == userId || c.BuyerId == userId);

        if (request.VerifiedSalesOnly == true)
            query = query.Where(c => c.Status == ContractStatus.Valide);

        var total = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminContractRowDto(
                c.Id, c.PublicReference, c.NegotiationId, c.VehicleId, c.VehicleReference,
                c.VehicleMake + " " + (c.VehicleModel ?? "") + " " + c.VehicleYear,
                c.SellerId, c.SellerLegalName,
                c.BuyerId, c.BuyerLegalName,
                c.AgreedPrice, c.Status, c.SaleDate,
                c.CreatedAt, c.SentAt, c.ValidatedAt, c.CancelledAt,
                c.Status == ContractStatus.Valide))
            .ToListAsync(ct);

        return Result<AdminContractListDto>.Success(
            new AdminContractListDto(total, page, pageSize, rows));
    }
}

/// <summary>Ficha administrativa del contrato.</summary>
public record GetAdminContractQuery(Guid ContractId) : IRequest<Result<AdminContractDetailDto>>;

public record AdminContractDetailDto(
    AdminContractRowDto Contract,
    string? VehicleModel,
    string? VehicleVersion,
    int VehicleYear,
    int? VehicleMileage,
    string? VehicleVin,
    string? RegistrationPlate,
    string? SellerIdDocument,
    string? SellerAddress,
    string? BuyerIdDocument,
    string? BuyerAddress,
    /// <summary>Código del QR. Permite verificar la venta desde el backoffice.</summary>
    string? VerificationCode,
    string? ChangeRequestNotes,
    IReadOnlyList<Negotiations.AdminTimelineDto> Timeline,
    IReadOnlyList<Users.AdminActionDto> Actions,
    IReadOnlyList<Users.AdminNoteDto> Notes
);

public class GetAdminContractQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminContractQuery, Result<AdminContractDetailDto>>
{
    public async Task<Result<AdminContractDetailDto>> Handle(
        GetAdminContractQuery request, CancellationToken ct)
    {
        var c = await db.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ContractId, ct);

        if (c is null) return Result<AdminContractDetailDto>.Failure("Contract.NotFound");

        var row = new AdminContractRowDto(
            c.Id, c.PublicReference, c.NegotiationId, c.VehicleId, c.VehicleReference,
            $"{c.VehicleMake} {c.VehicleModel} {c.VehicleYear}".Trim(),
            c.SellerId, c.SellerLegalName, c.BuyerId, c.BuyerLegalName,
            c.AgreedPrice, c.Status, c.SaleDate,
            c.CreatedAt, c.SentAt, c.ValidatedAt, c.CancelledAt,
            c.Status == ContractStatus.Valide);

        var timeline = await db.NegotiationEvents
            .AsNoTracking()
            .Where(e => e.NegotiationId == c.NegotiationId)
            .OrderBy(e => e.Sequence)
            .Select(e => new Negotiations.AdminTimelineDto(e.Type, e.Amount, e.CreatedAt))
            .ToListAsync(ct);

        var actions = await db.AdminActions
            .AsNoTracking()
            .Where(a => a.TargetType == AdminTargetType.Contract && a.TargetId == c.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new Users.AdminActionDto(
                a.Id, a.Type, a.Reason, a.Admin != null ? a.Admin.DisplayName : "—", a.CreatedAt))
            .ToListAsync(ct);

        var notes = await db.AdminNotes
            .AsNoTracking()
            .Where(n => n.TargetType == AdminTargetType.Contract && n.TargetId == c.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new Users.AdminNoteDto(
                n.Id, n.Body, n.Admin != null ? n.Admin.DisplayName : "—", n.CreatedAt))
            .ToListAsync(ct);

        return Result<AdminContractDetailDto>.Success(new AdminContractDetailDto(
            row, c.VehicleModel, c.VehicleVersion, c.VehicleYear, c.VehicleMileage,
            c.VehicleVin, c.RegistrationPlate,
            c.SellerIdDocument, c.SellerAddress, c.BuyerIdDocument, c.BuyerAddress,
            c.VerificationCode, c.ChangeRequestNotes,
            timeline, actions, notes));
    }
}

/// <summary>
/// Invalidar administrativamente un contrato, en situaciones excepcionales.
/// </summary>
/// <remarks>
/// ⚠️ Es lo <b>único</b> que el administrador puede hacerle a un contrato. No existe una
/// vía para validarlo: eso pertenece a las partes.
/// </remarks>
public record InvalidateContractCommand(Guid AdminId, Guid ContractId, string Reason)
    : IRequest<Result>;

public class InvalidateContractCommandHandler(
    IApplicationDbContext db,
    INotificationPusher? pusher = null)
    : IRequestHandler<InvalidateContractCommand, Result>
{
    public async Task<Result> Handle(InvalidateContractCommand request, CancellationToken ct)
    {
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason)) return Result.Failure("Admin.ReasonRequired");

        var contract = await db.Contracts.FirstOrDefaultAsync(c => c.Id == request.ContractId, ct);
        if (contract is null) return Result.Failure("Contract.NotFound");

        if (contract.Status == ContractStatus.Annule)
            return Result.Failure("Contract.AlreadyCancelled");

        var wasVerifiedSale = contract.Status == ContractStatus.Valide;

        contract.Status = ContractStatus.Annule;
        contract.CancelledAt = DateTimeOffset.UtcNow;

        // Si había venta verificada, deja de haberla: la reputación no puede sostenerse
        // sobre un contrato que se ha invalidado.
        if (wasVerifiedSale)
        {
            var seller = await db.UserProfiles.FirstOrDefaultAsync(u => u.Id == contract.SellerId, ct);
            if (seller is not null)
            {
                if (seller.VerifiedSalesCount > 0) seller.VerifiedSalesCount--;

                // Los puntos se retiran con un movimiento propio en negativo: el libro
                // cuenta lo que pasó, incluido lo que se deshizo.
                var awarded = await db.LoyaltyPointEntries
                    .Where(e => e.ContractId == contract.Id
                             && e.Origin == LoyaltyPointOrigin.VenteVerifiee)
                    .SumAsync(e => e.Points, ct);

                LoyaltyPointsService.Add(db, seller, -awarded,
                    LoyaltyPointOrigin.VenteInvalidee,
                    contract.Id, contract.PublicReference,
                    request.AdminId, reason);
            }
        }

        db.AdminActions.Add(new AdminAction
        {
            AdminId    = request.AdminId,
            TargetType = AdminTargetType.Contract,
            TargetId   = contract.Id,
            Type       = AdminActionType.ContractInvalidated,
            Reason     = reason
        });

        // Las dos partes se enteran: es su contrato.
        var notifications = new[] { contract.SellerId, contract.BuyerId }
            .Select(userId => new UserNotification
            {
                UserId   = userId,
                Category = NotificationCategories.Admin,
                Title    = "Contrat invalidé",
                Body     = $"Le contrat #{contract.PublicReference} a été invalidé : {reason}",
                Link     = $"/mis-negociaciones/{contract.NegotiationId}"
            })
            .ToList();

        foreach (var notification in notifications) db.UserNotifications.Add(notification);

        await db.SaveChangesAsync(ct);
        await pusher.PushAsync(notifications, ct);

        return Result.Success();
    }
}
