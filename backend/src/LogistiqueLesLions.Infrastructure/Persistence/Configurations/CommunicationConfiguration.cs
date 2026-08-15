using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class CommunicationConfiguration : IEntityTypeConfiguration<Communication>
{
    public void Configure(EntityTypeBuilder<Communication> builder)
    {
        builder.ToTable("communications", "messaging");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Audience).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Region).HasMaxLength(10);
        builder.Property(c => c.Title).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Body).HasMaxLength(4000).IsRequired();

        // El histórico se lee del más reciente al más antiguo.
        builder.HasIndex(c => c.SentAt);

        builder.HasQueryFilter(c => c.DeletedAt == null);

        builder.HasOne(c => c.Admin)
            .WithMany()
            .HasForeignKey(c => c.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.TargetUser)
            .WithMany()
            .HasForeignKey(c => c.TargetUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
