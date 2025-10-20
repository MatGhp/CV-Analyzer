using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Infrastructure.Persistence;
using CVAnalyzer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
                var secretResponse = secretClient.GetSecret("DatabaseConnectionString");
                connectionString = secretResponse.Value.Value;
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
