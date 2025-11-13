using CVAnalyzer.Domain.Common;

namespace CVAnalyzer.Domain.Entities;

public class CandidateInfo : BaseEntity
{
    public Guid ResumeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public List<string> Skills { get; set; } = new();
    public int? YearsOfExperience { get; set; }
    public string? CurrentJobTitle { get; set; }
    public string? Education { get; set; }
    
    public Resume Resume { get; set; } = null!;
}
