using CVAnalyzer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Application.Features.Resumes.Commands;

public class DeleteResumeCommandHandler : IRequestHandler<DeleteResumeCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<DeleteResumeCommandHandler> _logger;

    public DeleteResumeCommandHandler(
        IApplicationDbContext context, 
        IBlobStorageService blobStorage,
        ILogger<DeleteResumeCommandHandler> logger)
    {
        _context = context;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteResumeCommand request, CancellationToken cancellationToken)
    {
        var resume = await _context.Resumes
            .Include(r => r.Suggestions)
            .Include(r => r.CandidateInfo)
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId, cancellationToken);

        if (resume == null)
        {
            _logger.LogWarning("Resume {ResumeId} not found for deletion", request.ResumeId);
            return false;
        }

        // Authorization: Verify the requesting user owns this resume
        if (resume.UserId != request.RequestingUserId)
        {
            _logger.LogWarning(
                "User {RequestingUserId} attempted to delete resume {ResumeId} owned by {OwnerId}",
                request.RequestingUserId, request.ResumeId, resume.UserId);
            return false;
        }

        // Delete blob from storage
        if (!string.IsNullOrEmpty(resume.BlobUrl))
        {
            try
            {
                await _blobStorage.DeleteFileAsync(resume.BlobUrl, cancellationToken);
                _logger.LogDebug("Deleted blob for resume {ResumeId}: {BlobUrl}", request.ResumeId, resume.BlobUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete blob for resume {ResumeId}: {BlobUrl}", 
                    request.ResumeId, resume.BlobUrl);
                // Continue with database deletion even if blob deletion fails
            }
        }

        // Delete candidate info if exists
        if (resume.CandidateInfo != null)
        {
            _context.CandidateInfos.Remove(resume.CandidateInfo);
        }

        // Delete suggestions
        if (resume.Suggestions.Any())
        {
            _context.Suggestions.RemoveRange(resume.Suggestions);
        }

        // Delete resume
        _context.Resumes.Remove(resume);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Resume {ResumeId} deleted successfully by user {UserId}", 
            request.ResumeId, request.RequestingUserId);

        return true;
    }
}
