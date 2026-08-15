using LogistiqueLesLions.Domain.Entities;

namespace LogistiqueLesLions.Application.Features.Auth;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserDto User
);

/// <summary>Identidad mínima del usuario que el frontend guarda en sesión.</summary>
public record UserDto(
    Guid Id,
    string DisplayName,
    string? Phone,
    string? Email,
    string Role,
    string AccountType,
    string? AvatarUrl,
    bool PhoneVerified
)
{
    public static UserDto From(UserProfile user) => new(
        user.Id,
        user.DisplayName,
        user.Phone,
        user.Email,
        user.Role.ToString(),
        user.AccountType.ToString(),
        user.AvatarUrl,
        user.PhoneVerified);
}
