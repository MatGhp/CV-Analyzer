using CVAnalyzer.Domain.Common;
using CVAnalyzer.Domain.Enums;

namespace CVAnalyzer.Domain.Entities;

public class Resume : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? OptimizedContent { get; set; }
    public ResumeStatus Status { get; set; } = ResumeStatus.Pending;
    public int? Score { get; set; }
    public DateTime? AnalyzedAt { get; set; }
    public bool IsAnonymous { get; set; } = false;
    public DateTime? AnonymousExpiresAt { get; set; }
    
    public CandidateInfo? CandidateInfo { get; set; }
    public ICollection<Suggestion> Suggestions { get; set; } = new List<Suggestion>();
}
