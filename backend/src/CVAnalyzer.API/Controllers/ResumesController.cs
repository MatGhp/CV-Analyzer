using CVAnalyzer.Application.Features.Resumes.Commands;
using CVAnalyzer.Application.Features.Resumes.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVAnalyzer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ResumesController> _logger;

    public ResumesController(IMediator mediator, ILogger<ResumesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadResumeResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10_485_760)] // 10MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string? userId,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        // Generate anonymous userId if not provided
        var effectiveUserId = string.IsNullOrEmpty(userId) ? $"anonymous-{Guid.NewGuid():N}" : userId;

        _logger.LogInformation("Uploading resume {FileName} for user {UserId}", file.FileName, effectiveUserId);

        using var stream = file.OpenReadStream();
        var command = new UploadResumeCommand(effectiveUserId, file.FileName, stream);
        var response = await _mediator.Send(command, cancellationToken);

        return AcceptedAtAction(
            nameof(GetStatus),
            new { id = response.ResumeId },
            response);
    }

    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(ResumeStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetResumeStatusQuery(id);
        var status = await _mediator.Send(query, cancellationToken);
        return Ok(status);
    }

    [HttpGet("{id}/analysis")]
    [ProducesResponseType(typeof(ResumeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysis(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetResumeByIdQuery(id);
        var resume = await _mediator.Send(query, cancellationToken);

        if (resume == null)
        {
            return NotFound();
        }

        return Ok(resume);
    }

    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized();
        }

        var command = new DeleteResumeCommand(id, userIdClaim);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        _logger.LogInformation("Deleted resume {ResumeId} by user {UserId}", id, userIdClaim);
        return NoContent();
    }
}
