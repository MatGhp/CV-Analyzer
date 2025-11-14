namespace CVAnalyzer.Application.Features.Resumes.Queries;

public record ResumeDto(
    Guid Id,
    string UserId,
    string FileName,
    string BlobUrl,
    string BlobUrlWithSas,
    string Content,
    string? OptimizedContent,
    string Status,
    int? Score,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? AnalyzedAt,
    CandidateInfoDto? CandidateInfo,
    List<SuggestionDto> Suggestions);

public record CandidateInfoDto(
    string FullName,
    string Email,
    string? Phone,
    string? Location,
    List<string> Skills,
    int? YearsOfExperience,
    string? CurrentJobTitle,
    string? Education);

public record SuggestionDto(
    string Category,
    string Description,
    int Priority);
