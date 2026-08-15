using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.Login;

/// <summary>
/// Inicio de sesión. <paramref name="Identifier"/> admite el teléfono (identificador
/// principal) o el correo, para que las cuentas creadas antes de la migración sigan
/// pudiendo entrar.
/// </summary>
public record LoginCommand(string Identifier, string Password) : IRequest<Result<AuthResponseDto>>;
