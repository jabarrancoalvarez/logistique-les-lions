namespace LogistiqueLesLions.Application.Features.Compliance.Queries.EstimateCost;

public record CostEstimateDto(
    Guid VehicleId,
    string OriginCountry,
    string DestinationCountry,
    decimal VehiclePriceEur,
    decimal CustomsDutyEur,
    decimal VatEur,
    decimal ProcessingFeesEur,
    decimal HomologationCostEur,
    decimal TotalEstimatedEur,
    decimal CustomsRatePercent,
    decimal VatRatePercent,
    bool HomologationRequired,
    int EstimatedDays,
    IReadOnlyList<CostLineItemDto> LineItems
);

public record CostLineItemDto(
    string Label,
    decimal AmountEur,
    string? Description
);
