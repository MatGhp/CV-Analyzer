using CVAnalyzer.Domain.Entities;
using MediatR;

namespace CVAnalyzer.Application.Features.Resumes.Queries;

public record GetResumeByIdQuery(Guid Id) : IRequest<Resume?>;
