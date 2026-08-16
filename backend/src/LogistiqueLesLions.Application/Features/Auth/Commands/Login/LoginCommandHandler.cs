using LogistiqueLesLions.Application.Common;
using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(
    IApplicationDbContext db,
    IJwtService jwt)
    : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        // Un cuerpo incompleto no puede tumbar el endpoint: es anónimo, y es el primero
        // que recibe peticiones malformadas desde fuera. Sin esto, un JSON sin
        // «identifier» llegaba nulo a la normalización y devolvía un 500.
        if (string.IsNullOrWhiteSpace(request.Identifier) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponseDto>.Failure("Auth.InvalidCredentials");
        }

        var phone = SenegalPhone.Normalize(request.Identifier);

        var user = phone is not null
            ? await db.UserProfiles.FirstOrDefaultAsync(u => u.Phone == phone, ct)
            : await LookupByEmailAsync(request.Identifier, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponseDto>.Failure("Auth.InvalidCredentials");

        if (user.Status == AccountStatus.Blocked)
            return Result<AuthResponseDto>.Failure("Auth.AccountBlocked");

        if (user.Status == AccountStatus.Suspended)
            return Result<AuthResponseDto>.Failure("Auth.AccountSuspended");

        // Una fila por sesión: entrar desde el móvil ya no expulsa al ordenador.
        var refreshToken = jwt.GenerateRefreshToken();
        db.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId    = user.Id,
            Token     = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });

        user.LastLoginAt           = DateTimeOffset.UtcNow;
        user.LastActivityAt        = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var access    = jwt.GenerateAccessToken(user);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        return Result<AuthResponseDto>.Success(
            new AuthResponseDto(access, refreshToken, expiresAt, UserDto.From(user)));
    }

    private Task<Domain.Entities.UserProfile?> LookupByEmailAsync(string identifier, CancellationToken ct)
    {
        var email = identifier.Trim().ToLowerInvariant();
        return db.UserProfiles.FirstOrDefaultAsync(u => u.Email == email, ct);
    }
}
