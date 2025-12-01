using CVAnalyzer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CVAnalyzer.Application.Features.Auth.Queries;

/// <summary>
/// Handler for retrieving user's resumes.
/// </summary>
public class GetUserResumesQueryHandler : IRequestHandler<GetUserResumesQuery, List<UserResumeListItem>>
{
    private readonly IApplicationDbContext _context;

    public GetUserResumesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserResumeListItem>> Handle(GetUserResumesQuery request, CancellationToken cancellationToken)
    {
        var resumes = await _context.Resumes
            .Where(r => r.AuthenticatedUserId == request.UserId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new UserResumeListItem(
                ResumeId: r.Id,
                FileName: r.FileName,
                Score: r.Score,
                Status: r.Status.ToString(),
                UploadedAt: r.CreatedAt,
                AnalyzedAt: r.AnalyzedAt,
                BlobUrl: r.BlobUrl
            ))
            .ToListAsync(cancellationToken);

        return resumes;
    }
}
