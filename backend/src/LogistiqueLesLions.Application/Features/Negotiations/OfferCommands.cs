using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Negotiations;

/// <summary>
/// «Faire une offre» desde la ficha del vehículo.
/// </summary>
/// <param name="Amount">
/// Importe ofrecido. Es opcional en la especificación: sin importe, el formulario
/// simplemente inicia la conversación y no se registra ninguna oferta.
/// </param>
public record MakeOfferCommand(
    Guid UserId,
    Guid VehicleId,
    decimal? Amount,
    string? Message
) : IRequest<Result<MakeOfferResultDto>>;

/// <param name="OfferId"><c>null</c> si solo se inició la conversación, sin importe.</param>
public record MakeOfferResultDto(Guid NegotiationId, Guid? OfferId);

/// <summary>Contraoferta: una oferta nueva dentro de una negociación ya abierta.</summary>
public record CounterOfferCommand(
    Guid UserId,
    Guid NegotiationId,
    decimal Amount,
    string? Message
) : IRequest<Result<Guid>>;

public record AcceptOfferCommand(Guid UserId, Guid OfferId) : IRequest<Result>;

public record RejectOfferCommand(Guid UserId, Guid OfferId) : IRequest<Result>;
