using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Application.Features.Compliance.Queries.GetProcessStatus;

public record ProcessStatusDto(
    Guid Id,
    Guid VehicleId,
    string VehicleTitle,
    string OriginCountry,
    string DestinationCountry,
    ProcessType ProcessType,
    ProcessStatus Status,
    int CompletionPercent,
    decimal? EstimatedCostEur,
    decimal? ActualCostEur,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ProcessDocumentDto> Documents
);

public record ProcessDocumentDto(
    Guid Id,
    string DocumentType,
    DocumentStatus Status,
    string ResponsibleParty,
    DateTimeOffset? RequiredByDate,
    string? FileUrl,
    string? TemplateUrl,
    string? OfficialUrl,
    string? InstructionsEs,
    decimal? EstimatedCostEur
);
