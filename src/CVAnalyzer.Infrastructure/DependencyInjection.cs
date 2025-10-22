using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Infrastructure.Persistence;
using CVAnalyzer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
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
                catch (Exception ex)
                {
                    var logger = services.BuildServiceProvider().GetService<ILogger<SecretClient>>();
                    logger?.LogError(ex, "Failed to retrieve connection string from Key Vault. Falling back to configuration.");
                }
            }
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString ?? "Server=(localdb)\\mssqllocaldb;Database=CVAnalyzerDb;Trusted_Connection=True;MultipleActiveResultSets=true",
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IAIResumeAnalyzerService, AIResumeAnalyzerService>();

        return services;
    }
}
