using CVAnalyzer.Domain.Entities;

namespace CVAnalyzer.Application.Features.Resumes.Queries;

public class ResumeDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string BlobUrlWithSas { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? OptimizedContent { get; set; }
    public int Status { get; set; }
    public int? Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? AnalyzedAt { get; set; }
    public CandidateInfo? CandidateInfo { get; set; }
    public List<Suggestion> Suggestions { get; set; } = new();
}
