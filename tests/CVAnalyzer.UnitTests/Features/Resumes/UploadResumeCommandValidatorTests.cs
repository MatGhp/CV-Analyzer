using CVAnalyzer.Application.Features.Resumes.Commands;
using FluentAssertions;
using Xunit;

namespace CVAnalyzer.UnitTests.Features.Resumes;

public class UploadResumeCommandValidatorTests
{
    private readonly UploadResumeCommandValidator _validator;

    public UploadResumeCommandValidatorTests()
    {
        _validator = new UploadResumeCommandValidator();
    }

    [Fact]
    public void Should_Have_Error_When_UserId_Is_Empty()
    {
        var command = new UploadResumeCommand(string.Empty, "test.pdf", new MemoryStream());
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Is_Empty()
    {
        var command = new UploadResumeCommand("user123", string.Empty, new MemoryStream());
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new UploadResumeCommand("user123", "test.pdf", new MemoryStream());
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
