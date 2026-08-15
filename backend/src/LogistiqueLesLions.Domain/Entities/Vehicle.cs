using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Agregado raíz principal: un anuncio de vehículo del Marketplace.
/// </summary>
public class Vehicle : AuditableEntity
{
    // ─── Identificación ────────────────────────────────────────────────────
    /// <summary>
    /// Referencia pública única mostrada al usuario: "YU12345". Se usa en chat, ofertas,
    /// contratos, administración y soporte para no exponer el UUID interno.
    /// </summary>
    public string PublicReference { get; set; } = string.Empty;

    /// <summary>URL amigable única: "toyota-rav4-2019-12345"</summary>
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Texto libre escrito por quien publica («Description du vendeur»).
    /// ⚠️ Yoon u Auto no lo modifica ni lo genera mediante IA.
    /// </summary>
    public string? Description { get; set; }

    // ─── Clasificación ─────────────────────────────────────────────────────
    public Guid MakeId { get; set; }
    public Guid? ModelId { get; set; }
    /// <summary>Versión/acabado: "2.0 VVT-i", "1.6 HDi Allure"… Opcional.</summary>
    public string? Version { get; set; }
    public int Year { get; set; }
    public int? Mileage { get; set; }
    public VehicleCondition Condition { get; set; }
    public BodyType? BodyType { get; set; }
    public FuelType? FuelType { get; set; }
    public TransmissionType? Transmission { get; set; }
    public string? Color { get; set; }
    public string? Vin { get; set; }

    // ─── Motor y características técnicas ──────────────────────────────────
    /// <summary>Potencia en caballos fiscales/DIN.</summary>
    public int? PowerCv { get; set; }
    /// <summary>Cilindrada en centímetros cúbicos.</summary>
    public int? EngineDisplacementCc { get; set; }
    public Drivetrain? Drivetrain { get; set; }
    /// <summary>Motorización, cuando proceda: "1.6 HDi 115".</summary>
    public string? EngineName { get; set; }
    public int? Doors { get; set; }
    public int? Seats { get; set; }

    // ─── Situación aduanera ────────────────────────────────────────────────
    /// <summary>
    /// Declarado por quien publica. Nullable únicamente por los anuncios anteriores a
    /// la migración: los nuevos lo exigen.
    /// </summary>
    public CustomsStatus? CustomsStatus { get; set; }

    // ─── Precio ────────────────────────────────────────────────────────────
    public decimal Price { get; set; }
    /// <summary>Código ISO 4217. Yoon u Auto opera únicamente en francos CFA (XOF/FCFA).</summary>
    public string Currency { get; set; } = "XOF";
    public bool PriceNegotiable { get; set; }

    // ─── Ubicación (Senegal) ───────────────────────────────────────────────
    /// <summary>Código de región senegalesa: DK, TH, SL…</summary>
    public string? Region { get; set; }
    public string? City { get; set; }
    /// <summary>Zona o barrio. Nunca se muestra la dirección exacta.</summary>
    public string? District { get; set; }

    /// <summary>
    /// Código ISO del país. Heredado de los módulos de importación/exportación
    /// anteriores, que siguen dependiendo de él. Ver docs/MODULOS-LEGACY.md.
    /// </summary>
    public string CountryOrigin { get; set; } = "SN";
    public string? PostalCode { get; set; }

    // ─── Estado del anuncio ────────────────────────────────────────────────
    public VehicleStatus Status { get; set; } = VehicleStatus.Brouillon;
    public bool IsFeatured { get; set; }
    /// <summary>Heredado de los módulos de exportación. Ver docs/MODULOS-LEGACY.md.</summary>
    public bool IsExportReady { get; set; }

    // ─── Contadores (desnormalizados para rendimiento) ─────────────────────
    public int ViewsCount { get; set; }
    public int FavoritesCount { get; set; }
    public int ContactsCount { get; set; }

    // ─── Propietario ───────────────────────────────────────────────────────
    public Guid SellerId { get; set; }
    public Guid? DealerId { get; set; }

    // ─── Fechas de ciclo de vida ───────────────────────────────────────────
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? ReservedAt { get; set; }
    public DateTimeOffset? SoldAt { get; set; }

    // ─── Moderación ────────────────────────────────────────────────────────
    /// <summary>
    /// Ocultado por un administrador.
    /// </summary>
    /// <remarks>
    /// Es una marca aparte del estado a propósito: «En pause» es una decisión de quien
    /// publica, y si se usara la misma podría deshacer la medida él mismo. Solo un
    /// administrador levanta esto.
    /// </remarks>
    public DateTimeOffset? AdminHiddenAt { get; set; }

    /// <summary>
    /// Marcado para revisión. No oculta el anuncio: es una señal interna.
    /// </summary>
    public DateTimeOffset? AdminFlaggedAt { get; set; }

    // ─── Full-text search (gestionado por EF/PostgreSQL) ──────────────────
    /// <summary>Columna tsvector: generada automáticamente por PostgreSQL trigger</summary>
    public string? SearchVector { get; set; }

    // ─── Navegación ────────────────────────────────────────────────────────
    public VehicleMake Make { get; set; } = null!;
    public VehicleModel? Model { get; set; }
    public ICollection<VehicleImage> Images { get; set; } = [];
    public ICollection<VehicleEquipmentLink> Equipments { get; set; } = [];
    public ICollection<VehiclePriceHistory> PriceHistory { get; set; } = [];
    public UserProfile? Seller { get; set; }

    // ─── Reglas de estado ──────────────────────────────────────────────────
    /// <summary>Visible en el Marketplace y en las búsquedas.</summary>
    public bool IsPubliclyVisible =>
        Status is VehicleStatus.Actif or VehicleStatus.Reserve
        && AdminHiddenAt is null;

    /// <summary>
    /// Un anuncio vendido conserva su ficha por referencias, favoritos, comparaciones y
    /// contratos, pero deja de admitir ofertas y nuevos contactos.
    /// </summary>
    public bool AcceptsNegotiation => Status == VehicleStatus.Actif && AdminHiddenAt is null;
}
