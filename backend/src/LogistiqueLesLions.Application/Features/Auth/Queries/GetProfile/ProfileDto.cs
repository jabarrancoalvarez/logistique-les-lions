namespace LogistiqueLesLions.Application.Features.Auth.Queries.GetProfile;

public record ProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string? Phone,
    string? AvatarUrl,
    string? CountryCode,
    string? City,
    string? CompanyName,
    string? CompanyVat,
    string? Bio,
    bool IsVerified,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt
);
