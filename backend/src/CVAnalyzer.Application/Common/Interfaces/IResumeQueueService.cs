namespace CVAnalyzer.Application.Common.Interfaces;

/// <summary>
/// Service for enqueueing resume analysis requests to Azure Storage Queue
/// </summary>
public interface IResumeQueueService
{
    /// <summary>
    /// Enqueues a resume for asynchronous analysis
    /// </summary>
    /// <param name="resumeId">ID of the resume to analyze</param>
    /// <param name="userId">ID of the user who uploaded the resume</param>
    Task EnqueueResumeAnalysisAsync(Guid resumeId, string userId);
}
