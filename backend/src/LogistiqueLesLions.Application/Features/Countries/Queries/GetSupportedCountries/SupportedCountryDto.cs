namespace LogistiqueLesLions.Application.Features.Countries.Queries.GetSupportedCountries;

public record SupportedCountryDto(
    string Code,
    string NameEs,
    string NameEn,
    string? FlagEmoji,
    string Currency,
    bool IsEuMember,
    bool SupportsImport,
    bool SupportsExport,
    int DisplayOrder
);
