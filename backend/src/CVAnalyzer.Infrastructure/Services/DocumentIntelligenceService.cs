using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace CVAnalyzer.Infrastructure.Services;

public class DocumentIntelligenceService : IDocumentIntelligenceService
{
    private readonly DocumentAnalysisClient _client;
    private readonly ILogger<DocumentIntelligenceService> _logger;
    private readonly DocumentIntelligenceOptions _options;

    public DocumentIntelligenceService(
        DocumentAnalysisClient client,
        IOptions<DocumentIntelligenceOptions> options,
        ILogger<DocumentIntelligenceService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ExtractTextFromDocumentAsync(string blobUrlWithSas, CancellationToken cancellationToken = default)
    {
        try
        {
            var sanitizedUrl = SanitizeBlobUrl(blobUrlWithSas);
            
            var operation = await _client.AnalyzeDocumentFromUriAsync(
                WaitUntil.Completed,
                _options.ModelId,
                new Uri(blobUrlWithSas),
                cancellationToken: cancellationToken);

            var result = operation.Value;
            var lines = result.Pages.SelectMany(p => p.Lines.Select(l => l.Content)).ToList();
            var text = string.Join(Environment.NewLine, lines);

            _logger.LogInformation("Extracted {CharCount} characters from {PageCount} pages in document {BlobUrl}", 
                text.Length, result.Pages.Count, sanitizedUrl);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from document {BlobUrl}", SanitizeBlobUrl(blobUrlWithSas));
            throw new InvalidOperationException("Failed to extract text from document", ex);
        }
    }

    private static string SanitizeBlobUrl(string blobUrlWithSas)
    {
        var uri = new Uri(blobUrlWithSas);
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
    }
}
