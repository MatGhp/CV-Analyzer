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
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();

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

        modelBuilder.Entity<PromptTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AgentType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.TaskType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Environment)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .IsRequired();

            // Unique constraint: One version per AgentType/TaskType/Environment/Version
            entity.HasIndex(e => new { e.AgentType, e.TaskType, e.Environment, e.Version })
                .IsUnique()
                .HasDatabaseName("IX_PromptTemplates_AgentTask_Version");

            // Performance index for active prompt lookups (IsActive in filter, not columns)
            entity.HasIndex(e => new { e.Environment, e.AgentType, e.TaskType })
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("IX_PromptTemplates_Active_Lookup");

            // Check constraint for valid environments (using updated API)
            entity.ToTable(tb => tb.HasCheckConstraint(
                "CK_PromptTemplates_Environment",
                "[Environment] IN ('Development', 'Test', 'Production')"));
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
