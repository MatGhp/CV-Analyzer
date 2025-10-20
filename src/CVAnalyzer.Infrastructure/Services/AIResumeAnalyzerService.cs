using CVAnalyzer.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Infrastructure.Services;

public class AIResumeAnalyzerService : IAIResumeAnalyzerService
{
    private readonly ILogger<AIResumeAnalyzerService> _logger;

    public AIResumeAnalyzerService(ILogger<AIResumeAnalyzerService> logger)
    {
        _logger = logger;
    }

    public async Task<(double score, string optimizedContent)> AnalyzeAsync(string content, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing resume content");
        await Task.CompletedTask;
        return (85.5, $"Optimized: {content}");
    }
}
