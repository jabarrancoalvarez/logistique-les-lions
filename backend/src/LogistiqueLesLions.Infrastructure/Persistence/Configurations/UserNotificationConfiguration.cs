using LogistiqueLesLions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogistiqueLesLions.Infrastructure.Persistence.Configurations;

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("user_notifications", "messaging");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Category).HasMaxLength(40).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(2000);
        builder.Property(n => n.Link).HasMaxLength(500);

        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.HasIndex(n => new { n.UserId, n.CreatedAt });

        builder.HasQueryFilter(n => n.DeletedAt == null);
    }
}
