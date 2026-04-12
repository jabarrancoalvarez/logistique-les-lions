namespace LogistiqueLesLions.Infrastructure.Services.Ai;

public class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public string ApiKey  { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public string Model   { get; set; } = "claude-sonnet-4-6";
    public string Version { get; set; } = "2023-06-01";
    public int MaxTokens  { get; set; } = 1024;
}
