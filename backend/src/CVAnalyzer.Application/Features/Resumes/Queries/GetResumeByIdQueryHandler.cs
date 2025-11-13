using CVAnalyzer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CVAnalyzer.Application.Features.Resumes.Queries;

public class GetResumeByIdQueryHandler : IRequestHandler<GetResumeByIdQuery, ResumeDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorageService;

    public GetResumeByIdQueryHandler(
        IApplicationDbContext context,
        IBlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    public async Task<ResumeDto?> Handle(GetResumeByIdQuery request, CancellationToken cancellationToken)
    {
        var resume = await _context.Resumes
            .Include(r => r.Suggestions)
            .Include(r => r.CandidateInfo)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (resume == null)
            return null;

        var sasToken = await _blobStorageService.GenerateSasTokenAsync(resume.BlobUrl, cancellationToken);

        return new ResumeDto
        {
            Id = resume.Id,
            UserId = resume.UserId,
            FileName = resume.FileName,
            BlobUrl = resume.BlobUrl,
            BlobUrlWithSas = sasToken,
            Content = resume.Content,
            OptimizedContent = resume.OptimizedContent,
            Status = (int)resume.Status,
            Score = resume.Score,
            CreatedAt = resume.CreatedAt,
            UpdatedAt = resume.UpdatedAt,
            AnalyzedAt = resume.AnalyzedAt,
            CandidateInfo = resume.CandidateInfo,
            Suggestions = resume.Suggestions.ToList()
        };
    }
}
