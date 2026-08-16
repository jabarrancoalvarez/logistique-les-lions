using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
    {
        builder.ToTable("user_refresh_tokens", "users");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Token).HasMaxLength(200).IsRequired();
        builder.Property(t => t.UserAgent).HasMaxLength(300);

        // Se busca siempre por el token: es la única entrada a esta tabla. Único además
        // de indexado, porque dos sesiones no pueden compartir el mismo valor.
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.UserId);

        builder.HasQueryFilter(t => t.DeletedAt == null);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(t => t.IsActive);
    }
}
