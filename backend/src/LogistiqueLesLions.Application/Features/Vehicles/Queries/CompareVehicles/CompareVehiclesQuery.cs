using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.CompareVehicles;

/// <summary>
/// Datos de los vehículos seleccionados en el comparador.
/// </summary>
/// <remarks>
/// El comparador almacena únicamente identificadores: cada vez que se abre se consultan
/// los datos actuales, de modo que un cambio de precio o de estado se refleje al
/// instante. Nunca debe copiar los datos del anuncio.
/// </remarks>
public record CompareVehiclesQuery(IReadOnlyList<Guid> VehicleIds)
    : IRequest<Result<List<VehicleComparisonDto>>>;

/// <summary>
/// Ficha reducida para la comparación. Solo lleva lo que la especificación pide mostrar
/// en la tabla; deliberadamente no incluye descripción ni galería completa.
/// </summary>
public record VehicleComparisonDto(
    Guid Id,
    string PublicReference,
    string Slug,
    string MakeName,
    string? ModelName,
    string? Version,
    string? PrimaryImageUrl,

    // ─── Cabecera ──────────────────────────────────────────────────────────
    decimal Price,
    PriceIndicator? PriceIndicator,
    string? City,
    VehicleStatus Status,

    // ─── Características principales ───────────────────────────────────────
    int Year,
    int? Mileage,
    FuelType? FuelType,
    TransmissionType? Transmission,
    BodyType? BodyType,
    int? PowerCv,
    int? EngineDisplacementCc,
    Drivetrain? Drivetrain,
    int? Doors,
    int? Seats,
    string? Color,

    // ─── Situación administrativa ──────────────────────────────────────────
    CustomsStatus? CustomsStatus,

    // ─── Equipamiento declarado ────────────────────────────────────────────
    IReadOnlyList<string> EquipmentCodes,

    Guid SellerId
);
