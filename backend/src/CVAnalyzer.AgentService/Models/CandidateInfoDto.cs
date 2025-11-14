namespace CVAnalyzer.AgentService.Models;

public sealed class CandidateInfoDto
{
    public string FullName { get; init; } = string.Empty;
    
    public string Email { get; init; } = string.Empty;
    
    public string? Phone { get; init; }
    
    public string? Location { get; init; }
    
    public string Skills { get; init; } = string.Empty;
    
    public int? YearsOfExperience { get; init; }
    
    public string? CurrentJobTitle { get; init; }
    
    public string? Education { get; init; }
}
