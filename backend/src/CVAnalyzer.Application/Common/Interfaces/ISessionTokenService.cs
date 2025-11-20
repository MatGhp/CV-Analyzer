namespace CVAnalyzer.Application.Common.Interfaces;

/// <summary>
/// Service for generating and validating session tokens for anonymous users
/// </summary>
public interface ISessionTokenService
{
    /// <summary>
    /// Generates a secure session token for anonymous users
    /// </summary>
    /// <returns>A unique session token (guest-{timestamp}-{random})</returns>
    string GenerateSessionToken();
    
    /// <summary>
    /// Validates if a session token is in the correct format
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <returns>True if valid format, false otherwise</returns>
    bool IsValidSessionToken(string token);
    
    /// <summary>
    /// Checks if a user ID represents an anonymous session
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <returns>True if anonymous, false otherwise</returns>
    bool IsAnonymousUser(string userId);
}
