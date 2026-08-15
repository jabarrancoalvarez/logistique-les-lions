using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.VehicleRequests;

/// <summary>Crear una solicitud «Trouvez-moi une voiture».</summary>
public record CreateVehicleRequestCommand(
    Guid UserId,

    Guid? MakeId,
    string MakeName,
    string? ModelName,
    string? Version,

    int? YearFrom,
    int? YearTo,
    int? MaxMileage,

    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    string? Color,
    string? ImportantEquipment,

    decimal? MaxBudget,
    VehicleRequestOrigin Origin,
    string? Notes
) : IRequest<Result<VehicleRequestCreatedDto>>;

/// <param name="PublicReference">Referencia asignada: "YD00248".</param>
public record VehicleRequestCreatedDto(Guid Id, string PublicReference);

/// <summary>Mensaje del usuario dentro de su solicitud.</summary>
public record AddVehicleRequestMessageCommand(Guid UserId, Guid RequestId, string Body)
    : IRequest<Result>;

/// <summary>
/// «Annuler ma demande». La solicitud permanece en el histórico como Annulée:
/// nunca se borra físicamente.
/// </summary>
public record CancelVehicleRequestCommand(Guid UserId, Guid RequestId) : IRequest<Result>;
