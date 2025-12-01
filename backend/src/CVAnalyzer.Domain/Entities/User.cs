namespace CVAnalyzer.Domain.Entities;

/// <summary>
/// Represents an authenticated user account in the system.
/// Users can register after completing guest resume analysis to save their results permanently.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address (unique, used for login).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt hashed password (never store plain text).
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User's full name (extracted from resume or provided during registration).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's phone number (optional, extracted from resume).
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp of the user's last successful login (updated on each login).
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Indicates whether the user's email address has been verified (future feature).
    /// </summary>
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// Navigation property: Collection of resumes owned by this user.
    /// Includes both originally authenticated uploads and migrated guest uploads.
    /// </summary>
    public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
}
