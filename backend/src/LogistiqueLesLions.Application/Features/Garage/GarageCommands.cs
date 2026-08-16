using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Garage;

/// <summary>
/// Datos de la ficha de un vehículo de Mon Garage.
/// </summary>
/// <remarks>
/// Casi todo es opcional a propósito: la especificación permite crear el vehículo con lo
/// mínimo y completar la ficha más tarde.
/// </remarks>
public record GarageVehicleInput(
    Guid MakeId,
    Guid? ModelId,
    string? Version,
    int Year,
    int? Mileage,
    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    int? PowerCv,
    int? EngineDisplacementCc,
    string? Color,
    string? RegistrationPlate,
    string? Vin,
    DateTimeOffset? PurchaseDate,
    decimal? PurchasePrice
);

/// <summary>«+ Ajouter un véhicule» desde Mon Garage.</summary>
/// <param name="SourceContractId">
/// Contrato de la compra cuando el vehículo llega desde una venta verificada.
/// Se acepta aquí, pero el handler comprueba que el usuario es realmente quien compró.
/// </param>
public record CreateGarageVehicleCommand(
    Guid UserId,
    GarageVehicleInput Vehicle,
    Guid? SourceContractId = null
) : IRequest<Result<Guid>>;

/// <summary>«Mettre à jour»: corregir la ficha o poner al día el kilometraje.</summary>
public record UpdateGarageVehicleCommand(
    Guid UserId,
    Guid GarageVehicleId,
    GarageVehicleInput Vehicle
) : IRequest<Result>;

/// <summary>Quitar el vehículo del garaje. Soft delete: conserva su historial.</summary>
public record DeleteGarageVehicleCommand(Guid UserId, Guid GarageVehicleId) : IRequest<Result>;

/// <summary>Alta de una fotografía ya subida al almacenamiento.</summary>
/// <summary>Alta de una fotografía ya subida al almacenamiento <b>privado</b>.</summary>
public record AddGarageVehicleImageCommand(
    Guid UserId,
    Guid GarageVehicleId,
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    bool IsPrimary,
    int SortOrder
) : IRequest<Result<Guid>>;

public record DeleteGarageVehicleImageCommand(Guid UserId, Guid ImageId) : IRequest<Result>;
