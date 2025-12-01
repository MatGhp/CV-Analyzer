using CVAnalyzer.Application.Features.Auth.Commands;
using CVAnalyzer.Application.Features.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CVAnalyzer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account.
    /// Optionally migrates guest resume data if GuestSessionToken is provided.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            Email: request.Email,
            Password: request.Password,
            FullName: request.FullName,
            Phone: request.Phone,
            GuestSessionToken: request.GuestSessionToken
        );

        var response = await _mediator.Send(command);

        _logger.LogInformation(
            "User registered: {UserId}, Email: {Email}, Migrated: {MigratedCount} resumes",
            response.UserId,
            request.Email,
            response.MigratedResumeCount
        );

        return CreatedAtAction(nameof(GetProfile), new { }, response);
    }

    /// <summary>
    /// Authenticate existing user with email and password.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(
            Email: request.Email,
            Password: request.Password
        );

        var response = await _mediator.Send(command);

        _logger.LogInformation("User logged in: {UserId}, Email: {Email}", response.UserId, response.Email);

        return Ok(response);
    }

    /// <summary>
    /// Get current authenticated user's profile.
    /// Requires valid JWT token.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid or missing user ID claim in token");
            return Unauthorized();
        }

        var query = new GetUserProfileQuery(userId);
        var response = await _mediator.Send(query);

        return Ok(response);
    }

    /// <summary>
    /// Get all resumes for the current authenticated user.
    /// Requires valid JWT token.
    /// </summary>
    [HttpGet("me/resumes")]
    [Authorize]
    [ProducesResponseType(typeof(List<UserResumeListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<UserResumeListItem>>> GetMyResumes()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid or missing user ID claim in token");
            return Unauthorized();
        }

        var query = new GetUserResumesQuery(userId);
        var response = await _mediator.Send(query);

        return Ok(response);
    }
}

/// <summary>
/// Request model for user registration.
/// </summary>
public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? Phone,
    string? GuestSessionToken
);

/// <summary>
/// Request model for user login.
/// </summary>
public record LoginRequest(
    string Email,
    string Password
);
