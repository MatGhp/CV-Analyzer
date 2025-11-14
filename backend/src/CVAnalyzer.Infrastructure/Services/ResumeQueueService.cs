using Azure.Storage.Queues;
using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CVAnalyzer.Infrastructure.Services;

/// <summary>
/// Message format for resume analysis queue
/// </summary>
public record ResumeAnalysisMessage(Guid ResumeId, string UserId);

/// <summary>
/// Azure Storage Queue implementation for resume analysis enqueueing
/// </summary>
public class ResumeQueueService : IResumeQueueService
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<ResumeQueueService> _logger;

    public ResumeQueueService(
        QueueServiceClient queueServiceClient,
        IOptions<QueueOptions> queueOptions,
        ILogger<ResumeQueueService> logger)
    {
        _queueClient = queueServiceClient.GetQueueClient(queueOptions.Value.ResumeAnalysisQueueName);
        _logger = logger;
    }

    public async Task EnqueueResumeAnalysisAsync(Guid resumeId, string userId)
    {
        var message = new ResumeAnalysisMessage(resumeId, userId);
        var messageText = JsonSerializer.Serialize(message);
        
        await _queueClient.SendMessageAsync(messageText);
        
        _logger.LogInformation(
            "Enqueued resume analysis for {ResumeId} (User: {UserId})", 
            resumeId, userId);
    }
}
