namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleMakes;

public record VehicleMakeDto(
    Guid Id,
    string Name,
    string? Country,
    string? LogoUrl,
    bool IsPopular,
    int ModelsCount
);
