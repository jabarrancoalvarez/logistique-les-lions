using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Negotiations;

/// <summary>
/// Pestaña «Contrat» de una negociación.
/// </summary>
/// <remarks>
/// Devuelve el contrato si existe y, si no, los datos precargados con los que se abre el
/// formulario. Así la pestaña se pinta con una sola llamada tanto antes como después de
/// crear el contrato.
/// </remarks>
public record GetContractQuery(Guid UserId, Guid NegotiationId)
    : IRequest<Result<ContractTabDto>>;

public record ContractTabDto(
    ContractDto? Contract,
    ContractPrefillDto Prefill,
    /// <summary>No hay contrato vivo y la operación sigue abierta.</summary>
    bool CanCreate,
    bool IsSeller
);

public record ContractDto(
    Guid Id,
    string PublicReference,
    ContractStatus Status,
    Guid NegotiationId,

    string VehicleMake,
    string? VehicleModel,
    string? VehicleVersion,
    int VehicleYear,
    int? VehicleMileage,
    string? VehicleVin,
    string? RegistrationPlate,
    string VehicleReference,

    decimal AgreedPrice,
    DateTimeOffset SaleDate,

    string SellerLegalName,
    string? SellerIdDocument,
    string? SellerAddress,
    string BuyerLegalName,
    string? BuyerIdDocument,
    string? BuyerAddress,

    /// <summary>Lo redactó el usuario que consulta; la otra parte es quien valida.</summary>
    bool CreatedByMe,
    bool CanEdit,
    bool CanSend,
    bool CanValidate,
    bool CanRequestChanges,
    bool CanCancel,
    /// <summary>El contrato definitivo ya se puede descargar en PDF.</summary>
    bool CanDownloadPdf,

    /// <summary>Código del QR. Solo existe una vez validado.</summary>
    string? VerificationCode,
    string? ChangeRequestNotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? ValidatedAt,
    DateTimeOffset? CancelledAt
);

/// <summary>
/// Datos que el formulario trae ya rellenos del anuncio y de los perfiles.
/// </summary>
public record ContractPrefillDto(
    string VehicleMake,
    string? VehicleModel,
    string? VehicleVersion,
    int VehicleYear,
    int? VehicleMileage,
    string? VehicleVin,
    string VehicleReference,
    /// <summary>Importe de la última oferta aceptada; si no hay, el precio del anuncio.</summary>
    decimal SuggestedPrice,
    string SellerLegalName,
    string BuyerLegalName
);

public class GetContractQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetContractQuery, Result<ContractTabDto>>
{
    public async Task<Result<ContractTabDto>> Handle(GetContractQuery request, CancellationToken ct)
    {
        var n = await db.Negotiations
            .AsNoTracking()
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Include(x => x.Vehicle).ThenInclude(v => v.Make)
            .Include(x => x.Vehicle).ThenInclude(v => v.Model)
            .Include(x => x.Offers)
            .FirstOrDefaultAsync(x => x.Id == request.NegotiationId, ct);

        if (n is null) return Result<ContractTabDto>.Failure("Negotiation.NotFound");
        if (!n.Involves(request.UserId)) return Result<ContractTabDto>.Failure("Negotiation.AccessDenied");

        var contract = await db.Contracts
            .AsNoTracking()
            .Where(c => c.NegotiationId == n.Id && c.Status != ContractStatus.Annule)
            .FirstOrDefaultAsync(ct);

        var accepted = n.Offers
            .Where(o => o.Status == OfferStatus.Acceptee)
            .OrderByDescending(o => o.RespondedAt)
            .FirstOrDefault();

        var prefill = new ContractPrefillDto(
            n.Vehicle.Make.Name,
            n.Vehicle.Model?.Name,
            n.Vehicle.Version,
            n.Vehicle.Year,
            n.Vehicle.Mileage,
            n.Vehicle.Vin,
            n.Vehicle.PublicReference,
            accepted?.Amount ?? n.Vehicle.Price,
            n.Seller.DisplayName,
            n.Buyer.DisplayName);

        var dto = new ContractTabDto(
            contract is null ? null : Map(contract, request.UserId),
            prefill,
            contract is null && n.Status != NegotiationStatus.Terminee,
            n.SellerId == request.UserId);

        return Result<ContractTabDto>.Success(dto);
    }

    private static ContractDto Map(Contract c, Guid userId)
    {
        var byMe      = c.CreatedByUserId == userId;
        var validator = c.ValidatorId == userId;

        return new ContractDto(
            c.Id, c.PublicReference, c.Status, c.NegotiationId,
            c.VehicleMake, c.VehicleModel, c.VehicleVersion, c.VehicleYear,
            c.VehicleMileage, c.VehicleVin, c.RegistrationPlate, c.VehicleReference,
            c.AgreedPrice, c.SaleDate,
            c.SellerLegalName, c.SellerIdDocument, c.SellerAddress,
            c.BuyerLegalName, c.BuyerIdDocument, c.BuyerAddress,
            byMe,
            // Editar y enviar son de quien lo redactó; validar y pedir cambios, de la otra parte.
            CanEdit:            byMe && c.IsEditable,
            CanSend:            byMe && c.IsEditable,
            CanValidate:        validator && c.AwaitsValidation,
            CanRequestChanges:  validator && c.AwaitsValidation,
            CanCancel:          c.Status != ContractStatus.Valide,
            CanDownloadPdf:     c.Status == ContractStatus.Valide,
            c.VerificationCode,
            c.ChangeRequestNotes,
            c.CreatedAt, c.SentAt, c.ValidatedAt, c.CancelledAt);
    }
}
