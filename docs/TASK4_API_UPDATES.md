# Task 4: API Updates for Async Processing

**Estimated Time**: 1 day  
**Priority**: P0 (Required for async workflow)  
**Dependencies**: Task 3 (Queue & Background Worker) - Must be complete

---

## Overview

Update API endpoints to support async CV processing with status polling. Refactor upload handler to use queue pattern and add new status endpoint for real-time progress updates.

---

## Prerequisites

✅ Task 3 completed:
- ResumeQueueService for message enqueuing
- ResumeAnalysisWorker processing messages
- CandidateInfo entity in database

---

## Deliverables

### 1. Refactor Upload Command Handler

**File**: `backend/src/CVAnalyzer.Application/Features/Resumes/Commands/UploadResumeCommand.cs`

**Changes**:
- ✅ Return 202 Accepted instead of waiting for analysis
- ✅ Enqueue message to Azure Storage Queue
- ✅ Save Resume with `Status = Pending`
- ✅ Include `BlobUrlWithSas` for Document Intelligence

**Implementation**:
```csharp
public record UploadResumeCommand(
    Stream FileStream,
    string FileName,
    string UserId) : IRequest<UploadResumeResponse>;

public record UploadResumeResponse(
    Guid ResumeId,
    string Status,
    string Message);

public class UploadResumeCommandValidator : AbstractValidator<UploadResumeCommand>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private static readonly string[] AllowedExtensions = { ".pdf", ".docx" };

    public UploadResumeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(HasValidExtension)
            .WithMessage("Only PDF and DOCX files are allowed");

        RuleFor(x => x.FileStream)
            .NotNull()
            .Must(stream => stream.Length > 0)
            .WithMessage("File cannot be empty")
            .Must(stream => stream.Length <= MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024}MB");
    }

    private bool HasValidExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}

public class UploadResumeCommandHandler 
    : IRequestHandler<UploadResumeCommand, UploadResumeResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly IResumeQueueService _queueService;
    private readonly ILogger<UploadResumeCommandHandler> _logger;

    public UploadResumeCommandHandler(
        IApplicationDbContext context,
        IBlobStorageService blobStorage,
        IResumeQueueService queueService,
        ILogger<UploadResumeCommandHandler> logger)
    {
        _context = context;
        _blobStorage = blobStorage;
        _queueService = queueService;
        _logger = logger;
    }

    public async Task<UploadResumeResponse> Handle(
        UploadResumeCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Uploading resume {FileName} for user {UserId}",
            request.FileName, request.UserId);

        // 1. Upload to blob storage with SAS token
        var uploadResult = await _blobStorage.UploadFileAsync(
            request.FileStream,
            request.FileName);

        // 2. Create Resume entity with Pending status
        var resume = new Resume
        {
            UserId = request.UserId,
            FileName = request.FileName,
            BlobUrl = uploadResult.BlobUrl,
            BlobUrlWithSas = uploadResult.BlobUrlWithSas,
            Status = ResumeStatus.Pending,
            UploadedAt = DateTime.UtcNow
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. Enqueue message for background processing
        await _queueService.EnqueueResumeAnalysisAsync(resume.Id, request.UserId);

        _logger.LogInformation(
            "Resume {ResumeId} uploaded successfully. Analysis message enqueued.",
            resume.Id);

        return new UploadResumeResponse(
            resume.Id,
            "pending",
            "Resume uploaded successfully. Analysis in progress.");
    }
}
```

**Key Changes**:
- Fast response (~2s): No waiting for AI analysis
- Queue message: Background worker processes asynchronously
- SAS token stored: Enables Document Intelligence to access blob

---

### 2. New Query: Get Resume Status

**File**: `backend/src/CVAnalyzer.Application/Features/Resumes/Queries/GetResumeStatusQuery.cs`

**Requirements**:
- ✅ Return current resume processing status
- ✅ Map ResumeStatus enum to user-friendly string
- ✅ Calculate progress percentage (0-100)
- ✅ Include error message for failed analyses

**Implementation**:
```csharp
public record GetResumeStatusQuery(Guid ResumeId) : IRequest<ResumeStatusResponse>;

public record ResumeStatusResponse(
    Guid ResumeId,
    string Status,
    int Progress,
    string? ErrorMessage);

public class GetResumeStatusQueryHandler 
    : IRequestHandler<GetResumeStatusQuery, ResumeStatusResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetResumeStatusQueryHandler> _logger;

    public GetResumeStatusQueryHandler(
        IApplicationDbContext context,
        ILogger<GetResumeStatusQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResumeStatusResponse> Handle(
        GetResumeStatusQuery request,
        CancellationToken cancellationToken)
    {
        var resume = await _context.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resume), request.ResumeId);

        var (status, progress) = resume.Status switch
        {
            ResumeStatus.Pending => ("pending", 0),
            ResumeStatus.Processing => ("processing", 50),
            ResumeStatus.Analyzed => ("complete", 100),
            ResumeStatus.Failed => ("failed", 0),
            _ => ("unknown", 0)
        };

        var errorMessage = resume.Status == ResumeStatus.Failed 
            ? "Analysis failed. Please try uploading again or contact support if the issue persists."
            : null;

        _logger.LogInformation(
            "Status check for resume {ResumeId}: {Status} ({Progress}%)",
            resume.Id, status, progress);

        return new ResumeStatusResponse(
            resume.Id,
            status,
            progress,
            errorMessage);
    }
}
```

**Status Mapping**:
- `Pending` → "pending" (0%)
- `Processing` → "processing" (50%)
- `Analyzed` → "complete" (100%)
- `Failed` → "failed" (0%)

---

### 3. Update GetResumeByIdQuery

**File**: `backend/src/CVAnalyzer.Application/Features/Resumes/Queries/GetResumeByIdQuery.cs`

**Changes**:
- ✅ Include `CandidateInfo` in response
- ✅ Add null checks for optional fields
- ✅ Map Skills list from JSON

**Updated Response DTO**:
```csharp
public record ResumeDetailResponse(
    Guid Id,
    string FileName,
    int? Score,
    DateTime UploadedAt,
    DateTime? AnalyzedAt,
    string Status,
    CandidateInfoDto? CandidateInfo,
    List<SuggestionDto> Suggestions,
    string? OptimizedContent);

public record CandidateInfoDto(
    string FullName,
    string Email,
    string? Phone,
    string? Location,
    List<string> Skills,
    int? YearsOfExperience,
    string? CurrentJobTitle,
    string? Education);

public record SuggestionDto(
    string Category,
    string Description,
    int Priority);
```

**Updated Handler**:
```csharp
public class GetResumeByIdQueryHandler 
    : IRequestHandler<GetResumeByIdQuery, ResumeDetailResponse>
{
    private readonly IApplicationDbContext _context;

    public GetResumeByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResumeDetailResponse> Handle(
        GetResumeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var resume = await _context.Resumes
            .Include(r => r.CandidateInfo)
            .Include(r => r.Suggestions)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ResumeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resume), request.ResumeId);

        var candidateInfoDto = resume.CandidateInfo != null
            ? new CandidateInfoDto(
                resume.CandidateInfo.FullName,
                resume.CandidateInfo.Email,
                resume.CandidateInfo.Phone,
                resume.CandidateInfo.Location,
                resume.CandidateInfo.Skills,
                resume.CandidateInfo.YearsOfExperience,
                resume.CandidateInfo.CurrentJobTitle,
                resume.CandidateInfo.Education)
            : null;

        var suggestions = resume.Suggestions
            .Select(s => new SuggestionDto(s.Category, s.Description, s.Priority))
            .ToList();

        var status = resume.Status switch
        {
            ResumeStatus.Pending => "pending",
            ResumeStatus.Processing => "processing",
            ResumeStatus.Analyzed => "complete",
            ResumeStatus.Failed => "failed",
            _ => "unknown"
        };

        return new ResumeDetailResponse(
            resume.Id,
            resume.FileName,
            resume.Score,
            resume.UploadedAt,
            resume.AnalyzedAt,
            status,
            candidateInfoDto,
            suggestions,
            resume.OptimizedContent);
    }
}
```

---

### 4. Update Controllers

**File**: `backend/src/CVAnalyzer.API/Controllers/ResumesController.cs`

**Changes**:
- ✅ Upload returns 202 Accepted
- ✅ Add GET `/status` endpoint
- ✅ Update GET `/analysis` to include candidate info

**Updated Controller**:
```csharp
using Microsoft.AspNetCore.Mvc;
using MediatR;

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

    /// <summary>
    /// Upload a resume for analysis (async)
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadResumeResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string userId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        using var stream = file.OpenReadStream();
        var command = new UploadResumeCommand(stream, file.FileName, userId);
        var response = await _mediator.Send(command);

        return AcceptedAtAction(
            nameof(GetStatus),
            new { id = response.ResumeId },
            response);
    }

    /// <summary>
    /// Get resume processing status
    /// </summary>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(ResumeStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid id)
    {
        var query = new GetResumeStatusQuery(id);
        var status = await _mediator.Send(query);
        return Ok(status);
    }

    /// <summary>
    /// Get full analysis results (only when status = complete)
    /// </summary>
    [HttpGet("{id}/analysis")]
    [ProducesResponseType(typeof(ResumeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysis(Guid id)
    {
        var query = new GetResumeByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
```

**API Endpoints Summary**:
- `POST /api/resumes/upload` → 202 Accepted (fast response)
- `GET /api/resumes/{id}/status` → Resume status with progress
- `GET /api/resumes/{id}/analysis` → Full analysis results
- `GET /api/resumes/health` → Health check

---

## Unit Tests

### UploadResumeCommandHandlerTests

**File**: `backend/tests/CVAnalyzer.UnitTests/Features/Resumes/Commands/UploadResumeCommandHandlerTests.cs`

**Test Cases**:
- ✅ `Handle_ValidFile_ReturnsAcceptedResponse`
- ✅ `Handle_ValidFile_EnqueuesMessage`
- ✅ `Handle_ValidFile_SavesResumeWithPendingStatus`
- ✅ `Handle_InvalidFileType_ThrowsValidationException`
- ✅ `Handle_FileTooLarge_ThrowsValidationException`

### GetResumeStatusQueryHandlerTests

**File**: `backend/tests/CVAnalyzer.UnitTests/Features/Resumes/Queries/GetResumeStatusQueryHandlerTests.cs`

**Test Cases**:
- ✅ `Handle_PendingResume_Returns0Progress`
- ✅ `Handle_ProcessingResume_Returns50Progress`
- ✅ `Handle_AnalyzedResume_Returns100Progress`
- ✅ `Handle_FailedResume_ReturnsErrorMessage`
- ✅ `Handle_NonExistentResume_ThrowsNotFoundException`

---

## Integration Tests

**File**: `backend/tests/CVAnalyzer.IntegrationTests/Features/Resumes/ResumeWorkflowIntegrationTests.cs`

**Test Scenario**: Full async upload → status polling → analysis retrieval
```csharp
[Fact]
public async Task FullAsyncWorkflow_UploadPollAndRetrieve_Success()
{
    // Arrange
    var samplePdfPath = "TestData/sample-resume.pdf";
    using var fileStream = File.OpenRead(samplePdfPath);
    var fileName = "sample-resume.pdf";
    var userId = "test-user-123";

    // Act 1: Upload resume
    var uploadCommand = new UploadResumeCommand(fileStream, fileName, userId);
    var uploadResponse = await _mediator.Send(uploadCommand);

    Assert.Equal("pending", uploadResponse.Status);
    Assert.NotEqual(Guid.Empty, uploadResponse.ResumeId);

    // Act 2: Poll status until complete (max 30s)
    var statusQuery = new GetResumeStatusQuery(uploadResponse.ResumeId);
    var maxAttempts = 15; // 15 * 2s = 30s
    ResumeStatusResponse? statusResponse = null;

    for (int i = 0; i < maxAttempts; i++)
    {
        statusResponse = await _mediator.Send(statusQuery);
        
        if (statusResponse.Status == "complete")
            break;
        
        if (statusResponse.Status == "failed")
            Assert.Fail("Resume analysis failed");

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    Assert.NotNull(statusResponse);
    Assert.Equal("complete", statusResponse.Status);
    Assert.Equal(100, statusResponse.Progress);

    // Act 3: Retrieve full analysis
    var analysisQuery = new GetResumeByIdQuery(uploadResponse.ResumeId);
    var analysisResponse = await _mediator.Send(analysisQuery);

    // Assert
    Assert.NotNull(analysisResponse.CandidateInfo);
    Assert.NotEmpty(analysisResponse.CandidateInfo.FullName);
    Assert.NotEmpty(analysisResponse.CandidateInfo.Email);
    Assert.NotEmpty(analysisResponse.CandidateInfo.Skills);
    Assert.True(analysisResponse.Score > 0);
    Assert.NotEmpty(analysisResponse.Suggestions);
}
```

---

## API Documentation

### Swagger/OpenAPI Configuration

**File**: `backend/src/CVAnalyzer.API/Program.cs`

**Add Swagger annotations**:
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CV Analyzer API",
        Version = "v1",
        Description = "Async resume analysis API with status polling"
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

---

## Acceptance Criteria

- [ ] Upload endpoint returns 202 Accepted within 2 seconds
- [ ] Status endpoint returns correct progress for all resume states
- [ ] Analysis endpoint returns candidate info when status = complete
- [ ] Validation rejects files > 10MB
- [ ] Validation rejects non-PDF/DOCX files
- [ ] Unit tests pass for all command/query handlers (>15 tests)
- [ ] Integration test verifies full async workflow (upload → poll → retrieve)
- [ ] Swagger documentation displays all endpoints correctly
- [ ] CORS configured for frontend origin

---

## Configuration Updates

**File**: `backend/src/CVAnalyzer.API/appsettings.json`

**Add file upload limits**:
```json
{
  "FileUpload": {
    "MaxFileSizeBytes": 10485760,
    "AllowedExtensions": [".pdf", ".docx"]
  },
  "Kestrel": {
    "Limits": {
      "MaxRequestBodySize": 10485760
    }
  }
}
```

**Update CORS** (if needed):
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowFrontend");
```

---

## Troubleshooting

### Issue: Upload times out

**Check**:
```bash
# Verify Kestrel request size limit
curl -X POST http://localhost:5000/api/resumes/upload \
  -F "file=@large-resume.pdf" \
  -F "userId=test"
```

**Solution**: Increase `MaxRequestBodySize` in appsettings.json

### Issue: Status always returns "pending"

**Check worker logs**:
```bash
kubectl logs <api-pod> | grep "ResumeAnalysisWorker"
```

**Verify queue has messages**:
```bash
az storage queue show --name resume-analysis --account-name cvanalyzerdevs4b3 --auth-mode login
```

---

## Rollback Plan

1. Revert controller changes (restore sync upload endpoint)
2. Remove status endpoint from routing
3. Fallback: Process synchronously in upload handler
4. No data loss: Queue messages can be drained manually

---

## Next Steps

After Task 4 completion:
- **Task 5**: Frontend (status polling UI, candidate info display)
- **Task 6**: Testing & Deployment (E2E tests, monitoring, production deployment)
