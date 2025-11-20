namespace CVAnalyzer.AgentService;

public sealed class AgentServiceOptions
{
    public const string SectionName = "Agent";

    public string Endpoint { get; set; } = string.Empty;

    public string Deployment { get; set; } = "gpt-4o";

    public double Temperature { get; set; } = 0.15;

    public double TopP { get; set; } = 0.95;

    public string? ApiKey { get; set; }
}
