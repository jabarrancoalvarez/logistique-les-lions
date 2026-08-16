using LogistiqueLesLions.Application.Common;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler(
    IApplicationDbContext db,
    IJwtService jwt)
    : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var phone = SenegalPhone.Normalize(request.Phone);
        if (phone is null)
            return Result<AuthResponseDto>.Failure("Auth.InvalidPhone");

        if (await db.UserProfiles.AnyAsync(u => u.Phone == phone, ct))
            return Result<AuthResponseDto>.Failure("Auth.PhoneAlreadyExists");

        var email = string.IsNullOrWhiteSpace(request.Email)
            ? null
            : request.Email.Trim().ToLowerInvariant();

        if (email is not null && await db.UserProfiles.AnyAsync(u => u.Email == email, ct))
            return Result<AuthResponseDto>.Failure("Auth.EmailAlreadyExists");

        var user = new UserProfile
        {
            Phone        = phone,
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName  = request.DisplayName.Trim(),
            AccountType  = request.AccountType,
            Region       = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim(),
            City         = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            // El rol nunca procede del cliente: toda cuenta nueva es un usuario normal.
            Role         = UserRole.User,
            Status       = AccountStatus.Active
        };

        var refreshToken    = jwt.GenerateRefreshToken();
        user.LastLoginAt    = DateTimeOffset.UtcNow;
        user.LastActivityAt = DateTimeOffset.UtcNow;

        db.UserProfiles.Add(user);

        // Registrarse abre la primera sesión, como iniciar sesión abre las siguientes.
        db.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId    = user.Id,
            Token     = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync(ct);

        var access    = jwt.GenerateAccessToken(user);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        return Result<AuthResponseDto>.Success(
            new AuthResponseDto(access, refreshToken, expiresAt, UserDto.From(user)));
    }
}
