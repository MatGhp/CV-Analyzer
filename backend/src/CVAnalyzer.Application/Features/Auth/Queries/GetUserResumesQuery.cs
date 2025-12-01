using MediatR;

namespace CVAnalyzer.Application.Features.Auth.Queries;

/// <summary>
/// Query to retrieve all resumes for an authenticated user.
/// </summary>
public record GetUserResumesQuery(
    Guid UserId
) : IRequest<List<UserResumeListItem>>;

/// <summary>
/// Resume list item for user dashboard.
/// </summary>
public record UserResumeListItem(
    Guid ResumeId,
    string FileName,
    decimal? Score,
    string Status,
    DateTime UploadedAt,
    DateTime? AnalyzedAt,
    string? BlobUrl
);
