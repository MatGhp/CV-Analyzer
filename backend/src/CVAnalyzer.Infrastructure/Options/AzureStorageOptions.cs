namespace CVAnalyzer.Infrastructure.Options;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    public bool UseManagedIdentity { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }
    public string BlobEndpoint { get; set; } = string.Empty;
    public string QueueEndpoint { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "resumes";
    public string QueueName { get; set; } = "resume-analysis";
    public string PoisonQueueName { get; set; } = "resume-analysis-poison";
    
    public int SasExpirationHours { get; set; } = 24;
    public int SasClockSkewMinutes { get; set; } = 5;
}
