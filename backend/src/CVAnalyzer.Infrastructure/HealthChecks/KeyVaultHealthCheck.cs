using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CVAnalyzer.Infrastructure.HealthChecks;

public class KeyVaultHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly SecretClient? _secretClient;

    public KeyVaultHealthCheck(IConfiguration configuration, SecretClient? secretClient = null)
    {
        _configuration = configuration;
        _secretClient = secretClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var useKeyVault = _configuration["UseKeyVault"] == "true";
            var keyVaultUri = _configuration["KeyVault:Uri"];
            
            // If Key Vault is not configured (UseKeyVault = false), consider it healthy but degraded
            if (!useKeyVault || string.IsNullOrWhiteSpace(keyVaultUri))
            {
                return HealthCheckResult.Degraded("Key Vault not configured (using local configuration)");
            }

            // If SecretClient is not available, Key Vault is not properly configured
            if (_secretClient == null)
            {
                return HealthCheckResult.Degraded("Key Vault configured but SecretClient not available");
            }

            // Test connectivity by attempting to get vault properties
            // This doesn't require reading any specific secret
            var properties = _secretClient.GetPropertiesOfSecretsAsync(cancellationToken);
            await properties.GetAsyncEnumerator(cancellationToken).MoveNextAsync();

            return HealthCheckResult.Healthy($"Key Vault is accessible: {keyVaultUri}");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            return HealthCheckResult.Unhealthy("Key Vault access denied (check managed identity permissions)", ex);
        }
        catch (Azure.RequestFailedException ex)
        {
            return HealthCheckResult.Unhealthy($"Key Vault request failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Key Vault is not accessible", ex);
        }
    }
}
