using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
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
        var emailLower = request.Email.Trim().ToLowerInvariant();
        var user = await db.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == emailLower, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponseDto>.Failure("Auth.InvalidCredentials");

        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure("Auth.AccountDisabled");

        var refreshToken           = jwt.GenerateRefreshToken();
        user.RefreshToken           = refreshToken;
        user.RefreshTokenExpiresAt  = DateTimeOffset.UtcNow.AddDays(30);
        user.LastLoginAt            = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var access    = jwt.GenerateAccessToken(user);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            access, refreshToken, expiresAt,
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName,
                        user.Role.ToString(), user.AvatarUrl, user.IsVerified)));
    }
}
