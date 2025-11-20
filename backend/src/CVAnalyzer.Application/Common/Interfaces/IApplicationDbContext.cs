using CVAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CVAnalyzer.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Resume> Resumes { get; }
    DbSet<Suggestion> Suggestions { get; }
    DbSet<CandidateInfo> CandidateInfos { get; }
    DbSet<PromptTemplate> PromptTemplates { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
