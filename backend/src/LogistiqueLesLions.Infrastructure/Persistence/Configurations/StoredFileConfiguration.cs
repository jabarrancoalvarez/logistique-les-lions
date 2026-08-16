using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("stored_files");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.StorageKey).HasMaxLength(400).IsRequired();
        builder.Property(f => f.FileName).HasMaxLength(300).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(f => f.Content).IsRequired();

        // Se busca siempre por la clave: es lo único que las tablas de negocio guardan.
        // Única porque dos archivos no pueden compartir la misma ruta relativa.
        builder.HasIndex(f => f.StorageKey).IsUnique();
    }
}
