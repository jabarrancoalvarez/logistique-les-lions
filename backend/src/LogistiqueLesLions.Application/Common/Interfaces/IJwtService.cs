using LogistiqueLesLions.Domain.Entities;

namespace LogistiqueLesLions.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(UserProfile user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
}
