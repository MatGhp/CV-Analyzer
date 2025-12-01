using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Application.Services;
using CVAnalyzer.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Application.Features.Auth.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionTokenService _sessionTokenService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ISessionTokenService sessionTokenService,
        ILogger<RegisterCommandHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _sessionTokenService = sessionTokenService;
        _logger = logger;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Check email uniqueness
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

        if (emailExists)
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
            throw new ValidationException("An account with this email already exists");
        }

        // 2. Create user entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false
        };

        _context.Users.Add(user);

        // 3. Migrate guest data if session token provided
        int migratedCount = 0;
        if (!string.IsNullOrEmpty(request.GuestSessionToken) &&
            _sessionTokenService.IsAnonymousUser(request.GuestSessionToken))
        {
            migratedCount = await MigrateGuestDataAsync(request.GuestSessionToken, user.Id, cancellationToken);
        }

        // 4. Save all changes in a single transaction
        await _context.SaveChangesAsync(cancellationToken);

        // 5. Generate JWT token
        var token = _jwtTokenService.GenerateToken(user);

        _logger.LogInformation(
            "User registered successfully: {Email} (ID: {UserId}), Migrated {Count} resume(s)",
            user.Email, user.Id, migratedCount);

        return new RegisterResponse(user.Id, token, migratedCount > 0, migratedCount);
    }

    private async Task<int> MigrateGuestDataAsync(
        string guestToken,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Find all resumes with the guest session token that haven't been migrated yet
        var guestResumes = await _context.Resumes
            .Where(r => r.UserId == guestToken 
                && r.IsAnonymous 
                && r.AuthenticatedUserId == null) // Only migrate unclaimed resumes
            .ToListAsync(cancellationToken);

        if (guestResumes.Count == 0)
        {
            _logger.LogInformation("No guest resumes found for token {Token}", guestToken);
            return 0;
        }

        // Transfer ownership to authenticated user
        foreach (var resume in guestResumes)
        {
            resume.AuthenticatedUserId = userId;
            resume.IsAnonymous = false; // No longer anonymous
            resume.AnonymousExpiresAt = null; // Remove expiration
            resume.UpdatedAt = DateTime.UtcNow;
        }

        _logger.LogInformation(
            "Migrated {Count} resume(s) from guest token {Token} to user {UserId}",
            guestResumes.Count, guestToken, userId);

        return guestResumes.Count;
    }
}
