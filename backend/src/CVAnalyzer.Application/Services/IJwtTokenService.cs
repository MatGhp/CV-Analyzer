using CVAnalyzer.Domain.Entities;

namespace CVAnalyzer.Application.Services;

/// <summary>
/// Service for generating and validating JWT authentication tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for an authenticated user.
    /// Token includes claims for userId, email, and fullName.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>A JWT token string valid for 60 minutes.</returns>
    string GenerateToken(User user);

    /// <summary>
    /// Validates a JWT token and extracts the user ID.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>The user ID if valid, null if invalid or expired.</returns>
    Guid? ValidateToken(string token);
}
