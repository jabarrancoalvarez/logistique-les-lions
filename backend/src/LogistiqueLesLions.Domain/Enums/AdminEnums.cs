namespace LogistiqueLesLions.Domain.Enums;

/// <summary>Sobre qué recae una acción o una nota administrativa.</summary>
public enum AdminTargetType
{
    User = 1,
    Listing = 2,
    /// <summary>Solicitud «Trouvez-moi cette voiture».</summary>
    Request = 3,
    Negotiation = 4,
    Contract = 5,
    Report = 6,
    /// <summary>Parámetros, interruptores y catálogos de la plataforma.</summary>
    Settings = 7
}

/// <summary>
/// Situaciones que justifican leer una conversación privada.
/// </summary>
/// <remarks>
/// La especificación no da por hecho que el administrador pueda sentarse a leer
/// indiscriminadamente las conversaciones entre usuarios: el acceso al contenido se
/// limita a estos casos y <b>queda registrado</b>.
/// </remarks>
public enum ContentAccessReason
{
    /// <summary>Un usuario ha reportado algo.</summary>
    Report = 1,
    /// <summary>Moderación de contenido.</summary>
    Moderation = 2,
    /// <summary>Disputa entre las partes.</summary>
    Dispute = 3,
    /// <summary>Investigación de fraude.</summary>
    FraudInvestigation = 4,
    /// <summary>Soporte solicitado por una de las partes.</summary>
    SupportRequested = 5
}

/// <summary>
/// Acciones administrativas que quedan registradas.
/// </summary>
/// <remarks>
/// La especificación es explícita: el administrador no debe poder tocar información
/// sensible <b>sin dejar trazabilidad</b>. Cada una de estas acciones deja una fila con
/// quién la hizo, sobre quién y por qué.
/// </remarks>
public enum AdminActionType
{
    /// <summary>Cuenta reactivada.</summary>
    AccountActivated = 1,
    /// <summary>Cuenta suspendida temporalmente.</summary>
    AccountSuspended = 2,
    /// <summary>Cuenta bloqueada.</summary>
    AccountBlocked = 3,

    /// <summary>Anuncio ocultado temporalmente.</summary>
    ListingHidden = 10,
    /// <summary>Anuncio reactivado.</summary>
    ListingReactivated = 11,
    /// <summary>Anuncio marcado para revisión.</summary>
    ListingFlagged = 12,
    /// <summary>Anuncio archivado.</summary>
    ListingArchived = 13,
    /// <summary>Anuncio eliminado.</summary>
    ListingDeleted = 14,
    /// <summary>
    /// Se ha pedido a quien publica que corrija el anuncio.
    /// </summary>
    /// <remarks>
    /// La información comercial pertenece a quien publica: ante un dato incorrecto, el
    /// administrador pide la corrección en lugar de reescribirla él.
    /// </remarks>
    ListingCorrectionRequested = 15,

    /// <summary>Solicitud asignada a un administrador.</summary>
    RequestAssigned = 20,
    /// <summary>Cambio de estado de la solicitud.</summary>
    RequestStatusChanged = 21,
    /// <summary>Vehículo propuesto para la solicitud.</summary>
    RequestProposalAdded = 22,
    /// <summary>Propuesta retirada.</summary>
    RequestProposalRemoved = 23,

    /// <summary>
    /// Un administrador ha leído el contenido de una negociación privada.
    /// </summary>
    NegotiationContentAccessed = 30,
    /// <summary>Contrato invalidado administrativamente.</summary>
    ContractInvalidated = 31,

    /// <summary>Reporte cerrado (resuelto o rechazado).</summary>
    ReportResolved = 40,
    /// <summary>Advertencia enviada al usuario señalado.</summary>
    UserWarned = 41,
    /// <summary>Se ha pedido más información a quien reporta.</summary>
    ReportInfoRequested = 42,
    /// <summary>Puesto en examen: ni cerrado ni rechazado, solo en estudio.</summary>
    ReportUnderReview = 43,

    /// <summary>Ajuste manual del saldo de puntos de un usuario.</summary>
    PointsAdjusted = 50,

    /// <summary>Parámetros generales de la plataforma modificados.</summary>
    SettingsChanged = 60,
    /// <summary>Interruptor de funcionalidad encendido o apagado.</summary>
    FeatureFlagToggled = 61,
    /// <summary>Alta, cambio o retirada en un catálogo (marcas, modelos, equipamiento).</summary>
    CatalogChanged = 62
}

/// <summary>De dónde vienen los puntos de un movimiento.</summary>
public enum LoyaltyPointOrigin
{
    /// <summary>Venta verificada: el contrato quedó validado por ambas partes.</summary>
    VenteVerifiee = 1,

    /// <summary>
    /// Compensación de una venta verificada que el administrador invalidó.
    /// </summary>
    /// <remarks>
    /// Se registra como movimiento propio, en negativo, en lugar de borrar el de la
    /// venta: el libro cuenta lo que pasó, incluido lo que se deshizo.
    /// </remarks>
    VenteInvalidee = 2,

    /// <summary>Ajuste manual del administrador, siempre con motivo escrito.</summary>
    AjustementAdministrateur = 3
}
