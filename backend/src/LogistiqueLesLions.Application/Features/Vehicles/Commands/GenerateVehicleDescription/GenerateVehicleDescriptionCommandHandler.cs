using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Vehicles.Commands.GenerateVehicleDescription;

/// <summary>
/// ⛔ Funcionalidad desactivada.
/// </summary>
/// <remarks>
/// La especificación funcional de Yoon u Auto es explícita: «Yoon u Auto no modificará
/// ni generará mediante IA esta descripción». El bloque «Description du vendeur» debe
/// mostrar íntegramente el texto que escribió quien publica el anuncio.
///
/// El handler se conserva —en lugar de eliminarlo— porque pertenece al inventario de
/// módulos heredados pendiente de decisión (docs/MODULOS-LEGACY.md, parte P35). Devuelve
/// un fallo en lugar de escribir en <c>Vehicle.Description</c>.
/// </remarks>
public class GenerateVehicleDescriptionCommandHandler
    : IRequestHandler<GenerateVehicleDescriptionCommand, Result<GenerateVehicleDescriptionResult>>
{
    public Task<Result<GenerateVehicleDescriptionResult>> Handle(
        GenerateVehicleDescriptionCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(
            Result<GenerateVehicleDescriptionResult>.Failure("Vehicle.AiDescriptionDisabled"));
}
