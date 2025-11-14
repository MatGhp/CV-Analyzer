using CVAnalyzer.Application.Common.Constants;
using CVAnalyzer.Application.Common.Exceptions;
using CVAnalyzer.Application.Common.Extensions;
using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Application.Features.Resumes.Queries;

public class GetResumeStatusQueryHandler : IRequestHandler<GetResumeStatusQuery, ResumeStatusResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetResumeStatusQueryHandler> _logger;

    public GetResumeStatusQueryHandler(
        IApplicationDbContext context,
        ILogger<GetResumeStatusQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResumeStatusResponse> Handle(
        GetResumeStatusQuery request,
        CancellationToken cancellationToken)
    {
        var resume = await _context.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resume), request.ResumeId);

        var status = resume.Status.ToStatusString();
        var progress = resume.Status.GetProgress();
        var errorMessage = resume.Status == Domain.Enums.ResumeStatus.Failed
            ? ErrorMessages.AnalysisFailed
            : null;

        _logger.LogInformation("Status check for resume {ResumeId}: {Status} ({Progress}%)", resume.Id, status, progress);

        return new ResumeStatusResponse(resume.Id, status, progress, errorMessage);
    }
}
