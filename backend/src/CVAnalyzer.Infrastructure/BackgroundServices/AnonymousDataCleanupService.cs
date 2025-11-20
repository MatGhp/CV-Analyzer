using CVAnalyzer.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up expired anonymous user data
/// Runs every hour and deletes resumes that have passed their expiration date
/// </summary>
public class AnonymousDataCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnonymousDataCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public AnonymousDataCleanupService(
        IServiceProvider serviceProvider,
        ILogger<AnonymousDataCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Anonymous Data Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredDataAsync(stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Anonymous Data Cleanup Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during anonymous data cleanup");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry after 5 minutes on error
            }
        }
    }

    private async Task CleanupExpiredDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();

        var cutoffDate = DateTime.UtcNow;
        int totalCleaned = 0;
        int totalFailed = 0;
        const int batchSize = 100;

        while (true)
        {
            // Find up to batchSize expired anonymous resumes (batch processing for memory efficiency)
            var expiredResumes = await context.Resumes
                .Where(r => r.IsAnonymous && r.AnonymousExpiresAt.HasValue && r.AnonymousExpiresAt.Value < cutoffDate)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (expiredResumes.Count == 0)
            {
                if (totalCleaned == 0)
                {
                    _logger.LogInformation("No expired anonymous resumes to clean up");
                }
                break;
            }

            _logger.LogInformation("Processing batch of {Count} expired anonymous resumes", expiredResumes.Count);

            int batchSuccessCount = 0;

            foreach (var resume in expiredResumes)
            {
                try
                {
                    // Delete blob file first
                    await blobStorage.DeleteFileAsync(resume.BlobUrl, cancellationToken);
                    
                    // Delete resume record (cascade deletes suggestions and candidate info)
                    context.Resumes.Remove(resume);
                    
                    batchSuccessCount++;
                    
                    _logger.LogInformation(
                        "Marked resume {ResumeId} for deletion (anonymous user {UserId})", 
                        resume.Id, 
                        resume.UserId);
                }
                catch (Exception ex)
                {
                    totalFailed++;
                    _logger.LogError(
                        ex, 
                        "Failed to clean up resume {ResumeId} for anonymous user {UserId}", 
                        resume.Id, 
                        resume.UserId);
                    
                    // Don't remove from context if blob deletion failed (maintains consistency)
                    if (context.Resumes.Local.Contains(resume))
                    {
                        context.Resumes.Attach(resume);
                    }
                }
            }

            // Save changes for this batch (transactional consistency)
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                totalCleaned += batchSuccessCount;
                
                _logger.LogInformation(
                    "Batch cleanup completed. Successfully cleaned {SuccessCount} resumes in this batch",
                    batchSuccessCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save batch cleanup changes");
                totalFailed += batchSuccessCount;
            }
        }

        if (totalCleaned > 0 || totalFailed > 0)
        {
            _logger.LogInformation(
                "Anonymous data cleanup completed. Total cleaned: {Cleaned}, Total failed: {Failed}",
                totalCleaned,
                totalFailed);
        }
    }
}
