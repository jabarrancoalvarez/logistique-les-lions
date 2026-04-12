using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class ServicePartnerConfiguration : IEntityTypeConfiguration<ServicePartner>
{
    public void Configure(EntityTypeBuilder<ServicePartner> builder)
    {
        builder.ToTable("service_partners", "marketplace");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(160).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.CountriesCsv).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ContactEmail).HasMaxLength(200);
        builder.Property(p => p.ContactPhone).HasMaxLength(50);
        builder.Property(p => p.Website).HasMaxLength(300);
        builder.Property(p => p.LogoUrl).HasMaxLength(500);
        builder.Property(p => p.Rating).HasPrecision(3, 2);
        builder.Property(p => p.BasePriceEur).HasPrecision(10, 2);

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => new { p.Type, p.IsActive });

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
