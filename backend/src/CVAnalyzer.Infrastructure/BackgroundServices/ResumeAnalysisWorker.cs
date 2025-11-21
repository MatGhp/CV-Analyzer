using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CVAnalyzer.AgentService;
using CVAnalyzer.Application.Common.Exceptions;
using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Domain.Enums;
using CVAnalyzer.Infrastructure.Options;
using CVAnalyzer.Infrastructure.Persistence;
using CVAnalyzer.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CVAnalyzer.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes resume analysis requests from Azure Storage Queue
/// </summary>
public class ResumeAnalysisWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly QueueClient _queueClient;
    private readonly QueueClient _poisonQueueClient;
    private readonly QueueOptions _queueOptions;
    private readonly ILogger<ResumeAnalysisWorker> _logger;

    public ResumeAnalysisWorker(
        IServiceProvider serviceProvider,
        QueueServiceClient? queueServiceClient,
        IOptions<QueueOptions> queueOptions,
        ILogger<ResumeAnalysisWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _queueOptions = queueOptions.Value;
        _logger = logger;
        
        // Handle null QueueServiceClient (e.g., in test environments)
        if (queueServiceClient != null)
        {
            _queueClient = queueServiceClient.GetQueueClient(_queueOptions.ResumeAnalysisQueueName);
            _poisonQueueClient = queueServiceClient.GetQueueClient(_queueOptions.PoisonQueueName);
        }
        else
        {
            _logger.LogWarning("QueueServiceClient not configured. ResumeAnalysisWorker will not process messages.");
            _queueClient = null!;
            _poisonQueueClient = null!;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ResumeAnalysisWorker started");

        // If queue client is not configured, exit early (test environment)
        if (_queueClient == null)
        {
            _logger.LogInformation("ResumeAnalysisWorker exiting - no queue client configured");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _queueClient.ReceiveMessagesAsync(
                    maxMessages: _queueOptions.MaxMessagesPerBatch,
                    visibilityTimeout: TimeSpan.FromMinutes(_queueOptions.VisibilityTimeoutMinutes),
                    cancellationToken: stoppingToken);

                if (response.Value?.Length > 0)
                {
                    _logger.LogInformation(
                        "Received {Count} messages from queue", 
                        response.Value.Length);
                    
                    foreach (var message in response.Value)
                    {
                        await ProcessMessageAsync(message, stoppingToken);
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(_queueOptions.PollingIntervalSeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break; // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in message processing loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("ResumeAnalysisWorker stopped");
    }

    private async Task ProcessMessageAsync(
        QueueMessage message, 
        CancellationToken cancellationToken)
    {
        ResumeAnalysisMessage? analysisMessage = null;
        
        try
        {
            analysisMessage = JsonSerializer.Deserialize<ResumeAnalysisMessage>(
                message.MessageText)
                ?? throw new InvalidOperationException("Invalid message format");

            _logger.LogInformation(
                "Processing resume {ResumeId} (Attempt {DequeueCount}/{MaxRetries})",
                analysisMessage.ResumeId, message.DequeueCount, _queueOptions.MaxDequeueCount);

            // Check max retries
            if (message.DequeueCount >= _queueOptions.MaxDequeueCount)
            {
                _logger.LogError(
                    "Resume {ResumeId} exceeded max retries. Moving to poison queue.",
                    analysisMessage.ResumeId);
                
                await HandleFailedMessageAsync(analysisMessage, "Max retries exceeded");
                await _queueClient.DeleteMessageAsync(
                    message.MessageId, 
                    message.PopReceipt,
                    cancellationToken);
                return;
            }

            // Process with scoped services
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<ResumeAnalysisOrchestrator>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
            var documentIntelligence = scope.ServiceProvider.GetRequiredService<IDocumentIntelligenceService>();
            var agent = scope.ServiceProvider.GetRequiredService<ResumeAnalysisAgent>();

            await orchestrator.ProcessResumeAsync(
                analysisMessage.ResumeId,
                analysisMessage.UserId,
                context,
                blobStorage,
                documentIntelligence,
                agent,
                cancellationToken);

            // Success - delete message
            await _queueClient.DeleteMessageAsync(
                message.MessageId, 
                message.PopReceipt, 
                cancellationToken);

            _logger.LogInformation(
                "Successfully processed resume {ResumeId}",
                analysisMessage.ResumeId);
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Resume not found for message {MessageId}. Moving to poison queue.", message.MessageId);
            if (analysisMessage != null)
            {
                await HandleFailedMessageAsync(analysisMessage, $"Resume not found: {ex.Message}");
            }
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid message format {MessageId}. Moving to poison queue.", message.MessageId);
            if (analysisMessage != null)
            {
                await HandleFailedMessageAsync(analysisMessage, $"Invalid JSON: {ex.Message}");
            }
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId} (will retry)", message.MessageId);
            // Don't delete - message will become visible again after timeout
        }
    }

    private async Task HandleFailedMessageAsync(
        ResumeAnalysisMessage message, 
        string errorReason)
    {
        // Move to poison queue for manual review
        var poisonMessage = JsonSerializer.Serialize(new
        {
            message.ResumeId,
            message.UserId,
            ErrorReason = errorReason,
            FailedAt = DateTimeOffset.UtcNow
        });
        
        await _poisonQueueClient.SendMessageAsync(poisonMessage);
        
        // Update resume status to failed
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        
        var resume = await context.Resumes.FindAsync(message.ResumeId);
        if (resume != null)
        {
            resume.Status = ResumeStatus.Failed;
            await context.SaveChangesAsync();
        }
    }
}
