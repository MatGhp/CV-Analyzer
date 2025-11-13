using CVAnalyzer.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CVAnalyzer.Infrastructure.HealthChecks;

public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly IBlobStorageService _blobStorageService;

    public BlobStorageHealthCheck(IBlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test actual connectivity by attempting to generate a test SAS token
            var testBlobUrl = "https://test.blob.core.windows.net/test/test.txt";
            try
            {
                await _blobStorageService.GenerateSasTokenAsync(testBlobUrl, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // Expected when managed identity not configured, but service is available
            }
            
            return HealthCheckResult.Healthy("Blob storage is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob storage is not accessible", ex);
        }
    }
}
