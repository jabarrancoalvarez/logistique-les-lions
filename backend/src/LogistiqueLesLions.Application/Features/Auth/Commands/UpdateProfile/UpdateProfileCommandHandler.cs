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

        var email = string.IsNullOrWhiteSpace(request.Email)
            ? null
            : request.Email.Trim().ToLowerInvariant();

        if (email is not null && email != user.Email
            && await db.UserProfiles.AnyAsync(u => u.Email == email && u.Id != user.Id, ct))
            return Result<Unit>.Failure("Auth.EmailAlreadyExists");

        user.DisplayName          = request.DisplayName.Trim();
        user.AccountType          = request.AccountType;
        user.Region               = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim();
        user.City                 = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        user.Email                = email;
        user.Bio                  = request.Bio;
        user.AllowWhatsAppContact = request.AllowWhatsAppContact;
        user.LastActivityAt       = DateTimeOffset.UtcNow;

        if (request.AvatarUrl is not null)
            user.AvatarUrl = request.AvatarUrl;

        await db.SaveChangesAsync(ct);
        return Result<Unit>.Success(Unit.Value);
    }
}
