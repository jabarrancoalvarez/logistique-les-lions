using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Enums;
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

        var activeListings = await db.Vehicles
            .AsNoTracking()
            .CountAsync(v => v.SellerId == user.Id && v.Status == VehicleStatus.Actif, ct);

        return Result<ProfileDto>.Success(new ProfileDto(
            user.Id,
            user.DisplayName,
            user.Phone,
            user.PhoneVerified,
            user.Email,
            user.Role.ToString(),
            user.AccountType.ToString(),
            user.AvatarUrl,
            user.Region,
            user.City,
            user.Bio,
            user.AllowWhatsAppContact,
            user.VerifiedSalesCount,
            activeListings,
            user.LastLoginAt,
            user.CreatedAt));
    }
}
