using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries", "vehicles");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Code).HasMaxLength(2).IsRequired();
        builder.Property(c => c.NameEs).HasMaxLength(100).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(c => c.FlagEmoji).HasMaxLength(10);
        builder.Property(c => c.Currency).HasMaxLength(3).HasDefaultValue("EUR");
        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasQueryFilter(c => c.DeletedAt == null);

        // ─── Seed data: países de la plataforma ────────────────────────────
        builder.HasData(
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Code = "ES", NameEs = "España", NameEn = "Spain", FlagEmoji = "🇪🇸", Currency = "EUR", IsEuMember = true, SupportsImport = true, SupportsExport = true, IsActive = true, DisplayOrder = 1, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Code = "DE", NameEs = "Alemania", NameEn = "Germany", FlagEmoji = "🇩🇪", Currency = "EUR", IsEuMember = true, SupportsImport = true, SupportsExport = true, IsActive = true, DisplayOrder = 2, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Code = "FR", NameEs = "Francia", NameEn = "France", FlagEmoji = "🇫🇷", Currency = "EUR", IsEuMember = true, SupportsImport = true, SupportsExport = true, IsActive = true, DisplayOrder = 3, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Code = "IT", NameEs = "Italia", NameEn = "Italy", FlagEmoji = "🇮🇹", Currency = "EUR", IsEuMember = true, SupportsImport = true, SupportsExport = true, IsActive = true, DisplayOrder = 4, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Code = "PT", NameEs = "Portugal", NameEn = "Portugal", FlagEmoji = "🇵🇹", Currency = "EUR", IsEuMember = true, SupportsImport = true, SupportsExport = true, IsActive = true, DisplayOrder = 5, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Code = "NL", NameEs = "Países Bajos", NameEn = "Netherlands", FlagEmoji = "🇳🇱", Currency = "EUR", IsEuMember = true, SupportsImport = true, SupportsExport = true, IsActive = true, DisplayOrder = 6, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Code = "BE", NameEs = "Bélgica", NameEn = "Belgium", FlagEmoji = "🇧🇪", Currency = "EUR", IsEuMember = true, SupportsImport = true, SupportsExport = true, IsActive = true, DisplayOrder = 7, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), Code = "GB", NameEs = "Reino Unido", NameEn = "United Kingdom", FlagEmoji = "🇬🇧", Currency = "GBP", IsEuMember = false, SupportsImport = true, SupportsExport = true, IsActive = true, DisplayOrder = 8, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000009"), Code = "US", NameEs = "Estados Unidos", NameEn = "United States", FlagEmoji = "🇺🇸", Currency = "USD", IsEuMember = false, SupportsImport = true, SupportsExport = false, IsActive = true, DisplayOrder = 9, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000010"), Code = "MA", NameEs = "Marruecos", NameEn = "Morocco", FlagEmoji = "🇲🇦", Currency = "MAD", IsEuMember = false, SupportsImport = false, SupportsExport = true, IsActive = true, DisplayOrder = 10, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000011"), Code = "JP", NameEs = "Japón", NameEn = "Japan", FlagEmoji = "🇯🇵", Currency = "JPY", IsEuMember = false, SupportsImport = true, SupportsExport = false, IsActive = true, DisplayOrder = 11, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            new Country { Id = Guid.Parse("10000000-0000-0000-0000-000000000012"), Code = "CH", NameEs = "Suiza", NameEn = "Switzerland", FlagEmoji = "🇨🇭", Currency = "CHF", IsEuMember = false, SupportsImport = true, SupportsExport = true, IsActive = true, DisplayOrder = 12, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow }
        );
    }
}
