using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.UpdateProfile;

/// <summary>
/// Edición de "Mon profil".
/// </summary>
/// <remarks>
/// El teléfono no se edita aquí: es el identificador de la cuenta y lleva asociada una
/// verificación, por lo que requerirá su propio flujo de cambio + reverificación.
/// </remarks>
public record UpdateProfileCommand(
    Guid UserId,
    string DisplayName,
    AccountType AccountType,
    string? Region,
    string? City,
    string? Email,
    string? Bio,
    string? AvatarUrl,
    bool AllowWhatsAppContact
) : IRequest<Result<Unit>>;
