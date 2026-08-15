using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.CreateVehicle;

public record CreateVehicleCommand(
    string Title,
    /// <summary>Texto libre del vendedor. Nunca se genera ni se reescribe con IA.</summary>
    string? Description,

    // ─── Información general ───────────────────────────────────────────────
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

    // ─── Motor ─────────────────────────────────────────────────────────────
    int? PowerCv,
    int? EngineDisplacementCc,
    Drivetrain? Drivetrain,
    string? EngineName,

    // ─── Situación aduanera ────────────────────────────────────────────────
    CustomsStatus CustomsStatus,

    // ─── Precio ────────────────────────────────────────────────────────────
    decimal Price,
    bool PriceNegotiable,

    // ─── Ubicación ─────────────────────────────────────────────────────────
    string? Region,
    string? City,
    string? District,

    // ─── Equipamiento ──────────────────────────────────────────────────────
    IReadOnlyList<Guid> EquipmentIds,

    // ─── Publicación ───────────────────────────────────────────────────────
    /// <summary>Si es false el anuncio queda en Brouillon y no se publica.</summary>
    bool Publish,

    Guid SellerId
) : IRequest<Result<Guid>>;
