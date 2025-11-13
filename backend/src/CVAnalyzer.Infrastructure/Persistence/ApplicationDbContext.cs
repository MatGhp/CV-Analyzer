using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CVAnalyzer.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<Suggestion> Suggestions => Set<Suggestion>();
    public DbSet<CandidateInfo> CandidateInfos => Set<CandidateInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        modelBuilder.Entity<CandidateInfo>(entity =>
        {
            var skillsComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (hash, skill) => HashCode.Combine(hash, skill)),
                c => c.ToList());

            entity.Property(e => e.Skills)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(skillsComparer);

            entity.HasOne(e => e.Resume)
                .WithOne(r => r.CandidateInfo)
                .HasForeignKey<CandidateInfo>(e => e.ResumeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ResumeId).IsUnique();
        });

        modelBuilder.Entity<Resume>(entity =>
        {
            entity.HasIndex(e => e.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<Domain.Common.BaseEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
