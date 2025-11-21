using CVAnalyzer.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CVAnalyzer.UnitTests.Infrastructure;

public class SessionTokenServiceTests
{
    private readonly SessionTokenService _service = new();

    [Fact]
    public void GenerateSessionToken_Should_Return_Valid_Format()
    {
        // Act
        var token = _service.GenerateSessionToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().StartWith("guest-");
        
        var parts = token.Split('-');
        parts.Should().HaveCount(3);
        parts[0].Should().Be("guest");
        long.TryParse(parts[1], out _).Should().BeTrue("timestamp should be numeric");
        parts[2].Should().HaveLength(12, "random part should be 12 characters");
    }

    [Fact]
    public void GenerateSessionToken_Should_Return_Unique_Tokens()
    {
        // Act
        var token1 = _service.GenerateSessionToken();
        var token2 = _service.GenerateSessionToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Theory]
    [InlineData("guest-1700000000000-abc123def456", true)]
    [InlineData("guest-123-randomstring", true)]
    [InlineData("user-1700000000000-abc123def456", false)]
    [InlineData("guest-notanumber-abc123def456", false)]
    [InlineData("guest-1700000000000", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidSessionToken_Should_Validate_Correctly(string? token, bool expectedValid)
    {
        // Act
        var result = _service.IsValidSessionToken(token!);

        // Assert
        result.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("guest-1700000000000-abc123def456", true)]
    [InlineData("user-123", false)]
    [InlineData("authenticated-user-id", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAnonymousUser_Should_Detect_Guest_Prefix(string? userId, bool expectedAnonymous)
    {
        // Act
        var result = _service.IsAnonymousUser(userId!);

        // Assert
        result.Should().Be(expectedAnonymous);
    }
}
