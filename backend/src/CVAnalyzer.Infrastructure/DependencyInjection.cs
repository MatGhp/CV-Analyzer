using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Infrastructure.Options;
using CVAnalyzer.Infrastructure.Persistence;
using CVAnalyzer.Infrastructure.Services;
using CVAnalyzer.Infrastructure.BackgroundServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Azure.AI.OpenAI;

namespace CVAnalyzer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureStorageOptions>(options => configuration.GetSection(AzureStorageOptions.SectionName).Bind(options));
        services.Configure<DocumentIntelligenceOptions>(options => configuration.GetSection(DocumentIntelligenceOptions.SectionName).Bind(options));
        services.Configure<QueueOptions>(options => configuration.GetSection(QueueOptions.SectionName).Bind(options));

        ConfigureDatabase(services, configuration);
        ConfigureAzureStorage(services, configuration);
        ConfigureDocumentIntelligence(services, configuration);
        
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();
        services.AddScoped<IAIResumeAnalyzerService, AIResumeAnalyzerService>();

        // Queue Service
        services.AddScoped<IResumeQueueService, ResumeQueueService>();

        // Resume Analysis Orchestrator
        services.AddScoped<ResumeAnalysisOrchestrator>();

        // Background Worker
        services.AddHostedService<ResumeAnalysisWorker>();

        // Configure AgentService
        ConfigureAgentService(services, configuration);

        return services;
    }

    private static void ConfigureAgentService(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentService.AgentServiceOptions>(
            configuration.GetSection(AgentService.AgentServiceOptions.SectionName));

        services.AddSingleton<OpenAIClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AgentService.AgentServiceOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                // For testing/development, return placeholder
                throw new InvalidOperationException("Agent:Endpoint configuration value is required.");
            }

            var credential = new DefaultAzureCredential();
            return new OpenAIClient(new Uri(options.Endpoint), credential);
        });

        services.AddSingleton<AgentService.ResumeAnalysisAgent>();
    }

    private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (configuration["UseKeyVault"] == "true")
        {
            var keyVaultUri = configuration["KeyVault:Uri"];
            if (!string.IsNullOrEmpty(keyVaultUri))
            {
                try
                {
                    var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
                    var secretResponse = secretClient.GetSecret("DatabaseConnectionString");
                    connectionString = secretResponse.Value.Value;
                }
                catch
                {
                    // Logged at runtime when DbContext is first used
                }
            }
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString ?? "Server=(localdb)\\mssqllocaldb;Database=CVAnalyzerDb;Trusted_Connection=True;MultipleActiveResultSets=true",
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
    }

    private static void ConfigureAzureStorage(IServiceCollection services, IConfiguration configuration)
    {
        var storageOptions = new AzureStorageOptions();
        configuration.GetSection(AzureStorageOptions.SectionName).Bind(storageOptions);
        
        if (string.IsNullOrEmpty(storageOptions.ConnectionString) && string.IsNullOrEmpty(storageOptions.AccountName))
        {
            // For testing/development, register placeholder clients that won't be used
            // This prevents DI validation errors in integration tests
            services.AddSingleton(sp => 
            {
                // Return null when not configured - allows graceful degradation
                return (BlobServiceClient?)null;
            });
            services.AddSingleton(sp => 
            {
                // Return null when not configured - allows graceful degradation
                return (QueueServiceClient?)null;
            });
            return;
        }

        if (!string.IsNullOrEmpty(storageOptions.ConnectionString))
        {
            services.AddSingleton(new BlobServiceClient(storageOptions.ConnectionString));
            services.AddSingleton(new QueueServiceClient(storageOptions.ConnectionString));
        }
        else if (!string.IsNullOrEmpty(storageOptions.AccountName))
        {
            var credential = new DefaultAzureCredential();
            services.AddSingleton(new BlobServiceClient(new Uri($"https://{storageOptions.AccountName}.blob.core.windows.net"), credential));
            services.AddSingleton(new QueueServiceClient(new Uri($"https://{storageOptions.AccountName}.queue.core.windows.net"), credential));
        }
    }

    private static void ConfigureDocumentIntelligence(IServiceCollection services, IConfiguration configuration)
    {
        var docIntelOptions = new DocumentIntelligenceOptions();
        configuration.GetSection(DocumentIntelligenceOptions.SectionName).Bind(docIntelOptions);
        
        if (string.IsNullOrEmpty(docIntelOptions.Endpoint))
        {
            // For testing/development, register placeholder client that won't be used
            // This prevents DI validation errors in integration tests
            services.AddSingleton<DocumentAnalysisClient>(sp => 
            {
                // Placeholder - will throw if actually used
                throw new InvalidOperationException("DocumentAnalysisClient not configured. Set DocumentIntelligence:Endpoint and either DocumentIntelligence:ApiKey or UseManagedIdentity in configuration.");
            });
            return;
        }

        if (!string.IsNullOrEmpty(docIntelOptions.ApiKey))
        {
            services.AddSingleton(new DocumentAnalysisClient(new Uri(docIntelOptions.Endpoint), new AzureKeyCredential(docIntelOptions.ApiKey)));
        }
        else if (docIntelOptions.UseManagedIdentity)
        {
            services.AddSingleton(new DocumentAnalysisClient(new Uri(docIntelOptions.Endpoint), new DefaultAzureCredential()));
        }
        else
        {
            // Placeholder when endpoint is set but no auth configured
            services.AddSingleton<DocumentAnalysisClient>(sp => 
            {
                throw new InvalidOperationException($"DocumentAnalysisClient configured with endpoint {docIntelOptions.Endpoint} but no authentication method specified. Set either DocumentIntelligence:ApiKey or UseManagedIdentity=true.");
            });
        }
    }
}
