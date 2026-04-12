using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IApplicationDbContext db,
    IJwtService jwt)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var user = await db.UserProfiles
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, ct);

        if (user is null || user.RefreshTokenExpiresAt < DateTimeOffset.UtcNow)
            return Result<AuthResponseDto>.Failure("Auth.InvalidRefreshToken");

        var newRefresh             = jwt.GenerateRefreshToken();
        user.RefreshToken           = newRefresh;
        user.RefreshTokenExpiresAt  = DateTimeOffset.UtcNow.AddDays(30);
        user.LastLoginAt            = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var access    = jwt.GenerateAccessToken(user);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            access, newRefresh, expiresAt,
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName,
                        user.Role.ToString(), user.AvatarUrl, user.IsVerified)));
    }
}
