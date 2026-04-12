namespace LogistiqueLesLions.Domain.Entities;

public class NewsletterSubscriber
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset SubscribedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
