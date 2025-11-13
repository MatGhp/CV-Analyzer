namespace CVAnalyzer.Application.Common.Interfaces;

public interface IDocumentIntelligenceService
{
    Task<string> ExtractTextFromDocumentAsync(string blobUrlWithSas, CancellationToken cancellationToken = default);
}
