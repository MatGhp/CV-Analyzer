namespace CVAnalyzer.AgentService.Models;

public sealed class ResumeSuggestion
{
    public string Category { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int Priority { get; init; }
}
