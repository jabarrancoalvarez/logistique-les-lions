using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Una sesión abierta de una cuenta. Hay tantas filas como dispositivos conectados.
/// </summary>
/// <remarks>
/// Antes el refresh token vivía en una única columna de <c>UserProfile</c>, así que una
/// cuenta solo podía tener una sesión: entrar desde el móvil sobrescribía la columna y
/// expulsaba al ordenador sin explicación. En un marketplace la gente mira anuncios en el
/// móvil y publica desde el ordenador, y eso hacía la aplicación inservible a dos manos.
///
/// ⚠️ Este es el material con el que se autentica una persona. Tres reglas que no se
/// pueden relajar, y que vienen de un fallo real:
/// <list type="number">
///   <item>Nunca se busca por un token vacío o nulo: casaría con filas ajenas.</item>
///   <item>La caducidad tiene que <b>existir y estar en el futuro</b>. Preguntar si «no ha
///   pasado» deja entrar a quien la tenga a nulo, porque en C# comparar con un nulo
///   levantado siempre da falso.</item>
///   <item>Al usarse, el token se <b>rota</b>: se revoca el anterior y se emite otro. Un
///   token usado dos veces no vale.</item>
/// </list>
/// </remarks>
public class UserRefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }

    /// <summary>Valor opaco. No es un JWT: no lleva información dentro.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Cuándo dejó de valer: al rotarse, al cerrar sesión o al revocarse.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Para reconocer el dispositivo en un futuro listado de sesiones.</summary>
    public string? UserAgent { get; set; }

    public UserProfile User { get; set; } = null!;

    /// <summary>Vale ahora mismo: ni revocada ni caducada.</summary>
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
