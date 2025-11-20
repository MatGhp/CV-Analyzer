namespace CVAnalyzer.Infrastructure.Options;

/// <summary>
/// Configuration options for prompt template caching.
/// </summary>
public class PromptCacheOptions
{
    public const string SectionName = "PromptCache";

    /// <summary>
    /// Cache expiration time in minutes.
    /// Default: 15 minutes for Production, can be adjusted per environment.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 15;
}
