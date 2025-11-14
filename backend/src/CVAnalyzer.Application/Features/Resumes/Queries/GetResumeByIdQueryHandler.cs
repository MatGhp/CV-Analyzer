using CVAnalyzer.Application.Common.Extensions;
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
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (resume == null)
            return null;

        var sasToken = await _blobStorageService.GenerateSasTokenAsync(resume.BlobUrl, cancellationToken);

        var candidateInfoDto = resume.CandidateInfo != null
            ? new CandidateInfoDto(
                resume.CandidateInfo.FullName,
                resume.CandidateInfo.Email,
                resume.CandidateInfo.Phone,
                resume.CandidateInfo.Location,
                resume.CandidateInfo.Skills,
                resume.CandidateInfo.YearsOfExperience,
                resume.CandidateInfo.CurrentJobTitle,
                resume.CandidateInfo.Education)
            : null;

        var suggestions = resume.Suggestions
            .Select(s => new SuggestionDto(s.Category, s.Description, s.Priority))
            .ToList();

        return new ResumeDto(
            resume.Id,
            resume.UserId,
            resume.FileName,
            resume.BlobUrl,
            sasToken,
            resume.Content,
            resume.OptimizedContent,
            resume.Status.ToStatusString(),
            resume.Score,
            resume.CreatedAt,
            resume.UpdatedAt,
            resume.AnalyzedAt,
            candidateInfoDto,
            suggestions);
    }
}
