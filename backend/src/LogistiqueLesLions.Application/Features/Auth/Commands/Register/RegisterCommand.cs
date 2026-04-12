using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
using MediatR;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role,
    string? Phone,
    string? CountryCode,
    string? CompanyName,
    string? CompanyVat
) : IRequest<Result<AuthResponseDto>>;
