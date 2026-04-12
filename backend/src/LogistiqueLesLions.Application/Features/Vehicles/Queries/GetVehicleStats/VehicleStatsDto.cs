namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleStats;

public record VehicleStatsDto(
    int ActiveVehicles,
    int SupportedCountries,
    int CompletedTransactions,
    int RegisteredDealers,
    int TotalMakes
);
