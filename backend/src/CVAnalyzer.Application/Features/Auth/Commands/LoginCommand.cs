using MediatR;

namespace CVAnalyzer.Application.Features.Auth.Commands;

/// <summary>
/// Command to authenticate a user with email and password.
/// </summary>
public record LoginCommand(
    string Email,
    string Password
) : IRequest<LoginResponse>;

/// <summary>
/// Response from user login.
/// </summary>
public record LoginResponse(
    Guid UserId,
    string Token,
    string FullName,
    string Email,
    DateTime LastLoginAt
);
