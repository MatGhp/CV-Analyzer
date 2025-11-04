using CVAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CVAnalyzer.Infrastructure.Persistence.Configurations;

public class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
    public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(s => s.Priority)
            .IsRequired();

        builder.HasIndex(s => s.ResumeId);
    }
}
