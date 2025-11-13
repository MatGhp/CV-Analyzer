using CVAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVAnalyzer.Infrastructure.Persistence.Configurations;

public class ResumeConfiguration : IEntityTypeConfiguration<Resume>
{
    public void Configure(EntityTypeBuilder<Resume> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(r => r.BlobUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasMany(r => r.Suggestions)
            .WithOne(s => s.Resume)
            .HasForeignKey(s => s.ResumeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.CreatedAt);
    }
}
