using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CVAnalyzer.Application.Features.Resumes.Queries;

public class GetResumeByIdQueryHandler : IRequestHandler<GetResumeByIdQuery, Resume?>
{
    private readonly IApplicationDbContext _context;

    public GetResumeByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Resume?> Handle(GetResumeByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Resumes
            .Include(r => r.Suggestions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
    }
}
