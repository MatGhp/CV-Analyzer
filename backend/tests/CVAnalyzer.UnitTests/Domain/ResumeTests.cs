using CVAnalyzer.Domain.Entities;
using CVAnalyzer.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CVAnalyzer.UnitTests.Domain;

public class ResumeTests
{
    [Fact]
    public void Resume_Should_Be_Created_With_Default_Values()
    {
        var resume = new Resume();
        resume.Status.Should().Be(ResumeStatus.Pending);
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
            BlobUrl = "https://example.com/test.pdf",
            Content = "Test content",
            Status = ResumeStatus.Pending
        };

        resume.Id.Should().Be(resumeId);
        resume.UserId.Should().Be("user123");
        resume.FileName.Should().Be("test.pdf");
        resume.Status.Should().Be(ResumeStatus.Pending);
    }
}
