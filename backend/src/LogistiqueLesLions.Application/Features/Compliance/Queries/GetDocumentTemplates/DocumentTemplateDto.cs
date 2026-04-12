namespace LogistiqueLesLions.Application.Features.Compliance.Queries.GetDocumentTemplates;

public record DocumentTemplateDto(
    Guid Id,
    string Country,
    string DocumentType,
    string? TemplateUrl,
    string? InstructionsEs,
    string? InstructionsEn,
    string? OfficialUrl,
    string? IssuingAuthority,
    decimal? EstimatedCostEur,
    int? EstimatedDays
);
