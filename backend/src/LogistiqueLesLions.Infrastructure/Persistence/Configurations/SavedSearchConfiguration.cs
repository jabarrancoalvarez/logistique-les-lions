using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.ToTable("saved_searches", "vehicles");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.FiltersJson).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(s => s.UserId);
        // Al publicarse un anuncio hay que recorrer las búsquedas con alerta activa.
        builder.HasIndex(s => s.AlertEnabled).HasFilter("alert_enabled");

        builder.HasQueryFilter(s => s.DeletedAt == null);
    }
}
