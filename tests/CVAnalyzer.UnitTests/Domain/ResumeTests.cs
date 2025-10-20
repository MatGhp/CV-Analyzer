using CVAnalyzer.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CVAnalyzer.UnitTests.Domain;

public class ResumeTests
{
    [Fact]
    public void Resume_Should_Be_Created_With_Default_Values()
    {
        var resume = new Resume();
        resume.Status.Should().Be("Pending");
        resume.Suggestions.Should().NotBeNull();
        resume.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public void Resume_Should_Accept_Properties()
    {
        var resumeId = Guid.NewGuid();
        var resume = new Resume
        {
            Id = resumeId,
            UserId = "user123",
            FileName = "test.pdf",
            BlobStorageUrl = "https://example.com/test.pdf",
            OriginalContent = "Test content",
            Status = "Uploaded"
        };

        resume.Id.Should().Be(resumeId);
        resume.UserId.Should().Be("user123");
        resume.FileName.Should().Be("test.pdf");
        resume.Status.Should().Be("Uploaded");
    }
}
