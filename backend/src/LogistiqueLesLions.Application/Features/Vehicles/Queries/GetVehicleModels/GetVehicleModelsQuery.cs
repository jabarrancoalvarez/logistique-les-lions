using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Queries.GetVehicleModels;

/// <summary>
/// Modelos de una marca, para el desplegable dependiente del filtro
/// (Toyota → Corolla, RAV4, Hilux, Prado, Yaris…).
/// </summary>
public record GetVehicleModelsQuery(Guid MakeId) : IRequest<Result<List<VehicleModelDto>>>;

public record VehicleModelDto(Guid Id, string Name, int VehiclesCount);
