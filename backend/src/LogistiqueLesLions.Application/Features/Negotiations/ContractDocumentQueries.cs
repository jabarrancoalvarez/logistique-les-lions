using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Negotiations;

/// <summary>
/// Datos con los que se compone el PDF del contrato.
/// </summary>
/// <remarks>
/// Solo se sirve cuando el contrato está validado: el documento descargable es el
/// contrato definitivo, no un borrador en curso de negociación.
/// </remarks>
public record GetContractDocumentQuery(Guid UserId, Guid ContractId)
    : IRequest<Result<ContractDocumentDto>>;

public record ContractDocumentDto(
    string PublicReference,
    string VerificationCode,

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
    string? SellerPhone,
    string BuyerLegalName,
    string? BuyerIdDocument,
    string? BuyerAddress,
    string? BuyerPhone,

    DateTimeOffset ValidatedAt
);

public class GetContractDocumentQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetContractDocumentQuery, Result<ContractDocumentDto>>
{
    public async Task<Result<ContractDocumentDto>> Handle(
        GetContractDocumentQuery request, CancellationToken ct)
    {
        var contract = await db.Contracts
            .AsNoTracking()
            .Include(c => c.Negotiation)
            .FirstOrDefaultAsync(c => c.Id == request.ContractId, ct);

        if (contract is null) return Result<ContractDocumentDto>.Failure("Contract.NotFound");

        // El contrato es de las dos partes y de nadie más.
        if (!contract.Negotiation.Involves(request.UserId))
            return Result<ContractDocumentDto>.Failure("Negotiation.AccessDenied");

        if (contract.Status != ContractStatus.Valide || contract.VerificationCode is null)
            return Result<ContractDocumentDto>.Failure("Contract.NotValidated");

        // El teléfono es el identificador de la cuenta y va en el contrato como dato de
        // contacto de las partes.
        var phones = await db.UserProfiles
            .AsNoTracking()
            .Where(u => u.Id == contract.SellerId || u.Id == contract.BuyerId)
            .Select(u => new { u.Id, u.Phone })
            .ToListAsync(ct);

        var dto = new ContractDocumentDto(
            contract.PublicReference,
            contract.VerificationCode,
            contract.VehicleMake, contract.VehicleModel, contract.VehicleVersion,
            contract.VehicleYear, contract.VehicleMileage, contract.VehicleVin,
            contract.RegistrationPlate, contract.VehicleReference,
            contract.AgreedPrice, contract.SaleDate,
            contract.SellerLegalName, contract.SellerIdDocument, contract.SellerAddress,
            phones.FirstOrDefault(p => p.Id == contract.SellerId)?.Phone,
            contract.BuyerLegalName, contract.BuyerIdDocument, contract.BuyerAddress,
            phones.FirstOrDefault(p => p.Id == contract.BuyerId)?.Phone,
            contract.ValidatedAt!.Value);

        return Result<ContractDocumentDto>.Success(dto);
    }
}

/// <summary>
/// Comprobación pública de una venta a partir del código del QR.
/// </summary>
/// <remarks>
/// No requiere cuenta: quien escanea el QR tiene el contrato en papel delante y solo
/// quiere saber si la venta existe. Por eso devuelve lo que ya figura en ese papel y
/// <b>nada más</b>: ni documentos de identidad, ni direcciones, ni teléfonos.
/// </remarks>
public record VerifyContractQuery(string Code) : IRequest<Result<ContractVerificationDto>>;

public record ContractVerificationDto(
    string PublicReference,
    string VehicleMake,
    string? VehicleModel,
    string? VehicleVersion,
    int VehicleYear,
    string? VehicleVin,
    string? RegistrationPlate,
    string VehicleReference,
    decimal AgreedPrice,
    DateTimeOffset SaleDate,
    string SellerLegalName,
    string BuyerLegalName,
    DateTimeOffset ValidatedAt
);

public class VerifyContractQueryHandler(IApplicationDbContext db)
    : IRequestHandler<VerifyContractQuery, Result<ContractVerificationDto>>
{
    public async Task<Result<ContractVerificationDto>> Handle(
        VerifyContractQuery request, CancellationToken ct)
    {
        var code = request.Code?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
            return Result<ContractVerificationDto>.Failure("Contract.NotFound");

        var contract = await db.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.VerificationCode == code && c.Status == ContractStatus.Valide, ct);

        // Un código que no corresponde a una venta validada no revela por qué: da igual
        // que no exista, que esté anulada o que aún no se haya validado.
        if (contract is null) return Result<ContractVerificationDto>.Failure("Contract.NotFound");

        var dto = new ContractVerificationDto(
            contract.PublicReference,
            contract.VehicleMake, contract.VehicleModel, contract.VehicleVersion,
            contract.VehicleYear, contract.VehicleVin, contract.RegistrationPlate,
            contract.VehicleReference,
            contract.AgreedPrice, contract.SaleDate,
            contract.SellerLegalName, contract.BuyerLegalName,
            contract.ValidatedAt!.Value);

        return Result<ContractVerificationDto>.Success(dto);
    }
}
