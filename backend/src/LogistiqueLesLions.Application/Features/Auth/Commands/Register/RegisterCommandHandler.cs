using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
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
        var emailLower = request.Email.Trim().ToLowerInvariant();

        if (await db.UserProfiles.AnyAsync(u => u.Email == emailLower, ct))
            return Result<AuthResponseDto>.Failure("Auth.EmailAlreadyExists");

        var user = new UserProfile
        {
            Email        = emailLower,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName    = request.FirstName.Trim(),
            LastName     = request.LastName.Trim(),
            Role         = request.Role,
            Phone        = request.Phone,
            CountryCode  = request.CountryCode,
            CompanyName  = request.CompanyName,
            CompanyVat   = request.CompanyVat,
            IsActive     = true
        };

        var refreshToken        = jwt.GenerateRefreshToken();
        user.RefreshToken        = refreshToken;
        user.RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
        user.LastLoginAt         = DateTimeOffset.UtcNow;

        db.UserProfiles.Add(user);
        await db.SaveChangesAsync(ct);

        var access    = jwt.GenerateAccessToken(user);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            access, refreshToken, expiresAt,
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName,
                        user.Role.ToString(), user.AvatarUrl, user.IsVerified)));
    }
}
