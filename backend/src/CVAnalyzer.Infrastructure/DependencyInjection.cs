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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CVAnalyzer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureStorageOptions>(options => configuration.GetSection(AzureStorageOptions.SectionName).Bind(options));
        services.Configure<DocumentIntelligenceOptions>(options => configuration.GetSection(DocumentIntelligenceOptions.SectionName).Bind(options));

        ConfigureDatabase(services, configuration);
        ConfigureAzureStorage(services, configuration);
        ConfigureDocumentIntelligence(services, configuration);
        
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();
        services.AddScoped<IAIResumeAnalyzerService, AIResumeAnalyzerService>();

        return services;
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
            return;

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
        
        if (string.IsNullOrEmpty(docIntelOptions.Endpoint)) return;

        if (!string.IsNullOrEmpty(docIntelOptions.ApiKey))
        {
            services.AddSingleton(new DocumentAnalysisClient(new Uri(docIntelOptions.Endpoint), new AzureKeyCredential(docIntelOptions.ApiKey)));
        }
        else if (docIntelOptions.UseManagedIdentity)
        {
            services.AddSingleton(new DocumentAnalysisClient(new Uri(docIntelOptions.Endpoint), new DefaultAzureCredential()));
        }
    }
}
