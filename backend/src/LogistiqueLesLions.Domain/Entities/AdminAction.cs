using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Registro de una acción administrativa.
/// </summary>
/// <remarks>
/// Append-only, como la cronología de una negociación: nunca se modifica ni se borra.
/// Es lo que permite responder «quién suspendió esta cuenta, cuándo y por qué», que es
/// justo lo que la especificación exige antes de dejar que un administrador toque nada.
///
/// No sustituye al <see cref="AuditLog"/> técnico: aquel registra cambios de columnas;
/// este registra <b>decisiones</b>, con su motivo escrito por una persona.
/// </remarks>
public class AdminAction : AuditableEntity
{
    /// <summary>Administrador que la ejecutó.</summary>
    public Guid AdminId { get; set; }

    public AdminTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    public AdminActionType Type { get; set; }

    /// <summary>Motivo escrito por el administrador.</summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Valor anterior y nuevo, cuando la acción cambia algo con valor.
    /// </summary>
    /// <remarks>
    /// El documento los pide «cuando proceda»: suspender una cuenta no tiene valor
    /// anterior, pero cambiar el mínimo de comparables de 5 a 8 sí, y sin ellos la
    /// pregunta «¿qué había antes?» no tiene respuesta. Se guardan como texto porque
    /// cada acción cambia cosas de tipos distintos.
    /// </remarks>
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public UserProfile? Admin { get; set; }
}

/// <summary>
/// Nota interna sobre un usuario o un anuncio.
/// </summary>
/// <remarks>
/// A diferencia de <see cref="AdminAction"/>, una nota se puede corregir y retirar: es
/// contexto de trabajo del equipo, no el registro de una decisión.
/// ❌ Nunca es visible para el usuario afectado.
/// </remarks>
public class AdminNote : AuditableEntity
{
    public Guid AdminId { get; set; }

    public AdminTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    public string Body { get; set; } = string.Empty;

    public UserProfile? Admin { get; set; }
}
