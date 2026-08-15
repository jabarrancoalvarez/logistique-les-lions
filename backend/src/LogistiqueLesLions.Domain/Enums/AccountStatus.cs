namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Estado administrativo de la cuenta. Lo gestiona el administrador desde
/// Administration → Utilisateurs.
/// </summary>
public enum AccountStatus
{
    /// <summary>Cuenta operativa.</summary>
    Active = 1,
    /// <summary>Suspendida temporalmente: no puede iniciar sesión.</summary>
    Suspended = 2,
    /// <summary>Bloqueada de forma permanente.</summary>
    Blocked = 3
}
