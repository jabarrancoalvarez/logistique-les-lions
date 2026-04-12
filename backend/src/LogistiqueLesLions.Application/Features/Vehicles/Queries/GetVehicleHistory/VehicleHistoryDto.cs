using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleHistory;

public record VehicleHistoryDto(
    Guid Id,
    VehicleHistoryEventType EventType,
    string Description,
    DateTimeOffset EventDate,
    int? MileageAtEvent,
    string? Source,
    bool IsVerified
);
