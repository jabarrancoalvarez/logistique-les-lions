using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Auth.Queries.GetProfile;

public class GetProfileQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null)
            return Result<ProfileDto>.Failure("User.NotFound");

        return Result<ProfileDto>.Success(new ProfileDto(
            user.Id, user.Email, user.FirstName, user.LastName,
            user.Role.ToString(), user.Phone, user.AvatarUrl,
            user.CountryCode, user.City, user.CompanyName, user.CompanyVat,
            user.Bio, user.IsVerified, user.LastLoginAt, user.CreatedAt));
    }
}
