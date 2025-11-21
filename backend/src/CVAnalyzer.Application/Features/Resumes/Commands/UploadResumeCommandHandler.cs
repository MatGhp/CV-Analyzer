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
    private readonly ISessionTokenService _sessionTokenService;
    private readonly ILogger<UploadResumeCommandHandler> _logger;

    public UploadResumeCommandHandler(
        IApplicationDbContext context,
        IBlobStorageService blobStorage,
        IResumeQueueService queueService,
        ISessionTokenService sessionTokenService,
        ILogger<UploadResumeCommandHandler> logger)
    {
        _context = context;
        _blobStorage = blobStorage;
        _queueService = queueService;
        _sessionTokenService = sessionTokenService;
        _logger = logger;
    }

    public async Task<UploadResumeResponse> Handle(UploadResumeCommand request, CancellationToken cancellationToken)
    {
        var isAnonymous = _sessionTokenService.IsAnonymousUser(request.UserId);
        
        _logger.LogInformation(
            "Uploading resume {FileName} for {UserType} user {UserId}", 
            request.FileName, 
            isAnonymous ? "anonymous" : "authenticated",
            request.UserId);

        var blobUrl = await _blobStorage.UploadFileAsync(request.FileStream, request.FileName, cancellationToken);

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FileName = request.FileName,
            BlobUrl = blobUrl,
            Content = string.Empty,
            Status = ResumeStatus.Pending,
            IsAnonymous = isAnonymous,
            AnonymousExpiresAt = isAnonymous ? DateTime.UtcNow.AddHours(24) : null,
            CreatedAt = DateTimeOffset.UtcNow.UtcDateTime
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync(cancellationToken);

        await _queueService.EnqueueResumeAnalysisAsync(resume.Id, request.UserId);

        _logger.LogInformation(
            "Resume {ResumeId} uploaded successfully. Analysis message enqueued. {ExpiryInfo}", 
            resume.Id,
            isAnonymous ? $"Expires at {resume.AnonymousExpiresAt:u}" : "No expiry");

        return new UploadResumeResponse(
            resume.Id,
            ResumeStatusConstants.Pending,
            "Resume uploaded successfully. Analysis in progress.");
    }
}
