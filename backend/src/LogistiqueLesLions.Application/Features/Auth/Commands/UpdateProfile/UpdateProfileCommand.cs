using LogistiqueLesLions.Application.Common.Models;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Phone,
    string? CountryCode,
    string? City,
    string? CompanyName,
    string? CompanyVat,
    string? Bio,
    string? AvatarUrl
) : IRequest<Result<Unit>>;
