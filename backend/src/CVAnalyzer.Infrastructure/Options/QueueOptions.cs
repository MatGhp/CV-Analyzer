namespace CVAnalyzer.Infrastructure.Options;

public class QueueOptions
{
    public const string SectionName = "Queue";

    public string ResumeAnalysisQueueName { get; set; } = "resume-analysis";
    
    public string PoisonQueueName { get; set; } = "resume-analysis-poison";
    
    public int MaxDequeueCount { get; set; } = 5;
    
    public int PollingIntervalSeconds { get; set; } = 2;
    
    public int VisibilityTimeoutMinutes { get; set; } = 5;
    
    public int MaxMessagesPerBatch { get; set; } = 10;
}
