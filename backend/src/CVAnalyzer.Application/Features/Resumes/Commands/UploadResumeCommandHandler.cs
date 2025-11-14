using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Domain.Entities;
using MediatR;

namespace CVAnalyzer.Application.Features.Resumes.Commands;

public class UploadResumeCommandHandler : IRequestHandler<UploadResumeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly IResumeQueueService _queueService;

    public UploadResumeCommandHandler(
        IApplicationDbContext context, 
        IBlobStorageService blobStorage,
        IResumeQueueService queueService)
    {
        _context = context;
        _blobStorage = blobStorage;
        _queueService = queueService;
    }

    public async Task<Guid> Handle(UploadResumeCommand request, CancellationToken cancellationToken)
    {
        var blobUrl = await _blobStorage.UploadFileAsync(request.FileStream, request.FileName, cancellationToken);

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FileName = request.FileName,
            BlobUrl = blobUrl,
            Content = string.Empty,
            Status = Domain.Enums.ResumeStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow.UtcDateTime
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync(cancellationToken);

        // Enqueue for async analysis
        await _queueService.EnqueueResumeAnalysisAsync(resume.Id, request.UserId);

        return resume.Id;
    }
}
