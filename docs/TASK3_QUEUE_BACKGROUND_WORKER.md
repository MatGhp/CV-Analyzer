# Task 3: Queue & Background Worker Implementation

**Estimated Time**: 1 day  
**Priority**: P0 (Blocking for async processing)  
**Dependencies**: Task 2 (Backend Core) - ✅ COMPLETE  
**Last Reviewed**: November 13, 2025

---

## ⚠️ Implementation Status

**Task 2 Dependencies - ✅ ALL COMPLETE:**
- ✅ BlobStorageService with SAS token generation (managed identity support)
- ✅ DocumentIntelligenceService for text extraction
- ✅ CandidateInfo entity and database migration (RefactoredTask2Implementation)
- ✅ Resume entity with ResumeStatus enum (Pending, Processing, Analyzed, Failed)
- ✅ QueueServiceClient registered in DI (connection string + managed identity modes)
- ✅ ResumeAnalysisAgent exists in CVAnalyzer.AgentService project (NOT as interface)

**Existing Resources:**
- ✅ AgentService project: Contains `ResumeAnalysisAgent` class (OpenAI-based, uses ChatCompletions API)
- ✅ Agent models: `ResumeAnalysisRequest`, `ResumeAnalysisResponse`, `ResumeSuggestion` in CVAnalyzer.AgentService.Models
- ✅ QueueServiceClient already configured in Infrastructure DI

**Critical Architecture Notes:**
- **ResumeAnalysisAgent is NOT an interface** - It's a concrete class in CVAnalyzer.AgentService project
- Agent uses OpenAI ChatCompletions API (not Agent Framework's ChatAgent)
- Response structure: Score, OptimizedContent, Suggestions[], Metadata dictionary
- Current agent returns structured JSON via function calling
- **BlobUrlWithSas removed from database** - Generate on-demand via `IBlobStorageService.GenerateSasTokenAsync()`

---

## 📋 Implementation Summary

### What Needs to Be Created

1. **ResumeQueueService** (`Infrastructure/Services/ResumeQueueService.cs`)
   - Interface: `IResumeQueueService` with `EnqueueResumeAnalysisAsync(Guid, string)`
   - Implementation: Uses `QueueServiceClient` to enqueue JSON messages
   - Message format: `ResumeAnalysisMessage(ResumeId, UserId)` record

2. **ResumeAnalysisWorker** (`Infrastructure/BackgroundServices/ResumeAnalysisWorker.cs`)
   - Extends `BackgroundService` (ASP.NET Core hosted service)
   - Polls `resume-analysis` queue every 2 seconds
   - Processes up to 10 messages per batch
   - Two-stage pipeline: Document Intelligence → GPT-4o analysis
   - Automatic retry with 5-minute visibility timeout
   - Poison queue for failed messages (>5 retries)

3. **DI Registration** (`Infrastructure/DependencyInjection.cs`)
   - Register `IResumeQueueService` as scoped
   - Register `ResumeAnalysisWorker` as hosted service
   - Register `OpenAIClient` and `ResumeAnalysisAgent` from AgentService
   - DO NOT create queues in DI (already exist from Terraform)

4. **AgentService Enhancement** (`AgentService/ResumeAnalysisAgent.cs` or new models)
   - **CRITICAL**: Extend response to include CandidateInfo fields
   - Update system prompt to extract: FullName, Email, Phone, Location, Skills, etc.
   - Add `CandidateInfoDto` to `ResumeAnalysisResponse` model

5. **Unit Tests** (`UnitTests/Services/ResumeQueueServiceTests.cs`, `UnitTests/BackgroundServices/ResumeAnalysisWorkerTests.cs`)
   - Test queue message serialization
   - Test worker message processing
   - Test retry and poison queue logic

6. **Integration Test** (`IntegrationTests/BackgroundServices/ResumeAnalysisWorkerIntegrationTests.cs`)
   - Full flow: Upload → Enqueue → Worker processes → Verify status = Analyzed

### What Already Exists (DO NOT RECREATE)

- ✅ `QueueServiceClient` registered in Infrastructure DI
- ✅ Queues `resume-analysis` and `resume-analysis-poison` created by Terraform
- ✅ `ResumeAnalysisAgent` class in CVAnalyzer.AgentService project
- ✅ `BlobStorageService.GenerateSasTokenAsync()` for on-demand SAS tokens
- ✅ `DocumentIntelligenceService.ExtractTextFromDocumentAsync()` for text extraction
- ✅ `Resume` entity with `Status` enum (Pending, Processing, Analyzed, Failed)
- ✅ `CandidateInfo` entity with all required fields
- ✅ Database migration `RefactoredTask2Implementation`

---

## Overview

Implement Azure Storage Queue integration and BackgroundService worker for async CV analysis. This enables non-blocking uploads and resilient processing with automatic retries.

---

## Prerequisites

✅ Task 2 completed:
- ✅ BlobStorageService with SAS token generation (on-demand)
- ✅ DocumentIntelligenceService for text extraction
- ✅ CandidateInfo entity and database migration
- ✅ Resume entity with ResumeStatus enum
- ✅ QueueServiceClient registered in DI
- ✅ ResumeAnalysisAgent in AgentService project

---

## Architecture

```
┌──────────────────┐
│ Upload Handler   │
│ (Fast ~2s)       │
└────────┬─────────┘
         │
         │ Enqueue message
         ▼
┌──────────────────────────┐
│ Azure Storage Queue      │
│ • resume-analysis        │
│ • Visibility timeout: 5m │
│ • Max retries: 5         │
└────────┬─────────────────┘
         │
         │ Poll every 2s
         ▼
┌──────────────────────────┐
│ ResumeAnalysisWorker     │
│ (BackgroundService)      │
│                          │
│ 1. Receive message       │
│ 2. Update status         │
│ 3. Extract text (DocAI)  │
│ 4. Analyze (GPT-4o)      │
│ 5. Save results          │
│ 6. Delete message        │
│                          │
│ On error: Auto-retry     │
│ After 5 retries: Poison  │
└──────────────────────────┘
```

---

## Deliverables

### 1. Resume Queue Service

**File**: `backend/src/CVAnalyzer.Infrastructure/Services/ResumeQueueService.cs`

**Requirements**:
- ✅ Enqueue resume analysis messages
- ✅ Serialize message as JSON
- ✅ Log message enqueued for monitoring

**Implementation**:
```csharp
using Azure.Storage.Queues;
using System.Text.Json;

public interface IResumeQueueService
{
    Task EnqueueResumeAnalysisAsync(Guid resumeId, string userId);
}

public record ResumeAnalysisMessage(Guid ResumeId, string UserId);

public class ResumeQueueService : IResumeQueueService
{
    private readonly QueueClient _queueClient;
    private readonly ILogger<ResumeQueueService> _logger;

    public ResumeQueueService(
        QueueServiceClient queueServiceClient,
        ILogger<ResumeQueueService> logger)
    {
        _queueClient = queueServiceClient.GetQueueClient("resume-analysis");
        _logger = logger;
    }

    public async Task EnqueueResumeAnalysisAsync(Guid resumeId, string userId)
    {
        var message = new ResumeAnalysisMessage(resumeId, userId);
        var messageText = JsonSerializer.Serialize(message);
        
        await _queueClient.SendMessageAsync(messageText);
        
        _logger.LogInformation(
            "Enqueued resume analysis for {ResumeId} (User: {UserId})", 
            resumeId, userId);
    }
}
```

**Why Azure Storage Queue?**
- Persistent: Survives container restarts (critical for Container Apps)
- Automatic retry: Visibility timeout re-queues failed messages
- No external dependencies: No Hangfire, no Redis
- Built-in monitoring: Azure Portal metrics (queue depth, message age)

**Testing**:
```csharp
[Fact]
public async Task EnqueueResumeAnalysisAsync_ValidMessage_Success()
{
    var resumeId = Guid.NewGuid();
    await _queueService.EnqueueResumeAnalysisAsync(resumeId, "test-user");
    
    // Verify message in queue
    var messages = await _queueClient.ReceiveMessagesAsync(maxMessages: 1);
    Assert.Single(messages.Value);
    
    var message = JsonSerializer.Deserialize<ResumeAnalysisMessage>(
        messages.Value[0].MessageText);
    Assert.Equal(resumeId, message!.ResumeId);
}
```

---

### 2. Background Worker Service

**File**: `backend/src/CVAnalyzer.Infrastructure/BackgroundServices/ResumeAnalysisWorker.cs`

**Requirements**:
- ✅ Poll queue every 2 seconds (up to 10 messages per batch)
- ✅ Process messages with visibility timeout (5 minutes)
- ✅ Automatic retry: Message becomes visible again if not deleted
- ✅ Max retries: 5 attempts, then move to poison queue
- ✅ Two-stage AI pipeline:
  - Stage 1: Document Intelligence text extraction
  - Stage 2: GPT-4o analysis of extracted text
- ✅ Transaction boundaries with status rollback on failure
- ✅ Graceful shutdown on cancellation
- ✅ Scoped service injection per message

**Implementation**:
```csharp
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

public class ResumeAnalysisWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly QueueClient _queueClient;
    private readonly QueueClient _poisonQueueClient;
    private readonly ILogger<ResumeAnalysisWorker> _logger;
    
    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromMinutes(5);
    private static readonly int MaxDequeueCount = 5;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);

    public ResumeAnalysisWorker(
        IServiceProvider serviceProvider,
        QueueServiceClient queueServiceClient,
        ILogger<ResumeAnalysisWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _queueClient = queueServiceClient.GetQueueClient("resume-analysis");
        _poisonQueueClient = queueServiceClient.GetQueueClient("resume-analysis-poison");
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ResumeAnalysisWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _queueClient.ReceiveMessagesAsync(
                    maxMessages: 10,
                    visibilityTimeout: VisibilityTimeout,
                    cancellationToken: stoppingToken);

                if (response.Value?.Length > 0)
                {
                    _logger.LogInformation(
                        "Received {Count} messages from queue", 
                        response.Value.Length);
                    
                    foreach (var message in response.Value)
                    {
                        await ProcessMessageAsync(message, stoppingToken);
                    }
                }
                else
                {
                    await Task.Delay(PollingInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break; // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in message processing loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("ResumeAnalysisWorker stopped");
    }

    private async Task ProcessMessageAsync(
        QueueMessage message, 
        CancellationToken cancellationToken)
    {
        try
        {
            var analysisMessage = JsonSerializer.Deserialize<ResumeAnalysisMessage>(
                message.MessageText)
                ?? throw new InvalidOperationException("Invalid message format");

            _logger.LogInformation(
                "Processing resume {ResumeId} (Attempt {DequeueCount}/{MaxRetries})",
                analysisMessage.ResumeId, message.DequeueCount, MaxDequeueCount);

            // Check max retries
            if (message.DequeueCount >= MaxDequeueCount)
            {
                _logger.LogError(
                    "Resume {ResumeId} exceeded max retries. Moving to poison queue.",
                    analysisMessage.ResumeId);
                
                await HandleFailedMessageAsync(analysisMessage, "Max retries exceeded");
                await _queueClient.DeleteMessageAsync(
                    message.MessageId, 
                    message.PopReceipt);
                return;
            }

            // Process with scoped services
            using var scope = _serviceProvider.CreateScope();
            await ProcessResumeAnalysisAsync(
                scope.ServiceProvider, 
                analysisMessage, 
                cancellationToken);

            // Success - delete message
            await _queueClient.DeleteMessageAsync(
                message.MessageId, 
                message.PopReceipt, 
                cancellationToken);

            _logger.LogInformation(
                "Successfully processed resume {ResumeId}",
                analysisMessage.ResumeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to process message {MessageId} (will retry)", 
                message.MessageId);
            // Don't delete - message will become visible again after timeout
        }
    }

    private async Task ProcessResumeAnalysisAsync(
        IServiceProvider serviceProvider,
        ResumeAnalysisMessage message,
        CancellationToken cancellationToken)
    {
        var context = serviceProvider.GetRequiredService<IApplicationDbContext>();
        var blobStorage = serviceProvider.GetRequiredService<IBlobStorageService>();
        var documentIntelligence = serviceProvider.GetRequiredService<IDocumentIntelligenceService>();
        var agent = serviceProvider.GetRequiredService<ResumeAnalysisAgent>(); // Concrete class from AgentService

        var resume = await context.Resumes
            .Include(r => r.CandidateInfo)
            .Include(r => r.Suggestions)
            .FirstOrDefaultAsync(r => r.Id == message.ResumeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resume), message.ResumeId);

        try
        {
            // Update status to processing
            resume.Status = ResumeStatus.Processing;
            await context.SaveChangesAsync(cancellationToken);

            // Generate SAS token for Document Intelligence (on-demand)
            var blobUrlWithSas = await blobStorage.GenerateSasTokenAsync(resume.BlobUrl, cancellationToken);

            // Stage 1: Extract text with Document Intelligence
            _logger.LogInformation(
                "Stage 1: Extracting text for resume {ResumeId}", 
                resume.Id);
            
            var extractedText = await documentIntelligence
                .ExtractTextFromDocumentAsync(blobUrlWithSas, cancellationToken);
            
            resume.Content = extractedText; // Store extracted text
            await context.SaveChangesAsync(cancellationToken);

            // Stage 2: Analyze with GPT-4o (ResumeAnalysisAgent from AgentService)
            _logger.LogInformation(
                "Stage 2: Analyzing resume {ResumeId} with GPT-4o", 
                resume.Id);
            
            var analysisRequest = new CVAnalyzer.AgentService.Models.ResumeAnalysisRequest
            {
                Content = extractedText,
                UserId = message.UserId
            };
            
            var analysisResult = await agent.AnalyzeAsync(analysisRequest, cancellationToken);

            // ⚠️ CRITICAL: AgentService response does NOT include CandidateInfo
            // AgentService.ResumeAnalysisResponse only contains: Score, OptimizedContent, Suggestions[], Metadata
            // CandidateInfo extraction must be added to AgentService or handled separately
            
            // TODO: Update AgentService to extract candidate info OR implement separate parser
            // For now, log warning if CandidateInfo needed
            _logger.LogWarning(
                "CandidateInfo extraction not implemented in AgentService. Resume {ResumeId} will have no candidate info.",
                resume.Id);

            // Update suggestions (map from AgentService.ResumeSuggestion to Domain.Suggestion)
            resume.Suggestions.Clear();
            foreach (var suggestion in analysisResult.Suggestions)
            {
                resume.Suggestions.Add(new Suggestion
                {
                    Category = suggestion.Category,
                    Description = suggestion.Description,
                    Priority = suggestion.Priority,
                    ResumeId = resume.Id
                });
            }

            // Update resume
            resume.Score = analysisResult.Score;
            resume.OptimizedContent = analysisResult.OptimizedContent;
            resume.Status = ResumeStatus.Analyzed;
            resume.AnalyzedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to analyze resume {ResumeId}. Rolling back status.", 
                resume.Id);
            
            // Rollback status on failure
            resume.Status = ResumeStatus.Pending;
            await context.SaveChangesAsync(cancellationToken);
            
            throw; // Re-throw for retry mechanism
        }
    }

    private async Task HandleFailedMessageAsync(
        ResumeAnalysisMessage message, 
        string errorReason)
    {
        // Move to poison queue for manual review
        var poisonMessage = JsonSerializer.Serialize(new
        {
            message.ResumeId,
            message.UserId,
            ErrorReason = errorReason,
            FailedAt = DateTime.UtcNow
        });
        
        await _poisonQueueClient.SendMessageAsync(poisonMessage);
        
        // Update resume status to failed
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        
        var resume = await context.Resumes.FindAsync(message.ResumeId);
        if (resume != null)
        {
            resume.Status = ResumeStatus.Failed;
            await context.SaveChangesAsync();
        }
    }
}
```

**Key Features**:
- **Batch Processing**: Up to 10 messages per poll (throughput optimization)
- **Visibility Timeout**: 5 minutes - if processing fails, message reappears
- **Automatic Retry**: Azure Storage Queue handles retry logic
- **Poison Queue**: Failed messages (>5 retries) isolated for manual review
- **Scoped Services**: New scope per message for DbContext isolation
- **Transaction Boundaries**: Status rollback on failure
- **Graceful Shutdown**: Cancellation token respected

---

### 3. Dependency Injection Updates

**File**: `backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs`

**Add Queue Service Registration**:
```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // ... existing blob/document intelligence setup ...
    
    // Queue Service
    services.AddScoped<IResumeQueueService, ResumeQueueService>();
    
    // Background Worker (auto-starts with host)
    services.AddHostedService<ResumeAnalysisWorker>();
    
    // Register AgentService (from CVAnalyzer.AgentService project)
    CVAnalyzer.AgentService.AgentStartup agentStartup = new();
    var tempBuilder = WebApplication.CreateBuilder(); // Temporary for AgentStartup
    agentStartup.ConfigureServices(services, tempBuilder);
    
    // ⚠️ Alternative: Manually register OpenAIClient and ResumeAnalysisAgent
    // services.AddSingleton<OpenAIClient>(sp => { /* configure */ });
    // services.AddSingleton<ResumeAnalysisAgent>();
    
    // ⚠️ DO NOT use BuildServiceProvider here (Service Locator anti-pattern fixed in Task 2)
    // Queue creation moved to ConfigureAzureStorage or Terraform
    
    return services;
}
```

**⚠️ Queue Creation Strategy**:

**Option A (Recommended): Terraform-managed (Task 1 already created queues)**
```hcl
# terraform/modules/ai-foundry/main.tf
resource "azurerm_storage_queue" "resume_analysis" {
  name                 = "resume-analysis"
  storage_account_name = azurerm_storage_account.main.name
}
```

**Option B: Application startup with proper async initialization**
```csharp
// In Program.cs AFTER app.Build()
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var queueServiceClient = scope.ServiceProvider.GetRequiredService<QueueServiceClient>();
    var mainQueue = queueServiceClient.GetQueueClient("resume-analysis");
    var poisonQueue = queueServiceClient.GetQueueClient("resume-analysis-poison");
    
    await mainQueue.CreateIfNotExistsAsync();
    await poisonQueue.CreateIfNotExistsAsync();
}

app.Run();
```

**Why NOT in DependencyInjection.cs?**
- **Service Locator anti-pattern**: `BuildServiceProvider()` during DI registration is bad practice (fixed in Task 2)
- **Async operations**: `CreateIfNotExistsAsync()` requires async/await
- **Idempotency**: Queues already created by Terraform in Task 1

---

## Unit Tests

### ResumeQueueServiceTests

**File**: `backend/tests/CVAnalyzer.UnitTests/Services/ResumeQueueServiceTests.cs`

**Test Cases**:
- ✅ `EnqueueResumeAnalysisAsync_ValidMessage_SendsToQueue`
- ✅ `EnqueueResumeAnalysisAsync_SerializesMessageCorrectly`

### ResumeAnalysisWorkerTests

**File**: `backend/tests/CVAnalyzer.UnitTests/BackgroundServices/ResumeAnalysisWorkerTests.cs`

**Test Cases** (using TestHost):
- ✅ `ExecuteAsync_ReceivesMessage_ProcessesSuccessfully`
- ✅ `ExecuteAsync_MaxRetriesExceeded_MovesToPoisonQueue`
- ✅ `ExecuteAsync_ProcessingFails_MessageBecomesVisibleAgain`
- ✅ `ExecuteAsync_CancellationToken_StopsGracefully`

---

## Integration Tests

**File**: `backend/tests/CVAnalyzer.IntegrationTests/BackgroundServices/ResumeAnalysisWorkerIntegrationTests.cs`

**Test Scenario**: Full async processing flow
```csharp
[Fact]
public async Task FullFlow_EnqueueAndProcess_Success()
{
    // Arrange - Upload resume
    var uploadResult = await UploadSampleResumeAsync();
    var resumeId = uploadResult.ResumeId;
    
    // Act - Enqueue message
    await _queueService.EnqueueResumeAnalysisAsync(resumeId, "test-user");
    
    // Wait for worker to process (max 30s)
    var processed = await WaitForResumeStatusAsync(
        resumeId, 
        ResumeStatus.Analyzed, 
        TimeSpan.FromSeconds(30));
    
    Assert.True(processed, "Resume should be analyzed within 30 seconds");
    
    // Assert - Verify results
    var resume = await _context.Resumes
        .Include(r => r.CandidateInfo)
        .Include(r => r.Suggestions)
        .FirstOrDefaultAsync(r => r.Id == resumeId);
    
    Assert.NotNull(resume);
    Assert.Equal(ResumeStatus.Analyzed, resume.Status);
    Assert.NotNull(resume.CandidateInfo);
    Assert.NotEmpty(resume.CandidateInfo.FullName);
    Assert.NotEmpty(resume.CandidateInfo.Email);
    Assert.True(resume.Score > 0);
    Assert.NotEmpty(resume.Suggestions);
}
```

---

## Monitoring & Observability

### Application Insights Queries

**Queue Depth Over Time**:
```kusto
customMetrics
| where name == "QueueDepth"
| where timestamp > ago(1h)
| project timestamp, value
| render timechart
```

**Processing Time**:
```kusto
traces
| where message contains "Successfully processed resume"
| extend resumeId = extract("resume ([a-f0-9-]+)", 1, message)
| join kind=inner (
    traces
    | where message contains "Processing resume"
    | extend resumeId = extract("resume ([a-f0-9-]+)", 1, message)
) on resumeId
| extend duration = datetime_diff('second', timestamp, timestamp1)
| summarize avg(duration), percentile(duration, 95) by bin(timestamp, 5m)
```

**Retry Rate**:
```kusto
traces
| where message contains "Attempt"
| extend dequeueCount = extract("Attempt (\\d+)", 1, message)
| summarize count() by tostring(dequeueCount), bin(timestamp, 1h)
| render columnchart
```

### Azure Portal Metrics

Navigate to Storage Account → Monitoring → Metrics:
- **Queue Message Count**: `resume-analysis` and `resume-analysis-poison`
- **Transaction Latency**: E2E latency for queue operations
- **Availability**: Queue service uptime

---

## Acceptance Criteria

- [ ] ResumeQueueService enqueues messages successfully
- [ ] ResumeAnalysisWorker polls queue every 2 seconds
- [ ] Worker processes messages with two-stage pipeline (DocAI + GPT-4o)
- [ ] Automatic retry: Failed messages reappear after 5 minutes
- [ ] Poison queue: Messages exceeding 5 retries moved to poison queue
- [ ] Transaction boundaries: Status rolled back on failure
- [ ] Graceful shutdown: Worker stops on cancellation token
- [ ] Integration test verifies full async flow (< 30s processing)
- [ ] Unit tests cover happy path and error scenarios (>15 tests)
- [ ] Monitoring queries return data in Application Insights

---

## ⚠️ Known Issues & Decisions

### Issue 1: CandidateInfo Not Extracted by AgentService

**Problem**: Current `ResumeAnalysisAgent` in CVAnalyzer.AgentService returns:
- `Score` (int)
- `OptimizedContent` (string)
- `Suggestions[]` (Category, Description, Priority)
- `Metadata` (dictionary)

**Missing**: CandidateInfo fields (FullName, Email, Phone, Location, Skills, YearsOfExperience, CurrentJobTitle, Education)

**Options**:

**A. Extend AgentService to extract candidate info (RECOMMENDED)**
```csharp
// Update AgentService/Models/ResumeAnalysisResponse.cs
public record ResumeAnalysisResponse
{
    public int Score { get; init; }
    public string OptimizedContent { get; init; } = string.Empty;
    public ResumeSuggestion[] Suggestions { get; init; } = Array.Empty<ResumeSuggestion>();
    public Dictionary<string, string> Metadata { get; init; } = new();
    
    // NEW: Add CandidateInfo
    public CandidateInfoDto? CandidateInfo { get; init; }
}

public record CandidateInfoDto(
    string FullName,
    string Email,
    string? Phone,
    string? Location,
    List<string> Skills,
    int? YearsOfExperience,
    string? CurrentJobTitle,
    string? Education
);
```

Update `ResumeAnalysisAgent.cs` system prompt to extract candidate info fields.

**B. Separate CandidateInfo extraction service**
```csharp
public interface ICandidateInfoExtractor
{
    Task<CandidateInfo> ExtractFromTextAsync(string resumeText);
}
```
Use separate LLM call or regex patterns to extract structured data.

**C. Skip CandidateInfo in MVP** (NOT recommended - breaks existing schema)

**Decision**: Extend AgentService in Task 3 implementation to avoid breaking database schema.

### Issue 2: AgentService DI Registration

**Problem**: AgentService has its own `AgentStartup` class with DI configuration. Need to register in Infrastructure.

**Solution**: Call `AgentStartup.ConfigureServices()` from Infrastructure DI or manually register services:

```csharp
// Option A: Use AgentStartup
var agentStartup = new CVAnalyzer.AgentService.AgentStartup();
var tempBuilder = WebApplication.CreateBuilder();
agentStartup.ConfigureServices(services, tempBuilder);

// Option B: Manual registration (preferred for cleaner DI)
services.Configure<AgentServiceOptions>(configuration.GetSection(AgentServiceOptions.SectionName));
services.AddSingleton<OpenAIClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<AgentServiceOptions>>().Value;
    return new OpenAIClient(new Uri(options.Endpoint), new DefaultAzureCredential());
});
services.AddSingleton<ResumeAnalysisAgent>();
```

**Decision**: Manual registration (Option B) to avoid coupling with AgentStartup WebApplication dependencies.

---

## Troubleshooting

### Issue: Worker not processing messages

**Check**:
```bash
# Verify worker started
kubectl logs <pod-name> | grep "ResumeAnalysisWorker started"

# Check queue depth
az storage queue show --name resume-analysis --account-name cvanalyzerdevs4b3 --auth-mode login
```

### Issue: Messages stuck in processing

**Check visibility timeout**:
```bash
# List messages (including invisible)
az storage queue peek --name resume-analysis --account-name cvanalyzerdevs4b3 --auth-mode login
```

**Solution**: Increase visibility timeout to 10 minutes if processing takes longer

### Issue: High poison queue depth

**Manual review**:
```bash
# View poison messages
az storage queue peek --name resume-analysis-poison --account-name cvanalyzerdevs4b3 --num-messages 10 --auth-mode login
```

**Common causes**:
- Invalid resume format (corrupted PDF/DOCX)
- Document Intelligence API failure
- GPT-4o rate limit exceeded
- Database connection timeout

---

## Rollback Plan

1. Stop BackgroundService: Remove `AddHostedService<ResumeAnalysisWorker>()`
2. Drain queue: Process remaining messages manually or clear queue
3. Revert DI changes: Remove queue service registration
4. No data loss: Queue messages can be reprocessed after fix

---

## Next Steps

After Task 3 completion:
- **Task 4**: API Updates (refactor upload handler, add status endpoint)
- **Task 5**: Frontend (status polling, loading UI, candidate info display)
