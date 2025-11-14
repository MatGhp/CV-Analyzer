using MediatR;

namespace CVAnalyzer.Application.Features.Resumes.Queries;

public record GetResumeStatusQuery(Guid ResumeId) : IRequest<ResumeStatusResponse>;

public record ResumeStatusResponse(Guid ResumeId, string Status, int Progress, string? ErrorMessage);
