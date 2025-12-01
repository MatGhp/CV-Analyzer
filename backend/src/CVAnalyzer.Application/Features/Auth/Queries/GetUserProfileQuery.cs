using MediatR;

namespace CVAnalyzer.Application.Features.Auth.Queries;

/// <summary>
/// Query to retrieve current user's profile information.
/// </summary>
public record GetUserProfileQuery(
    Guid UserId
) : IRequest<UserProfileResponse>;

/// <summary>
/// User profile response.
/// </summary>
public record UserProfileResponse(
    Guid UserId,
    string Email,
    string FullName,
    string? Phone,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    bool IsEmailVerified,
    int TotalResumes
);
