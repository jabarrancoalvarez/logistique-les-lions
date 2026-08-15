using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Una funcionalidad anunciada en «Prochainement».
/// </summary>
/// <remarks>
/// El propósito no es enseñar una lista bonita, sino medir: el documento quiere saber
/// «qué servicio premium merece realmente desarrollarse» antes de escribir una línea de
/// ese servicio. Por eso el catálogo se administra desde Configuration y el interés se
/// cuenta por persona.
/// </remarks>
public class UpcomingFeature : AuditableEntity
{
    /// <summary>Identificador estable e independiente del idioma: CRM, STOCK…</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Etiqueta en francés.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Retirada del catálogo sin perder los intereses ya declarados.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<FeatureInterest> Interests { get; set; } = [];
}

/// <summary>
/// «Ça m'intéresse»: una persona ha declarado interés por una funcionalidad futura.
/// </summary>
/// <remarks>
/// Una fila por persona y funcionalidad —índice único—: pulsar dos veces no vale por
/// dos. Retirar el interés borra la fila, porque aquí lo que importa es la foto actual
/// de la demanda, no su historia.
/// </remarks>
public class FeatureInterest : AuditableEntity
{
    public Guid FeatureId { get; set; }
    public Guid UserId { get; set; }

    public UpcomingFeature Feature { get; set; } = null!;
    public UserProfile? User { get; set; }
}
