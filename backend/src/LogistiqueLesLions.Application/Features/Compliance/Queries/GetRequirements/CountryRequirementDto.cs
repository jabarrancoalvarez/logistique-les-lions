namespace LogistiqueLesLions.Application.Features.Compliance.Queries.GetRequirements;

public record CountryRequirementDto(
    string OriginCountry,
    string DestinationCountry,
    IReadOnlyList<string> RequiredDocuments,
    bool HomologationRequired,
    decimal CustomsRatePercent,
    decimal VatRatePercent,
    bool TechnicalInspectionRequired,
    decimal EstimatedProcessingCostEur,
    int EstimatedDays,
    string? NotesEs,
    string? NotesEn,
    DateTimeOffset LastUpdatedAt
);
