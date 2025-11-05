using System.ComponentModel.DataAnnotations;

namespace CVAnalyzer.AgentService.Models;

public sealed class ResumeAnalysisRequest
{
    [Required]
    [MinLength(10)]
    [MaxLength(10000)]
    public string Content { get; init; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string UserId { get; init; } = string.Empty;
}
