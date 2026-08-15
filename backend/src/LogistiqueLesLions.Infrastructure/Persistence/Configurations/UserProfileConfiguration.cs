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

        // Teléfono: identificador principal de la cuenta.
        // PostgreSQL admite varios NULL en un índice UNIQUE, por lo que las cuentas
        // anteriores a la migración que aún no tienen teléfono no rompen el índice.
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.HasIndex(u => u.Phone).IsUnique();

        // Correo: opcional, solo para notificaciones.
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(512);

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.AccountType).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(u => u.Region).HasMaxLength(10);
        builder.Property(u => u.City).HasMaxLength(100);
        builder.Property(u => u.Bio).HasMaxLength(1000);
        builder.Property(u => u.RefreshToken).HasMaxLength(256);

        // Propiedad calculada, no se persiste.
        builder.Ignore(u => u.CanSignIn);

        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
