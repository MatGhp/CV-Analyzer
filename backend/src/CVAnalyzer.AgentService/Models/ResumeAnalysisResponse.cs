using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CVAnalyzer.AgentService.Models;

public sealed class ResumeAnalysisResponse
{
    public double Score { get; init; }

    public string OptimizedContent { get; init; } = string.Empty;

    public IReadOnlyList<ResumeSuggestion> Suggestions { get; init; } = Array.Empty<ResumeSuggestion>();

    public CandidateInfoDto? CandidateInfo { get; init; }

    public IReadOnlyDictionary<string, JsonElement> Metadata { get; init; } = new Dictionary<string, JsonElement>();
}
