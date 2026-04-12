using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles", "users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(30);
        builder.Property(u => u.AvatarUrl).HasMaxLength(512);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(50);
        builder.Property(u => u.CountryCode).HasMaxLength(2);
        builder.Property(u => u.City).HasMaxLength(100);
        builder.Property(u => u.CompanyName).HasMaxLength(200);
        builder.Property(u => u.CompanyVat).HasMaxLength(50);
        builder.Property(u => u.Bio).HasMaxLength(1000);
        builder.Property(u => u.RefreshToken).HasMaxLength(256);

        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
