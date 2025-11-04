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

    [HttpPost]
    public async Task<ActionResult<Guid>> Upload(IFormFile file, [FromForm] string userId, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        _logger.LogInformation("Uploading resume for user {UserId}", userId);

        using var stream = file.OpenReadStream();
        var command = new UploadResumeCommand(userId, file.FileName, stream);
        var resumeId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = resumeId }, resumeId);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken)
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
