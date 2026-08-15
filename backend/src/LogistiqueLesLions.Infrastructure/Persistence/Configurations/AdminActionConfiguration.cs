using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class AdminActionConfiguration : IEntityTypeConfiguration<AdminAction>
{
    public void Configure(EntityTypeBuilder<AdminAction> builder)
    {
        builder.ToTable("admin_actions", "users");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.TargetType).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Reason).HasMaxLength(1000);
        builder.Property(a => a.OldValue).HasMaxLength(500);
        builder.Property(a => a.NewValue).HasMaxLength(500);

        // El histórico se lee por afectado y en orden cronológico.
        builder.HasIndex(a => new { a.TargetType, a.TargetId, a.CreatedAt });
        // …y el journal d'activité, por fecha a secas.
        builder.HasIndex(a => a.CreatedAt);

        // Append-only: no lleva filtro de soft delete porque nunca se borra.
        builder.HasOne(a => a.Admin)
            .WithMany()
            .HasForeignKey(a => a.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AdminNoteConfiguration : IEntityTypeConfiguration<AdminNote>
{
    public void Configure(EntityTypeBuilder<AdminNote> builder)
    {
        builder.ToTable("admin_notes", "users");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.TargetType).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Body).HasMaxLength(2000).IsRequired();

        builder.HasIndex(n => new { n.TargetType, n.TargetId, n.CreatedAt });

        builder.HasQueryFilter(n => n.DeletedAt == null);

        builder.HasOne(n => n.Admin)
            .WithMany()
            .HasForeignKey(n => n.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
