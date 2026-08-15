using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Negotiations;

/// <summary>
/// «Créer le contrat de vente» desde la negociación.
/// </summary>
/// <remarks>
/// Gran parte de los datos se precargan del anuncio y de los perfiles; aquí solo llega
/// lo que hay que completar a mano, como la matrícula y los documentos de identidad.
/// </remarks>
public record CreateContractCommand(
    Guid UserId,
    Guid NegotiationId,
    decimal AgreedPrice,
    string? RegistrationPlate,
    string SellerLegalName,
    string? SellerIdDocument,
    string? SellerAddress,
    string BuyerLegalName,
    string? BuyerIdDocument,
    string? BuyerAddress
) : IRequest<Result<Guid>>;

/// <summary>Corrige un contrato en borrador o con modificación solicitada.</summary>
public record UpdateContractCommand(
    Guid UserId,
    Guid ContractId,
    decimal AgreedPrice,
    string? RegistrationPlate,
    string SellerLegalName,
    string? SellerIdDocument,
    string? SellerAddress,
    string BuyerLegalName,
    string? BuyerIdDocument,
    string? BuyerAddress
) : IRequest<Result>;

/// <summary>Envía el contrato a la otra parte para que lo valide.</summary>
public record SendContractCommand(Guid UserId, Guid ContractId) : IRequest<Result>;

/// <summary>
/// «Valider». Cierra la operación: marca la venta como verificada.
/// </summary>
public record ValidateContractCommand(Guid UserId, Guid ContractId) : IRequest<Result>;

/// <summary>
/// «Demander une modification».
/// </summary>
/// <remarks>
/// La especificación descarta un «Rejeter» seco: ante un error en el contrato es más
/// natural pedir la corrección que tumbar la operación.
/// </remarks>
public record RequestContractChangesCommand(Guid UserId, Guid ContractId, string Notes)
    : IRequest<Result>;

public record CancelContractCommand(Guid UserId, Guid ContractId) : IRequest<Result>;
