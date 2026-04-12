using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class ProcessIncidentConfiguration : IEntityTypeConfiguration<ProcessIncident>
{
    public void Configure(EntityTypeBuilder<ProcessIncident> builder)
    {
        builder.ToTable("process_incidents", "compliance");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(4000);
        builder.Property(i => i.Resolution).HasMaxLength(4000);
        builder.Property(i => i.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(i => i.Process)
            .WithMany()
            .HasForeignKey(i => i.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.ProcessId);
        builder.HasIndex(i => i.Status);
        builder.HasQueryFilter(i => i.DeletedAt == null);
    }
}
