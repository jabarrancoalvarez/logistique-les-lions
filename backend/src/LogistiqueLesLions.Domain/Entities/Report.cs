using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// «Signalement» — un usuario avisa de algo que no debería estar ocurriendo.
/// </summary>
/// <remarks>
/// La moderación es un módulo propio y no una casilla dentro del anuncio: un mismo
/// reporte puede señalar un anuncio, a una persona o una conversación, y todos acaban en
/// la misma bandeja.
/// </remarks>
public class Report : AuditableEntity
{
    /// <summary>Referencia pública: «Signalement #SG00042».</summary>
    public string PublicReference { get; set; } = string.Empty;

    /// <summary>Quién reporta.</summary>
    public Guid ReporterId { get; set; }

    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    /// <summary>
    /// Usuario señalado, cuando se puede determinar.
    /// </summary>
    /// <remarks>
    /// Se guarda resuelto y no se deduce al leer: si mañana el anuncio cambia de manos o
    /// desaparece, el reporte debe seguir diciendo a quién se estaba señalando.
    /// </remarks>
    public Guid? ReportedUserId { get; set; }

    public ReportReason Reason { get; set; } = ReportReason.Autre;
    public string? Description { get; set; }

    /// <summary>Enlaces a pruebas aportadas, separados por salto de línea.</summary>
    public string? Evidence { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Nouveau;

    /// <summary>Qué se decidió y por qué.</summary>
    public string? Resolution { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Administrador que lo cerró.</summary>
    public Guid? HandledByAdminId { get; set; }

    public UserProfile? Reporter { get; set; }
    public UserProfile? ReportedUser { get; set; }
    public UserProfile? HandledByAdmin { get; set; }

    /// <summary>Sigue abierto: nadie lo ha cerrado ni rechazado.</summary>
    public bool IsOpen => Status is ReportStatus.Nouveau or ReportStatus.EnExamen;
}
