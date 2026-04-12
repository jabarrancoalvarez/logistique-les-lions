using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.GenerateVehicleDescription;

/// <summary>
/// Genera con IA descripciones ES/EN para un vehículo existente y las persiste.
/// Si <see cref="Overwrite"/> es false, solo escribe los idiomas vacíos.
/// </summary>
public record GenerateVehicleDescriptionCommand(Guid VehicleId, bool Overwrite = true)
    : IRequest<Result<GenerateVehicleDescriptionResult>>;

public record GenerateVehicleDescriptionResult(string DescriptionEs, string DescriptionEn);
