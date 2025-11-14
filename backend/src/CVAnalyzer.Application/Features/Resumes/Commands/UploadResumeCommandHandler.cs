using CVAnalyzer.Application.Common.Constants;
using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Domain.Entities;
using CVAnalyzer.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Application.Features.Resumes.Commands;

public class UploadResumeCommandHandler : IRequestHandler<UploadResumeCommand, UploadResumeResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly IResumeQueueService _queueService;
    private readonly ILogger<UploadResumeCommandHandler> _logger;

    public UploadResumeCommandHandler(
        IApplicationDbContext context,
        IBlobStorageService blobStorage,
        IResumeQueueService queueService,
        ILogger<UploadResumeCommandHandler> logger)
    {
        _context = context;
        _blobStorage = blobStorage;
        _queueService = queueService;
        _logger = logger;
    }

    public async Task<UploadResumeResponse> Handle(UploadResumeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Uploading resume {FileName} for user {UserId}", request.FileName, request.UserId);

        var blobUrl = await _blobStorage.UploadFileAsync(request.FileStream, request.FileName, cancellationToken);

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FileName = request.FileName,
            BlobUrl = blobUrl,
            Content = string.Empty,
            Status = ResumeStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow.UtcDateTime
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync(cancellationToken);

        await _queueService.EnqueueResumeAnalysisAsync(resume.Id, request.UserId);

        _logger.LogInformation("Resume {ResumeId} uploaded successfully. Analysis message enqueued.", resume.Id);

        return new UploadResumeResponse(
            resume.Id,
            ResumeStatusConstants.Pending,
            "Resume uploaded successfully. Analysis in progress.");
    }
}
