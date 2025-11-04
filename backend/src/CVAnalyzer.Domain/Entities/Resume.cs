using CVAnalyzer.Domain.Common;

namespace CVAnalyzer.Domain.Entities;

public class Resume : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string BlobStorageUrl { get; set; } = string.Empty;
    public string OriginalContent { get; set; } = string.Empty;
    public string? OptimizedContent { get; set; }
    public string Status { get; set; } = "Pending";
    public double? Score { get; set; }
    public ICollection<Suggestion> Suggestions { get; set; } = new List<Suggestion>();
}
