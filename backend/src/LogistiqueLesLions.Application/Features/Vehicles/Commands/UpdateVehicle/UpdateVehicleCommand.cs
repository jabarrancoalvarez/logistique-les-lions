using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.UpdateVehicle;

public record UpdateVehicleCommand(
    Guid Id,
    string Title,
    string? Description,

    Guid MakeId,
    Guid? ModelId,
    string? Version,
    int Year,
    int? Mileage,
    VehicleCondition Condition,
    BodyType? BodyType,
    FuelType? FuelType,
    TransmissionType? Transmission,
    string? Color,
    int? Doors,
    int? Seats,
    string? Vin,

    int? PowerCv,
    int? EngineDisplacementCc,
    Drivetrain? Drivetrain,
    string? EngineName,


    decimal Price,
    bool PriceNegotiable,

    string? Region,
    string? City,
    string? District,

    IReadOnlyList<Guid> EquipmentIds,

    /// <summary>
    /// Usuario que solicita la edición. Lo fija el endpoint a partir del JWT: nunca se
    /// acepta del cuerpo de la petición.
    /// </summary>
    Guid RequesterId = default
) : IRequest<Result>;
