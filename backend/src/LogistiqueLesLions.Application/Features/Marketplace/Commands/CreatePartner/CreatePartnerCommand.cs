using LogistiqueLesLions.Application.Common.Interfaces;
using LogistiqueLesLions.Application.Common.Models;
using LogistiqueLesLions.Domain.Entities;
using LogistiqueLesLions.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogistiqueLesLions.Application.Features.Marketplace.Commands.CreatePartner;

public record CreatePartnerCommand(
    PartnerType Type,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyList<string> Countries,
    string? ContactEmail,
    string? ContactPhone,
    string? Website,
    string? LogoUrl,
    decimal? BasePriceEur,
    bool IsVerified = false
) : IRequest<Result<Guid>>;

public class CreatePartnerCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreatePartnerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePartnerCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<Guid>.Failure("Partner.NameRequired");
        if (string.IsNullOrWhiteSpace(request.Slug))
            return Result<Guid>.Failure("Partner.SlugRequired");
        if (request.Countries is null || request.Countries.Count == 0)
            return Result<Guid>.Failure("Partner.CountriesRequired");

        var slug = request.Slug.Trim().ToLowerInvariant();
        var exists = await db.ServicePartners.AnyAsync(p => p.Slug == slug, ct);
        if (exists)
            return Result<Guid>.Failure("Partner.SlugConflict");

        var partner = new ServicePartner
        {
            Type         = request.Type,
            Name         = request.Name.Trim(),
            Slug         = slug,
            Description  = request.Description?.Trim(),
            CountriesCsv = string.Join(',', request.Countries.Select(c => c.Trim().ToUpperInvariant())),
            ContactEmail = request.ContactEmail?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            Website      = request.Website?.Trim(),
            LogoUrl      = request.LogoUrl?.Trim(),
            BasePriceEur = request.BasePriceEur,
            IsVerified   = request.IsVerified,
            IsActive     = true,
            Rating       = 0m,
            ReviewsCount = 0
        };

        db.ServicePartners.Add(partner);
        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(partner.Id);
    }
}
