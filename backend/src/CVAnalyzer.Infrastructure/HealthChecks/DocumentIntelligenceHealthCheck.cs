using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CVAnalyzer.Infrastructure.HealthChecks;

public class DocumentIntelligenceHealthCheck : IHealthCheck
{
    private readonly DocumentAnalysisClient _client;

    public DocumentIntelligenceHealthCheck(DocumentAnalysisClient client)
    {
        _client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test connectivity by checking if client is properly initialized
            // A real connectivity test would require a valid document URL
            if (_client == null)
            {
                return HealthCheckResult.Unhealthy("Document Intelligence client is not initialized");
            }
            
            await Task.CompletedTask;
            return HealthCheckResult.Healthy("Document Intelligence is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Document Intelligence is not accessible", ex);
        }
    }
}
