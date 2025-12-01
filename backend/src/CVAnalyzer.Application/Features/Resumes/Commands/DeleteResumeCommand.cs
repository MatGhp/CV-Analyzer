using MediatR;

namespace CVAnalyzer.Application.Features.Resumes.Commands;

/// <summary>
/// Command to delete a resume and its associated data.
/// </summary>
/// <param name="ResumeId">The ID of the resume to delete.</param>
/// <param name="RequestingUserId">The ID of the user requesting the deletion (for authorization).</param>
public record DeleteResumeCommand(Guid ResumeId, string RequestingUserId) : IRequest<bool>;
