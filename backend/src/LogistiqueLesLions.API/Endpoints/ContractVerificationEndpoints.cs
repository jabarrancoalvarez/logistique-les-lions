using LogistiqueLesLions.Application.Features.Negotiations;
using MediatR;

namespace LogistiqueLesLions.API.Endpoints;

/// <summary>
/// Comprobación pública de una venta a partir del código del QR del contrato.
/// </summary>
public static class ContractVerificationEndpoints
{
    public static RouteGroupBuilder MapContractVerificationEndpoints(this RouteGroupBuilder group)
    {
        // Sin autenticación: quien escanea el QR tiene el contrato delante y puede no
        // tener cuenta. Cubierto por el rate limiter por IP definido en Program.cs.
        group.AllowAnonymous();

        // GET /api/v1/public/contracts/{code}
        group.MapGet("/{code}", async (string code, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new VerifyContractQuery(code), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithName("VerifyContract")
        .WithSummary("Verificar una venta con el código del QR");

        return group;
    }
}
