using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CVAnalyzer.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add in-memory configuration with minimal required Azure settings
            // This prevents DI validation failures when Azure services are missing
            var testConfiguration = new Dictionary<string, string?>
            {
                // Use LocalDB for integration tests
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=CVAnalyzerTestDb;Trusted_Connection=True;MultipleActiveResultSets=true",
                ["UseKeyVault"] = "false",
                
                // Provide minimal Azure Storage config (required by DI)
                ["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["AzureStorage:ContainerName"] = "resumes-test",
                ["AzureStorage:QueueName"] = "resume-analysis-test",
                ["AzureStorage:UseManagedIdentity"] = "false",
                
                // Provide minimal Document Intelligence config (required by DI)
                ["DocumentIntelligence:Endpoint"] = "https://test.cognitiveservices.azure.com/",
                ["DocumentIntelligence:ApiKey"] = "test-key",
                ["DocumentIntelligence:ModelId"] = "prebuilt-read"
            };

            config.AddInMemoryCollection(testConfiguration);
        });
    }
}
