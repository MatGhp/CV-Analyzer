using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Domain.Entities;
using MediatR;

namespace CVAnalyzer.Application.Features.Resumes.Commands;

public class UploadResumeCommandHandler : IRequestHandler<UploadResumeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public UploadResumeCommandHandler(IApplicationDbContext context, IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    public async Task<Guid> Handle(UploadResumeCommand request, CancellationToken cancellationToken)
    {
        var blobUrl = await _blobStorage.UploadAsync(request.FileStream, request.FileName, cancellationToken);

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FileName = request.FileName,
            BlobStorageUrl = blobUrl,
            OriginalContent = string.Empty,
            Status = "Uploaded",
            CreatedAt = DateTime.UtcNow
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync(cancellationToken);

        return resume.Id;
    }
}
