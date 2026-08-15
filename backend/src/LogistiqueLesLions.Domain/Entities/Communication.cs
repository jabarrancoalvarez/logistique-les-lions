using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>Naturaleza de la comunicación. Determina cómo se presenta al usuario.</summary>
public enum CommunicationType
{
    /// <summary>Avis général.</summary>
    AvisGeneral = 1,
    /// <summary>Maintenance programmée.</summary>
    Maintenance = 2,
    /// <summary>Information importante.</summary>
    InformationImportante = 3,
    /// <summary>Comunicación individual de soporte.</summary>
    Support = 4
}

/// <summary>A quién va dirigida.</summary>
public enum CommunicationAudience
{
    /// <summary>Todos los usuarios.</summary>
    Tous = 1,
    Particuliers = 2,
    Professionnels = 3,
    /// <summary>Una persona concreta.</summary>
    Individuel = 4
}

/// <summary>
/// Comunicación de plataforma enviada por el administrador.
/// </summary>
/// <remarks>
/// El MVP se queda deliberadamente corto: avisos, mantenimiento, información importante
/// y soporte individual. No es una herramienta de marketing.
///
/// La fila <b>es</b> el histórico: registra qué se envió, cuándo, por quién y a cuántos.
/// </remarks>
public class Communication : AuditableEntity
{
    public Guid AdminId { get; set; }

    public CommunicationType Type { get; set; } = CommunicationType.AvisGeneral;
    public CommunicationAudience Audience { get; set; } = CommunicationAudience.Tous;

    /// <summary>Destinatario, cuando la comunicación es individual.</summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>Región a la que se acota, si procede.</summary>
    public string? Region { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>Además de la notificación interna, se ha enviado correo.</summary>
    public bool SentByEmail { get; set; }

    /// <summary>Cuántas personas la recibieron.</summary>
    public int RecipientCount { get; set; }

    /// <summary>Cuántos correos se enviaron de verdad (solo quienes tienen correo).</summary>
    public int EmailsSent { get; set; }

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public UserProfile? Admin { get; set; }
    public UserProfile? TargetUser { get; set; }
}
