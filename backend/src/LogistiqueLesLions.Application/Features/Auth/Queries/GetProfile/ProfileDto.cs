namespace LogistiqueLesLions.Application.Features.Auth.Queries.GetProfile;

/// <summary>
/// Datos de "Mon profil". Reúne la identidad del usuario y los contadores públicos
/// que la especificación funcional muestra en el bloque "Vendu par".
/// </summary>
public record ProfileDto(
    Guid Id,
    string DisplayName,
    string? Phone,
    bool PhoneVerified,
    string? Email,
    string Role,
    string AccountType,
    string? AvatarUrl,
    string? Region,
    string? City,
    string? Bio,
    bool AllowWhatsAppContact,
    int VerifiedSalesCount,
    int ActiveListingsCount,
    DateTimeOffset? LastLoginAt,
    /// <summary>Fecha de alta — "Membre depuis 2026".</summary>
    DateTimeOffset CreatedAt
);
