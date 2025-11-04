namespace CVAnalyzer.Application.Common.Interfaces;

public interface IAIResumeAnalyzerService
{
    Task<(double score, string optimizedContent)> AnalyzeAsync(string content, CancellationToken cancellationToken = default);
}
