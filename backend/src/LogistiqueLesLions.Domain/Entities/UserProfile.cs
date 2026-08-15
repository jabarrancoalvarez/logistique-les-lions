using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Cuenta de usuario de Yoon u Auto.
/// </summary>
/// <remarks>
/// La especificación funcional fija los datos que se almacenan en el registro:
/// teléfono, nombre, tipo de usuario (Particulier/Professionnel), ciudad, fecha de
/// registro e ID (estos dos últimos automáticos, heredados de <see cref="AuditableEntity"/>).
/// </remarks>
public class UserProfile : AuditableEntity
{
    // ─── Identificación ────────────────────────────────────────────────────
    /// <summary>
    /// Identificador principal de la cuenta, en formato E.164 senegalés (+221XXXXXXXXX).
    /// Único. Nullable solo por compatibilidad con cuentas anteriores a la migración;
    /// el registro lo exige siempre.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>Teléfono confirmado. Se muestra públicamente como "✓ Téléphone vérifié".</summary>
    public bool PhoneVerified { get; set; }
    public DateTimeOffset? PhoneVerifiedAt { get; set; }

    /// <summary>
    /// Correo opcional, usado únicamente para notificaciones por e-mail.
    /// No es el identificador de la cuenta.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>Hash bcrypt de la contraseña</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Nombre o nombre comercial mostrado públicamente.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    /// <summary>Campo informativo del perfil. Ver <see cref="Enums.AccountType"/>.</summary>
    public AccountType AccountType { get; set; } = AccountType.Particulier;

    // ─── Ubicación (Senegal) ───────────────────────────────────────────────
    /// <summary>Código de región senegalesa: DK, TH, SL...</summary>
    public string? Region { get; set; }
    public string? City { get; set; }

    // ─── Rol y estado ──────────────────────────────────────────────────────
    public UserRole Role { get; set; } = UserRole.User;
    public AccountStatus Status { get; set; } = AccountStatus.Active;

    /// <summary>
    /// Hasta cuándo dura la suspensión temporal.
    /// </summary>
    /// <remarks>
    /// Distingue «suspender temporalmente» de «bloquear»: la suspensión tiene fecha de
    /// final escrita desde el principio, y así no depende de que alguien se acuerde de
    /// levantarla.
    /// </remarks>
    public DateTimeOffset? SuspendedUntil { get; set; }

    /// <summary>
    /// La cuenta puede operar: activa, o suspendida con la suspensión ya cumplida.
    /// </summary>
    public bool CanSignIn =>
        Status == AccountStatus.Active
        || (Status == AccountStatus.Suspended
            && SuspendedUntil is { } until && until <= DateTimeOffset.UtcNow);

    // ─── Reputación pública ────────────────────────────────────────────────
    /// <summary>Ventas cerradas con contrato validado dentro de Yoon u Auto.</summary>
    public int VerifiedSalesCount { get; set; }

    /// <summary>
    /// Saldo de puntos de fidelización.
    /// </summary>
    /// <remarks>
    /// Es la suma de <see cref="LoyaltyPointEntry"/>, guardada aquí para no recorrer el
    /// libro entero al pintar un listado. Se escribe siempre en la misma transacción que
    /// el movimiento: ❌ nunca se toca este campo sin añadir su fila.
    /// </remarks>
    public int LoyaltyPoints { get; set; }

    // ─── Preferencias de contacto ──────────────────────────────────────────
    /// <summary>
    /// Permite a otros usuarios contactar por WhatsApp. La mensajería interna sigue
    /// siendo el canal principal.
    /// </summary>
    public bool AllowWhatsAppContact { get; set; } = true;

    /// <summary>
    /// Interruptor general de Favoris: todos los vehículos guardados reciben alertas de
    /// bajada de precio. Al desactivarlo, el usuario elige a cuáles ponérselas mediante
    /// <see cref="SavedVehicle.PriceAlertEnabled"/>.
    /// </summary>
    public bool FavoriteAlertsAllEnabled { get; set; } = true;

    public string? Bio { get; set; }

    // ─── Sesión ────────────────────────────────────────────────────────────
    /// <summary>Token opaco de refresh (se regenera en cada uso)</summary>
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    /// <summary>Última actividad registrada. Se muestra en la ficha administrativa.</summary>
    public DateTimeOffset? LastActivityAt { get; set; }
}
