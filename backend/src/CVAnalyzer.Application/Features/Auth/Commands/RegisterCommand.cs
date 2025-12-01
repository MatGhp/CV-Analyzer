using MediatR;

namespace CVAnalyzer.Application.Features.Auth.Commands;

/// <summary>
/// Command to register a new user account.
/// Optionally migrates guest resume data if a session token is provided.
/// </summary>
public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string? Phone,
    string? GuestSessionToken
) : IRequest<RegisterResponse>;

/// <summary>
/// Response from user registration.
/// </summary>
public record RegisterResponse(
    Guid UserId,
    string Token,
    bool MigratedGuestData,
    int MigratedResumeCount
);
