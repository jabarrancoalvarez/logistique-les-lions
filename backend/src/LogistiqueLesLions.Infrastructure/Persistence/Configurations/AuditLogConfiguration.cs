using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "vehicles");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.EntityName).HasMaxLength(128).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(32).IsRequired();
        builder.Property(a => a.UserEmail).HasMaxLength(256);
        builder.Property(a => a.CorrelationId).HasMaxLength(64);

        // Usar jsonb en PostgreSQL para snapshots
        builder.Property(a => a.OldValues).HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnType("jsonb");

        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Timestamp);
    }
}
