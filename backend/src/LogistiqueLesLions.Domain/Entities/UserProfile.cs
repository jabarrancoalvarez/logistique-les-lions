using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Perfil de usuario de la plataforma. Almacena credenciales y datos personales.</summary>
public class UserProfile : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    /// <summary>Hash bcrypt de la contraseña</summary>
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Buyer;
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Token opaco de refresh (se regenera en cada uso)</summary>
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    /// <summary>Para dealers: nombre de la empresa</summary>
    public string? CompanyName { get; set; }
    public string? CompanyVat { get; set; }
    public string? Bio { get; set; }
}
