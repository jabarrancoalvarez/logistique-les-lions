using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Compliance.Commands.InitiateProcess;

/// <summary>Inicia un proceso de importación/exportación y genera el checklist de documentos.</summary>
public record InitiateProcessCommand(
    Guid VehicleId,
    Guid BuyerId,
    Guid SellerId,
    string OriginCountry,
    string DestinationCountry,
    ProcessType ProcessType
) : IRequest<Result<Guid>>;
