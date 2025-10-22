using CVAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CVAnalyzer.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Resume> Resumes { get; }
    DbSet<Suggestion> Suggestions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
