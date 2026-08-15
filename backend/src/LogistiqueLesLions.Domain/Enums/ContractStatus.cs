namespace LogistiqueLesLions.Domain.Enums;

/// <summary>
/// Estados del contrato de compraventa.
/// </summary>
/// <remarks>
/// La especificación enumera <c>Brouillon → Envoyé → À valider → Modification demandée →
/// Validé → Annulé</c>. Aquí «Envoyé» y «À valider» se unifican en
/// <see cref="AValider"/>: son el mismo estado visto desde cada lado —enviado para quien
/// lo creó, pendiente de validar para la otra parte— y mantenerlos separados obligaría a
/// una transición sin contenido que ninguna acción del usuario provoca.
/// </remarks>
public enum ContractStatus
{
    /// <summary>Creado pero todavía no enviado a la otra parte.</summary>
    Brouillon = 1,
    /// <summary>Enviado; la otra parte debe validarlo o pedir cambios.</summary>
    AValider = 2,
    /// <summary>La otra parte ha solicitado correcciones.</summary>
    ModificationDemandee = 3,
    /// <summary>Validado por ambas partes. Ya no puede modificarse.</summary>
    Valide = 4,
    Annule = 5
}
