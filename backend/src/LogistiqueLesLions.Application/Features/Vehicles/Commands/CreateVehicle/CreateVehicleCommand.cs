using System.Text.Json;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.CreateVehicle;

public record CreateVehicleCommand(
    string Title,
    string? DescriptionEs,
    string? DescriptionEn,
    Guid MakeId,
    Guid? ModelId,
    int Year,
    int? Mileage,
    VehicleCondition Condition,
    BodyType? BodyType,
    FuelType? FuelType,
    TransmissionType? Transmission,
    string? Color,
    string? Vin,
    decimal Price,
    string Currency,
    bool PriceNegotiable,
    string CountryOrigin,
    string? City,
    string? PostalCode,
    bool IsExportReady,
    Guid SellerId,
    Guid? DealerId,
    JsonDocument? Specs,
    JsonDocument? Features
) : IRequest<Result<Guid>>;
