using System;
using System.Collections.Generic;

namespace CVAnalyzer.AgentService.Models;

public sealed class ResumeAnalysisResponse
{
    public double Score { get; init; }

    public string OptimizedContent { get; init; } = string.Empty;

    public IReadOnlyList<ResumeSuggestion> Suggestions { get; init; } = Array.Empty<ResumeSuggestion>();

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
