using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports", "messaging");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.PublicReference).HasMaxLength(20).IsRequired();
        builder.HasIndex(r => r.PublicReference).IsUnique();

        builder.Property(r => r.TargetType).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Reason).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Description).HasMaxLength(2000);
        builder.Property(r => r.Evidence).HasMaxLength(2000);
        builder.Property(r => r.Resolution).HasMaxLength(2000);

        // La bandeja se lee por estado, y el aviso de «reportado» por objetivo.
        builder.HasIndex(r => new { r.Status, r.CreatedAt });
        builder.HasIndex(r => new { r.TargetType, r.TargetId, r.Status });

        builder.Ignore(r => r.IsOpen);

        builder.HasQueryFilter(r => r.DeletedAt == null);

        builder.HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReportedUser)
            .WithMany()
            .HasForeignKey(r => r.ReportedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.HandledByAdmin)
            .WithMany()
            .HasForeignKey(r => r.HandledByAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
