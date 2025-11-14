using CVAnalyzer.Application.Features.Resumes.Commands;
using CVAnalyzer.Application.Features.Resumes.Queries;
using MediatR;
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
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string userId,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        using var stream = file.OpenReadStream();
        var command = new UploadResumeCommand(userId, file.FileName, stream);
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
}
