namespace LogistiqueLesLions.Infrastructure.Services.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"Resend" | "Console". Default: Console.</summary>
    public string Provider { get; set; } = "Console";

    public string FromAddress { get; set; } = "no-reply@logistiqueleslions.com";
    public string FromName    { get; set; } = "Logistique Les Lions";

    public ResendOptions Resend { get; set; } = new();
}

public class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.resend.com";
}
