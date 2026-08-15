using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.Register;

/// <summary>
/// Alta de una cuenta. Los campos siguen la especificación funcional: teléfono, nombre,
/// tipo de usuario, ciudad. El rol nunca se recibe del cliente: toda cuenta nueva es
/// <see cref="UserRole.User"/>.
/// </summary>
public record RegisterCommand(
    string Phone,
    string Password,
    string DisplayName,
    AccountType AccountType,
    string? Region,
    string? City,
    /// <summary>Opcional: solo para recibir notificaciones por correo.</summary>
    string? Email
) : IRequest<Result<AuthResponseDto>>;
