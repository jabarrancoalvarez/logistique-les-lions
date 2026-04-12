namespace LogistiqueLesLions.Infrastructure.Services.Webhooks;

public class WebhookOptions
{
    public const string SectionName = "Webhooks";

    public bool   Enabled { get; set; }
    public string Url     { get; set; } = string.Empty;
    /// <summary>Secret HMAC opcional. Si está presente se envía como header X-LLL-Signature (HMAC-SHA256).</summary>
    public string Secret  { get; set; } = string.Empty;
}
