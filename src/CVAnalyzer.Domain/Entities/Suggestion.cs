using CVAnalyzer.Domain.Common;

namespace CVAnalyzer.Domain.Entities;

public class Suggestion : BaseEntity
{
    public Guid ResumeId { get; set; }
    public Resume Resume { get; set; } = null!;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
}
