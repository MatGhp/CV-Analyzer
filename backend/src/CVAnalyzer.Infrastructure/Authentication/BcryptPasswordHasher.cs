using CVAnalyzer.Application.Services;

namespace CVAnalyzer.Infrastructure.Authentication;

/// <summary>
/// BCrypt-based password hasher with cost factor 12 for production security.
/// Uses BCrypt.Net-Next library for secure password hashing with automatic salting.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // Cost factor (higher = more secure but slower)

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            // Invalid hash format or verification failed
            return false;
        }
    }
}
