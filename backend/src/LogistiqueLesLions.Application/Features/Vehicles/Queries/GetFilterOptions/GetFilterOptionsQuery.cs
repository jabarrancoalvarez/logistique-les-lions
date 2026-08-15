using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetFilterOptions;

/// <summary>
/// Catálogos que necesita el panel de filtros y que no son enums fijos.
/// </summary>
public record GetFilterOptionsQuery : IRequest<Result<FilterOptionsDto>>;

public record FilterOptionsDto(
    IReadOnlyList<EquipmentOptionDto> Equipments,
    /// <summary>Colores realmente presentes en los anuncios publicados.</summary>
    IReadOnlyList<string> Colors
);

public record EquipmentOptionDto(Guid Id, string Code, string Name);
