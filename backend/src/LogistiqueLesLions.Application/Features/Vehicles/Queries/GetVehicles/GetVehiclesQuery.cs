using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;

/// <summary>
/// Búsqueda paginada del Marketplace con los filtros de la especificación funcional.
/// </summary>
/// <remarks>
/// Se declara con propiedades <c>init</c> y no de forma posicional: son más de treinta
/// filtros y una lista posicional sería ilegible y fácil de desordenar al construirla.
/// </remarks>
public record GetVehiclesQuery : IRequest<Result<PagedResult<VehicleListDto>>>
{
    /// <summary>Barra de búsqueda simple: "Toyota Corolla" o cualquier palabra.</summary>
    public string? Search { get; init; }

    // ─── Marca y modelo ────────────────────────────────────────────────────
    public Guid? MakeId { get; init; }
    public Guid? ModelId { get; init; }

    // ─── Precio (FCFA) ─────────────────────────────────────────────────────
    public decimal? PriceFrom { get; init; }
    public decimal? PriceTo { get; init; }

    // ─── Año ───────────────────────────────────────────────────────────────
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }

    // ─── Kilometraje ───────────────────────────────────────────────────────
    public int? MileageFrom { get; init; }
    public int? MileageTo { get; init; }

    // ─── Ubicación ─────────────────────────────────────────────────────────
    public string? Region { get; init; }
    public string? City { get; init; }

    /// <summary>Filtro destacado por su importancia en Senegal.</summary>
    public CustomsStatus? CustomsStatus { get; init; }

    // ─── Mecánica ──────────────────────────────────────────────────────────
    public FuelType? FuelType { get; init; }
    public TransmissionType? Transmission { get; init; }
    public BodyType? BodyType { get; init; }
    public Drivetrain? Drivetrain { get; init; }
    public VehicleCondition? Condition { get; init; }

    public int? PowerFrom { get; init; }
    public int? PowerTo { get; init; }
    public int? DisplacementFrom { get; init; }
    public int? DisplacementTo { get; init; }

    public int? DoorsFrom { get; init; }
    public int? DoorsTo { get; init; }
    /// <summary>Para "8+" se envía solo <see cref="SeatsFrom"/>.</summary>
    public int? SeatsFrom { get; init; }
    public int? SeatsTo { get; init; }

    public string? Color { get; init; }

    /// <summary>Selección múltiple: el anuncio debe declarar <b>todos</b> los indicados.</summary>
    public IReadOnlyList<Guid>? EquipmentIds { get; init; }

    /// <summary>Tipo de usuario que publica. <c>null</c> = todos.</summary>
    public AccountType? SellerAccountType { get; init; }

    // ─── Heredado de los módulos de exportación ────────────────────────────
    public string? CountryOrigin { get; init; }
    public bool? IsExportReady { get; init; }

    // ─── Uso interno ───────────────────────────────────────────────────────
    public bool? IsFeatured { get; init; }
    public Guid? SellerId { get; init; }
    public VehicleStatus? Status { get; init; }

    /// <summary>
    /// Incluye borradores, pausados y archivados.
    /// </summary>
    /// <remarks>
    /// ⚠️ Solo debe activarlo la API cuando quien consulta es el propietario de los
    /// anuncios o un administrador. Si se dedujera de <see cref="SellerId"/>, cualquiera
    /// podría ver los borradores ajenos pasando el identificador de otro usuario.
    /// </remarks>
    public bool IncludeNonPublic { get; init; }

    // ─── Ordenación y paginación ───────────────────────────────────────────
    public string SortBy { get; init; } = "createdAt";
    public bool SortDesc { get; init; } = true;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
