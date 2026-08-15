using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Solicitud «Trouvez-moi une voiture»: el usuario pide a Yoon u Auto que le busque un
/// vehículo que no ha encontrado entre los anuncios.
/// </summary>
/// <remarks>
/// Gratuita y disponible para cualquier usuario registrado, sea Particulier o
/// Professionnel. No es un formulario que mande un correo: es una entidad que el
/// administrador gestiona desde el backoffice.
/// </remarks>
public class VehicleRequest : AuditableEntity
{
    /// <summary>Referencia pública legible: "YD00248".</summary>
    public string PublicReference { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    // ─── Datos del vehículo buscado ────────────────────────────────────────
    /// <summary>Marca del catálogo. Nullable por si el usuario pide algo no listado.</summary>
    public Guid? MakeId { get; set; }
    /// <summary>Marca tal y como se mostró al usuario, congelada por si el catálogo cambia.</summary>
    public string MakeName { get; set; } = string.Empty;
    /// <summary>Texto libre: el usuario puede pedir un modelo que aún no está en catálogo.</summary>
    public string? ModelName { get; set; }
    public string? Version { get; set; }

    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public int? MaxMileage { get; set; }

    public FuelType? FuelType { get; set; }
    public TransmissionType? Transmission { get; set; }
    public BodyType? BodyType { get; set; }
    public string? Color { get; set; }

    /// <summary>Equipamiento o características especialmente importantes. Texto libre.</summary>
    public string? ImportantEquipment { get; set; }

    // ─── Presupuesto y procedencia ─────────────────────────────────────────
    /// <summary>Presupuesto máximo en FCFA.</summary>
    public decimal? MaxBudget { get; set; }
    public VehicleRequestOrigin Origin { get; set; } = VehicleRequestOrigin.Indifferent;

    /// <summary>
    /// «Précisez votre recherche». Se almacena y se presenta al administrador tal cual.
    /// ⚠️ No hay IA procesando este texto.
    /// </summary>
    public string? Notes { get; set; }

    // ─── Gestión ───────────────────────────────────────────────────────────
    public VehicleRequestStatus Status { get; set; } = VehicleRequestStatus.NouvelleDemande;
    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>
    /// Administrador que se ha hecho cargo de la solicitud.
    /// </summary>
    /// <remarks>
    /// Aquí el administrador deja de moderar y presta un servicio: con varias personas
    /// en el equipo, saber quién lleva cada solicitud evita que dos la trabajen o que
    /// ninguna lo haga.
    /// </remarks>
    public Guid? AssignedAdminId { get; set; }
    public UserProfile? AssignedAdmin { get; set; }

    // ─── Navegación ────────────────────────────────────────────────────────
    public UserProfile? User { get; set; }
    public VehicleMake? Make { get; set; }
    public ICollection<VehicleRequestMessage> Messages { get; set; } = [];
    public ICollection<VehicleRequestProposal> Proposals { get; set; } = [];

    /// <summary>Mientras no esté finalizada, el usuario puede cancelarla.</summary>
    public bool CanBeCancelled =>
        Status is not (VehicleRequestStatus.Terminee or VehicleRequestStatus.Annulee);
}
