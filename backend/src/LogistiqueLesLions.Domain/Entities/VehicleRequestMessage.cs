using LogistiqueLesLions.Domain.Common;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Mensaje dentro de una solicitud «Trouvez-moi une voiture».
/// </summary>
/// <remarks>
/// Es un hilo <b>usuario ↔ administrador</b>, deliberadamente separado del chat entre
/// usuarios asociado a un anuncio: son dos conversaciones distintas y la especificación
/// pide no mezclarlas.
/// </remarks>
public class VehicleRequestMessage : AuditableEntity
{
    public Guid RequestId { get; set; }

    /// <summary>Autor. <c>null</c> cuando el mensaje lo genera el sistema.</summary>
    public Guid? SenderId { get; set; }

    /// <summary>Escrito desde el backoffice.</summary>
    public bool IsFromAdmin { get; set; }

    /// <summary>
    /// Nota interna del administrador.
    /// </summary>
    /// <remarks>
    /// ⚠️ Nunca debe llegar al usuario. Las consultas del lado del usuario tienen que
    /// filtrar por <c>!IsInternalNote</c> sin excepción.
    /// </remarks>
    public bool IsInternalNote { get; set; }

    public string Body { get; set; } = string.Empty;

    public VehicleRequest Request { get; set; } = null!;
    public UserProfile? Sender { get; set; }
}
