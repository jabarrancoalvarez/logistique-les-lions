using LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicles;
using LogistiqueLesLions.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LogistiqueLesLions.API.Endpoints;

/// <summary>
/// Filtros del Marketplace tal y como llegan en la query string.
/// </summary>
/// <remarks>
/// Se agrupan en un objeto y se enlazan con <c>[AsParameters]</c> porque son más de
/// treinta: como parámetros sueltos, la firma del endpoint sería inmanejable y cualquier
/// reordenación silenciosa cambiaría el significado de los valores.
/// </remarks>
public class VehicleSearchRequest
{
    [FromQuery] public string? Search { get; set; }

    [FromQuery] public Guid? MakeId { get; set; }
    [FromQuery] public Guid? ModelId { get; set; }

    [FromQuery] public decimal? PriceFrom { get; set; }
    [FromQuery] public decimal? PriceTo { get; set; }
    [FromQuery] public int? YearFrom { get; set; }
    [FromQuery] public int? YearTo { get; set; }
    [FromQuery] public int? MileageFrom { get; set; }
    [FromQuery] public int? MileageTo { get; set; }

    [FromQuery] public string? Region { get; set; }
    [FromQuery] public string? City { get; set; }

    [FromQuery] public FuelType? FuelType { get; set; }
    [FromQuery] public TransmissionType? Transmission { get; set; }
    [FromQuery] public BodyType? BodyType { get; set; }
    [FromQuery] public Drivetrain? Drivetrain { get; set; }
    [FromQuery] public VehicleCondition? Condition { get; set; }

    [FromQuery] public int? PowerFrom { get; set; }
    [FromQuery] public int? PowerTo { get; set; }
    [FromQuery] public int? DisplacementFrom { get; set; }
    [FromQuery] public int? DisplacementTo { get; set; }

    [FromQuery] public int? DoorsFrom { get; set; }
    [FromQuery] public int? DoorsTo { get; set; }
    [FromQuery] public int? SeatsFrom { get; set; }
    [FromQuery] public int? SeatsTo { get; set; }

    [FromQuery] public string? Color { get; set; }
    /// <summary>Selección múltiple: <c>?equipmentIds=…&amp;equipmentIds=…</c></summary>
    [FromQuery] public Guid[]? EquipmentIds { get; set; }

    [FromQuery] public AccountType? SellerAccountType { get; set; }

    [FromQuery] public string? CountryOrigin { get; set; }
    [FromQuery] public bool? IsExportReady { get; set; }
    [FromQuery] public bool? IsFeatured { get; set; }
    [FromQuery] public Guid? SellerId { get; set; }
    [FromQuery] public VehicleStatus? Status { get; set; }

    [FromQuery] public string? SortBy { get; set; }

    // Anulables a propósito: en una clase [AsParameters], un int o un bool no anulables
    // rompen el binding cuando el parámetro no viene en la query string, y el endpoint
    // responde 400 con el cuerpo vacío —sin llegar siquiera al handler—. Es lo que hacía
    // fallar /vehicles/count, al que el panel de filtros llama sin paginación.
    // El valor por defecto se aplica abajo, en ToQuery().
    [FromQuery] public bool? SortDesc { get; set; }
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }

    /// <param name="includeNonPublic">
    /// Lo decide el endpoint a partir del token; nunca llega en la query string.
    /// </param>
    public GetVehiclesQuery ToQuery(bool includeNonPublic = false) => new()
    {
        IncludeNonPublic  = includeNonPublic,
        Search            = Search,
        MakeId            = MakeId,
        ModelId           = ModelId,
        PriceFrom         = PriceFrom,
        PriceTo           = PriceTo,
        YearFrom          = YearFrom,
        YearTo            = YearTo,
        MileageFrom       = MileageFrom,
        MileageTo         = MileageTo,
        Region            = Region,
        City              = City,
        FuelType          = FuelType,
        Transmission      = Transmission,
        BodyType          = BodyType,
        Drivetrain        = Drivetrain,
        Condition         = Condition,
        PowerFrom         = PowerFrom,
        PowerTo           = PowerTo,
        DisplacementFrom  = DisplacementFrom,
        DisplacementTo    = DisplacementTo,
        DoorsFrom         = DoorsFrom,
        DoorsTo           = DoorsTo,
        SeatsFrom         = SeatsFrom,
        SeatsTo           = SeatsTo,
        Color             = Color,
        EquipmentIds      = EquipmentIds,
        SellerAccountType = SellerAccountType,
        CountryOrigin     = CountryOrigin,
        IsExportReady     = IsExportReady,
        IsFeatured        = IsFeatured,
        SellerId          = SellerId,
        Status            = Status,
        SortBy            = SortBy ?? "createdAt",
        SortDesc          = SortDesc ?? true,
        Page              = Page ?? 1,
        PageSize          = PageSize ?? 20
    };
}
