namespace CVAnalyzer.Infrastructure.Options;

public class DocumentIntelligenceOptions
{
    public const string SectionName = "DocumentIntelligence";

    public string Endpoint { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool UseManagedIdentity { get; set; }
    public string ModelId { get; set; } = "prebuilt-read";
}
