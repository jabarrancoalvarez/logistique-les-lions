using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Auth.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateProfileCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await db.UserProfiles
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null)
            return Result<Unit>.Failure("User.NotFound");

        user.FirstName   = request.FirstName.Trim();
        user.LastName    = request.LastName.Trim();
        user.Phone       = request.Phone;
        user.CountryCode = request.CountryCode;
        user.City        = request.City;
        user.CompanyName = request.CompanyName;
        user.CompanyVat  = request.CompanyVat;
        user.Bio         = request.Bio;
        if (request.AvatarUrl is not null)
            user.AvatarUrl = request.AvatarUrl;

        await db.SaveChangesAsync(ct);
        return Result<Unit>.Success(Unit.Value);
    }
}
