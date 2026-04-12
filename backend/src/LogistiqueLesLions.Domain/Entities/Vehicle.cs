using System.Text.Json;
using LogistiqueLesLions.Domain.Common;
using LogistiqueLesLions.Domain.Enums;

namespace LogistiqueLesLions.Domain.Entities;

/// <summary>
/// Agregado raíz principal: representa un anuncio de vehículo en la plataforma.
/// Incluye datos técnicos, precio, país de origen/destino y metadatos SEO.
/// </summary>
public class Vehicle : AuditableEntity
{
    // ─── Identificación ────────────────────────────────────────────────────
    /// <summary>URL amigable única: "bmw-serie-3-320d-2022-berlin"</summary>
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? DescriptionEs { get; set; }
    public string? DescriptionEn { get; set; }

    // ─── Clasificación ─────────────────────────────────────────────────────
    public Guid MakeId { get; set; }
    public Guid? ModelId { get; set; }
    public int Year { get; set; }
    public int? Mileage { get; set; }
    public VehicleCondition Condition { get; set; }
    public BodyType? BodyType { get; set; }
    public FuelType? FuelType { get; set; }
    public TransmissionType? Transmission { get; set; }
    public string? Color { get; set; }
    public string? Vin { get; set; }

    // ─── Precio y divisa ───────────────────────────────────────────────────
    public decimal Price { get; set; }
    /// <summary>Código ISO 4217: EUR, USD, GBP, MAD...</summary>
    public string Currency { get; set; } = "EUR";
    public bool PriceNegotiable { get; set; }

    // ─── Geografía ─────────────────────────────────────────────────────────
    /// <summary>Código ISO del país donde está el vehículo actualmente</summary>
    public string CountryOrigin { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    // ─── Estado del anuncio ────────────────────────────────────────────────
    public VehicleStatus Status { get; set; } = VehicleStatus.Reviewing;
    public bool IsFeatured { get; set; }
    public bool IsExportReady { get; set; }

    // ─── Datos variables (JSONB) ───────────────────────────────────────────
    /// <summary>Especificaciones técnicas adicionales: potencia, cilindrada, emisiones CO2...</summary>
    public JsonDocument? Specs { get; set; }
    /// <summary>Lista de equipamiento/extras: climatizador, navegador, sensores aparcamiento...</summary>
    public JsonDocument? Features { get; set; }

    // ─── Contadores (desnormalizados para rendimiento) ─────────────────────
    public int ViewsCount { get; set; }
    public int FavoritesCount { get; set; }
    public int ContactsCount { get; set; }

    // ─── Propietario ───────────────────────────────────────────────────────
    public Guid SellerId { get; set; }
    public Guid? DealerId { get; set; }

    // ─── Fechas de expiración ──────────────────────────────────────────────
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? SoldAt { get; set; }

    // ─── Full-text search (gestionado por EF/PostgreSQL) ──────────────────
    /// <summary>Columna tsvector: generada automáticamente por PostgreSQL trigger</summary>
    public string? SearchVector { get; set; }

    // ─── Navegación ────────────────────────────────────────────────────────
    public VehicleMake Make { get; set; } = null!;
    public VehicleModel? Model { get; set; }
    public ICollection<VehicleImage> Images { get; set; } = [];
    public UserProfile? Seller { get; set; }
}
