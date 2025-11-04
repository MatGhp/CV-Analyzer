using MediatR;

namespace CVAnalyzer.Application.Features.Resumes.Commands;

public record UploadResumeCommand(string UserId, string FileName, Stream FileStream) : IRequest<Guid>;
