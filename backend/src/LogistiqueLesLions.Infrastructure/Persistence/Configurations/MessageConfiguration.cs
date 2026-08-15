using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages", "messaging");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Body).HasMaxLength(4000).IsRequired();

        builder.HasOne(m => m.Negotiation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.NegotiationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.NegotiationId);

        builder.HasQueryFilter(m => m.DeletedAt == null);
    }
}
