using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Application.Features.Auth.Commands;

/// <summary>
/// Handler for user login authentication.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by email (case-insensitive)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User with email {Email} not found", request.Email);
            throw new ValidationException("Invalid email or password");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
            throw new ValidationException("Invalid email or password");
        }

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(user);

        _logger.LogInformation("User {UserId} ({Email}) logged in successfully", user.Id, user.Email);

        return new LoginResponse(
            UserId: user.Id,
            Token: token,
            FullName: user.FullName,
            Email: user.Email,
            LastLoginAt: user.LastLoginAt ?? DateTime.UtcNow
        );
    }
}
