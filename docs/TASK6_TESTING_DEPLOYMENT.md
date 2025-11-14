# Task 6: Testing & Deployment

**Estimated Time**: 1 day  
**Priority**: P0 (Required for production readiness)  
**Dependencies**: Tasks 2-5 (All implementation tasks must be complete)

---

## Overview

Comprehensive testing strategy and production deployment plan for the async CV processing system with Document Intelligence + GPT-4o pipeline.

---

## Prerequisites

✅ Tasks 2-5 completed:
- Backend core services (blob storage, Document Intelligence, database)
- Queue & background worker (two-stage AI pipeline)
- API updates (async upload, status endpoint)
- Frontend (status polling, candidate info display)

---

## Testing Strategy

### 1. Unit Tests

#### Backend Unit Tests

**Target Coverage**: >80% for business logic

**BlobStorageService Tests** (`backend/tests/CVAnalyzer.UnitTests/Services/BlobStorageServiceTests.cs`):
```csharp
[Fact]
public async Task UploadFileAsync_ValidStream_ReturnsBlobUrlWithSas()
{
    // Arrange
    var mockBlobClient = Substitute.For<BlobClient>();
    var mockContainerClient = Substitute.For<BlobContainerClient>();
    mockContainerClient.GetBlobClient(Arg.Any<string>()).Returns(mockBlobClient);
    
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
    var fileName = "test-resume.pdf";
    
    // Act
    var result = await _sut.UploadFileAsync(stream, fileName);
    
    // Assert
    Assert.NotNull(result.BlobUrl);
    Assert.NotNull(result.BlobUrlWithSas);
    Assert.Contains("?sv=", result.BlobUrlWithSas); // SAS token present
}

[Fact]
public async Task GenerateSasToken_ValidBlobName_Returns24HourToken()
{
    // Test SAS token generation with user delegation key
    var sasToken = await _sut.GenerateSasTokenAsync("test-blob.pdf");
    
    Assert.NotNull(sasToken);
    Assert.Contains("sv=", sasToken);
    Assert.Contains("se=", sasToken); // Expiry time
}
```

**DocumentIntelligenceService Tests** (`backend/tests/CVAnalyzer.UnitTests/Services/DocumentIntelligenceServiceTests.cs`):
```csharp
[Fact]
public async Task ExtractTextFromDocumentAsync_ValidPdf_ReturnsExtractedText()
{
    // Arrange
    var blobUrlWithSas = "https://storage.blob.core.windows.net/resumes/test.pdf?sv=...";
    
    // Act
    var extractedText = await _sut.ExtractTextFromDocumentAsync(blobUrlWithSas);
    
    // Assert
    Assert.NotEmpty(extractedText);
    Assert.Contains("Software Engineer", extractedText); // Expected content
}

[Fact]
public async Task ExtractTextFromDocumentAsync_InvalidUrl_ThrowsException()
{
    // Test error handling for invalid blob URLs
    await Assert.ThrowsAsync<Exception>(
        () => _sut.ExtractTextFromDocumentAsync("invalid-url"));
}
```

**ResumeQueueService Tests** (`backend/tests/CVAnalyzer.UnitTests/Services/ResumeQueueServiceTests.cs`):
```csharp
[Fact]
public async Task EnqueueResumeAnalysisAsync_ValidId_SendsMessage()
{
    // Arrange
    var resumeId = Guid.NewGuid();
    var userId = "test-user";
    
    // Act
    await _sut.EnqueueResumeAnalysisAsync(resumeId, userId);
    
    // Assert
    await _mockQueueClient.Received(1).SendMessageAsync(
        Arg.Is<string>(msg => msg.Contains(resumeId.ToString())));
}
```

**ValidationBehavior Tests** (`backend/tests/CVAnalyzer.UnitTests/Behaviors/ValidationBehaviorTests.cs`):
```csharp
[Fact]
public async Task Handle_InvalidCommand_ThrowsValidationException()
{
    // Test that file size > 10MB throws validation error
    var command = new UploadResumeCommand(
        new MemoryStream(new byte[11 * 1024 * 1024]), // 11MB
        "large-file.pdf",
        "user-123");
    
    await Assert.ThrowsAsync<ValidationException>(
        () => _mediator.Send(command));
}
```

#### Frontend Unit Tests

**ResumeService Tests** (`frontend/src/app/core/services/resume.service.spec.ts`):
```typescript
it('should upload resume and return 202 response', (done) => {
  const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
  const userId = 'user-123';
  
  service.uploadResume(file, userId).subscribe(response => {
    expect(response.status).toBe('pending');
    expect(response.resumeId).toBeTruthy();
    done();
  });
  
  const req = httpMock.expectOne(`${environment.apiUrl}/resumes/upload`);
  expect(req.request.method).toBe('POST');
  req.flush({ resumeId: '123', status: 'pending', message: 'Uploaded' });
});

it('should poll status until complete', (done) => {
  const resumeId = '123';
  let callCount = 0;
  
  service.pollResumeStatus(resumeId).subscribe(status => {
    callCount++;
    if (status.status === 'complete') {
      expect(callCount).toBeGreaterThanOrEqual(2);
      expect(status.progress).toBe(100);
      done();
    }
  });
  
  // Simulate polling responses
  tick(2000);
  httpMock.expectOne(`${environment.apiUrl}/resumes/${resumeId}/status`)
    .flush({ status: 'processing', progress: 50 });
  
  tick(2000);
  httpMock.expectOne(`${environment.apiUrl}/resumes/${resumeId}/status`)
    .flush({ status: 'complete', progress: 100 });
});
```

**CandidateInfoCardComponent Tests** (`frontend/src/app/shared/components/candidate-info-card/candidate-info-card.component.spec.ts`):
```typescript
it('should render all candidate info fields', () => {
  const candidateInfo: CandidateInfo = {
    fullName: 'John Doe',
    email: 'john@example.com',
    phone: '+1-555-0123',
    location: 'San Francisco, CA',
    skills: ['JavaScript', 'TypeScript', 'Angular'],
    yearsOfExperience: 5,
    currentJobTitle: 'Senior Engineer',
    education: 'BS Computer Science'
  };
  
  fixture.componentRef.setInput('candidateInfo', candidateInfo);
  fixture.detectChanges();
  
  expect(compiled.querySelector('.candidate-name')?.textContent).toBe('John Doe');
  expect(compiled.querySelector('.email')?.textContent).toContain('john@example.com');
  expect(compiled.querySelectorAll('.skill-badge').length).toBe(3);
});

it('should handle missing optional fields gracefully', () => {
  const candidateInfo: CandidateInfo = {
    fullName: 'Jane Doe',
    email: 'jane@example.com',
    skills: []
  };
  
  fixture.componentRef.setInput('candidateInfo', candidateInfo);
  fixture.detectChanges();
  
  expect(compiled.querySelector('.phone')).toBeNull();
  expect(compiled.querySelector('.location')).toBeNull();
  expect(compiled.querySelector('.skills-section')).toBeNull();
});
```

---

### 2. Integration Tests

**Full Async Workflow Test** (`backend/tests/CVAnalyzer.IntegrationTests/Features/Resumes/ResumeWorkflowIntegrationTests.cs`):

```csharp
[Fact]
public async Task FullAsyncWorkflow_UploadPollAndRetrieve_Success()
{
    // Arrange
    var samplePdfPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sample-resume.pdf");
    Assert.True(File.Exists(samplePdfPath), "Test PDF file not found");
    
    using var fileStream = File.OpenRead(samplePdfPath);
    var fileName = "sample-resume.pdf";
    var userId = "integration-test-user";

    // Act 1: Upload resume
    var uploadCommand = new UploadResumeCommand(fileStream, fileName, userId);
    var uploadResponse = await _mediator.Send(uploadCommand);

    Assert.Equal("pending", uploadResponse.Status);
    Assert.NotEqual(Guid.Empty, uploadResponse.ResumeId);
    
    _output.WriteLine($"Resume uploaded: {uploadResponse.ResumeId}");

    // Act 2: Poll status until complete (max 30 seconds)
    var statusQuery = new GetResumeStatusQuery(uploadResponse.ResumeId);
    var maxAttempts = 15; // 15 * 2s = 30s
    ResumeStatusResponse? statusResponse = null;

    for (int i = 0; i < maxAttempts; i++)
    {
        statusResponse = await _mediator.Send(statusQuery);
        _output.WriteLine($"Attempt {i + 1}: {statusResponse.Status} ({statusResponse.Progress}%)");
        
        if (statusResponse.Status == "complete")
            break;
        
        if (statusResponse.Status == "failed")
        {
            Assert.Fail($"Resume analysis failed: {statusResponse.ErrorMessage}");
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    Assert.NotNull(statusResponse);
    Assert.Equal("complete", statusResponse.Status);
    Assert.Equal(100, statusResponse.Progress);

    // Act 3: Retrieve full analysis
    var analysisQuery = new GetResumeByIdQuery(uploadResponse.ResumeId);
    var analysisResponse = await _mediator.Send(analysisQuery);

    // Assert - Candidate Info
    Assert.NotNull(analysisResponse.CandidateInfo);
    Assert.NotEmpty(analysisResponse.CandidateInfo.FullName);
    Assert.NotEmpty(analysisResponse.CandidateInfo.Email);
    Assert.Matches(@"^[\w\.-]+@[\w\.-]+\.\w+$", analysisResponse.CandidateInfo.Email); // Email regex
    Assert.NotEmpty(analysisResponse.CandidateInfo.Skills);
    Assert.True(analysisResponse.CandidateInfo.Skills.Count >= 3, "Expected at least 3 skills");
    
    // Assert - Score
    Assert.True(analysisResponse.Score > 0 && analysisResponse.Score <= 100);
    
    // Assert - Suggestions
    Assert.NotEmpty(analysisResponse.Suggestions);
    Assert.All(analysisResponse.Suggestions, s =>
    {
        Assert.NotEmpty(s.Category);
        Assert.NotEmpty(s.Description);
        Assert.InRange(s.Priority, 1, 5);
    });
    
    // Assert - Metadata
    Assert.NotNull(analysisResponse.AnalyzedAt);
    Assert.True(analysisResponse.AnalyzedAt > analysisResponse.UploadedAt);
    
    _output.WriteLine($"Analysis complete. Score: {analysisResponse.Score}/100");
    _output.WriteLine($"Candidate: {analysisResponse.CandidateInfo.FullName}");
    _output.WriteLine($"Skills: {string.Join(", ", analysisResponse.CandidateInfo.Skills)}");
}

[Fact]
public async Task UploadWorkflow_InvalidFileType_ReturnsValidationError()
{
    // Test validation for unsupported file types
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
    var command = new UploadResumeCommand(stream, "invalid.txt", "user-123");
    
    await Assert.ThrowsAsync<ValidationException>(() => _mediator.Send(command));
}

[Fact]
public async Task UploadWorkflow_FileTooLarge_ReturnsValidationError()
{
    // Test validation for file size > 10MB
    var largeStream = new MemoryStream(new byte[11 * 1024 * 1024]); // 11MB
    var command = new UploadResumeCommand(largeStream, "large.pdf", "user-123");
    
    await Assert.ThrowsAsync<ValidationException>(() => _mediator.Send(command));
}
```

**Document Intelligence Integration Test**:
```csharp
[Fact]
public async Task DocumentIntelligence_RealPdf_ExtractsTextAccurately()
{
    // Arrange
    var samplePdfPath = "TestData/sample-resume-john-doe.pdf";
    using var fileStream = File.OpenRead(samplePdfPath);
    
    // Upload to blob
    var uploadResult = await _blobService.UploadFileAsync(fileStream, "test-extract.pdf");
    
    // Act - Extract text
    var extractedText = await _docIntelService.ExtractTextFromDocumentAsync(uploadResult.BlobUrlWithSas);
    
    // Assert - Verify key information extracted
    Assert.Contains("John Doe", extractedText, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("john.doe@example.com", extractedText, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("Software Engineer", extractedText, StringComparison.OrdinalIgnoreCase);
    Assert.True(extractedText.Length > 500, "Extracted text too short");
}
```

---

### 3. End-to-End (E2E) Tests

**Playwright E2E Test** (`frontend/e2e/resume-workflow.spec.ts`):

```typescript
import { test, expect } from '@playwright/test';

test.describe('CV Analyzer Full Workflow', () => {
  test('should upload, poll status, and display analysis', async ({ page }) => {
    // Navigate to upload page
    await page.goto('http://localhost:4200/upload');
    
    // Upload file
    const fileInput = page.locator('input[type="file"]');
    await fileInput.setInputFiles('e2e/test-data/sample-resume.pdf');
    
    await expect(page.locator('.file-name')).toContainText('sample-resume.pdf');
    
    // Click upload button
    await page.locator('.upload-button').click();
    
    // Should navigate to analysis page
    await expect(page).toHaveURL(/\/analysis\/[a-f0-9-]{36}/);
    
    // Should show loading state
    await expect(page.locator('.loading-section')).toBeVisible();
    await expect(page.locator('.status-text')).toContainText(/pending|processing/i);
    
    // Wait for analysis to complete (max 30s)
    await expect(page.locator('.results-section')).toBeVisible({ timeout: 30000 });
    
    // Verify score displayed
    await expect(page.locator('.score-value')).toHaveText(/^\d{1,3}$/);
    
    // Verify candidate info card
    await expect(page.locator('.candidate-card')).toBeVisible();
    await expect(page.locator('.candidate-name')).not.toBeEmpty();
    await expect(page.locator('.email')).not.toBeEmpty();
    
    // Verify skills displayed
    const skillBadges = page.locator('.skill-badge');
    await expect(skillBadges).toHaveCount({ min: 3 });
    
    // Verify suggestions
    const suggestions = page.locator('.suggestion-card');
    await expect(suggestions).toHaveCount({ min: 1 });
  });

  test('should show error for invalid file type', async ({ page }) => {
    await page.goto('http://localhost:4200/upload');
    
    const fileInput = page.locator('input[type="file"]');
    await fileInput.setInputFiles('e2e/test-data/invalid.txt');
    
    await expect(page.locator('.error-alert')).toContainText(/only pdf and docx/i);
    await expect(page.locator('.upload-button')).toBeDisabled();
  });

  test('should handle analysis failure gracefully', async ({ page }) => {
    // Mock API to return failed status
    await page.route('**/api/resumes/*/status', route => {
      route.fulfill({
        status: 200,
        body: JSON.stringify({ 
          status: 'failed', 
          progress: 0, 
          errorMessage: 'Analysis failed. Please try again.' 
        })
      });
    });
    
    await page.goto('http://localhost:4200/analysis/test-id-123');
    
    await expect(page.locator('.error-section')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('.error-section')).toContainText(/analysis failed/i);
    await expect(page.locator('.retry-button')).toBeVisible();
  });
});
```

---

### 4. Performance Testing

**Load Test** (using Azure Load Testing or k6):

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';
import { SharedArray } from 'k6/data';

export let options = {
  stages: [
    { duration: '1m', target: 10 },  // Ramp-up to 10 users
    { duration: '3m', target: 50 },  // Peak load: 50 concurrent users
    { duration: '1m', target: 0 },   // Ramp-down
  ],
  thresholds: {
    'http_req_duration': ['p(95)<3000'], // 95% of requests < 3s
    'http_req_failed': ['rate<0.05'],     // Error rate < 5%
  },
};

const resumeFiles = new SharedArray('resumes', function () {
  return ['sample1.pdf', 'sample2.pdf', 'sample3.pdf'];
});

export default function () {
  const file = open(`./test-data/${resumeFiles[__VU % resumeFiles.length]}`, 'b');
  
  const formData = {
    file: http.file(file, resumeFiles[__VU % resumeFiles.length]),
    userId: `load-test-user-${__VU}`,
  };
  
  // Upload resume
  let uploadRes = http.post('http://localhost:5000/api/resumes/upload', formData);
  
  check(uploadRes, {
    'upload status 202': (r) => r.status === 202,
    'resumeId present': (r) => JSON.parse(r.body).resumeId !== undefined,
  });
  
  if (uploadRes.status === 202) {
    const resumeId = JSON.parse(uploadRes.body).resumeId;
    
    // Poll status until complete (max 30s)
    let status = 'pending';
    let attempts = 0;
    while (status !== 'complete' && status !== 'failed' && attempts < 15) {
      sleep(2);
      
      let statusRes = http.get(`http://localhost:5000/api/resumes/${resumeId}/status`);
      check(statusRes, { 'status check 200': (r) => r.status === 200 });
      
      status = JSON.parse(statusRes.body).status;
      attempts++;
    }
    
    check({ status }, {
      'analysis completed': (s) => s.status === 'complete',
    });
  }
  
  sleep(1);
}
```

---

## Monitoring & Observability

### Application Insights Queries

**Queue Depth Monitoring**:
```kusto
// Monitor Azure Storage Queue depth
customMetrics
| where name == "QueueDepth"
| where customDimensions.QueueName == "resume-analysis"
| summarize avg(value), max(value) by bin(timestamp, 5m)
| render timechart
```

**Processing Time Analysis**:
```kusto
// Average processing time per resume
traces
| where message contains "Resume analysis completed"
| parse message with * "ResumeId: " resumeId:guid " Duration: " duration:timespan
| summarize avg(duration), percentiles(duration, 50, 95, 99) by bin(timestamp, 1h)
| render timechart
```

**Error Rate Tracking**:
```kusto
// Track failed analyses
customEvents
| where name == "ResumeAnalysisFailed"
| summarize count() by tostring(customDimensions.ErrorType), bin(timestamp, 15m)
| render columnchart
```

**Document Intelligence API Calls**:
```kusto
// Monitor Document Intelligence usage
dependencies
| where target contains "cognitiveservices.azure.com"
| where name contains "FormRecognizer"
| summarize count(), avg(duration) by resultCode, bin(timestamp, 1h)
| render timechart
```

**GPT-4o Token Usage**:
```kusto
// Track AI token consumption
customMetrics
| where name == "TokensUsed"
| extend InputTokens = todouble(customDimensions.InputTokens)
| extend OutputTokens = todouble(customDimensions.OutputTokens)
| summarize sum(InputTokens), sum(OutputTokens) by bin(timestamp, 1d)
| render barchart
```

---

## Deployment Checklist

### Pre-Deployment Validation

- [ ] All unit tests pass (>80% coverage)
- [ ] All integration tests pass
- [ ] E2E tests pass locally
- [ ] Load test completed (50+ concurrent users)
- [ ] Security scan passed (no secrets in code)
- [ ] Code review approved
- [ ] Swagger documentation updated
- [ ] Environment variables verified

### Infrastructure Validation

- [ ] Azure Storage account provisioned (cvanalyzerdevs4b3)
- [ ] Blob container created (`resumes`, private access)
- [ ] Storage queues created (`resume-analysis`, `resume-analysis-poison`)
- [ ] Document Intelligence resource deployed (S0 tier)
- [ ] Document Intelligence API key/managed identity configured
- [ ] SQL database updated (migration applied)
- [ ] Container Apps environment configured
- [ ] Application Insights enabled
- [ ] Role assignments configured:
  - API → Storage Account (Storage Blob Data Contributor, Storage Queue Data Contributor)
  - API → Document Intelligence (Cognitive Services User)
- [ ] Lifecycle policy set (30-day blob retention)

### Deployment Steps (Azure Container Apps)

**1. Build and push Docker images**:
```bash
# Backend API
cd backend
docker build -t acrcvanalyzerdev.azurecr.io/cv-analyzer-api:latest -f Dockerfile .
docker push acrcvanalyzerdev.azurecr.io/cv-analyzer-api:latest

# Frontend
cd ../frontend
docker build -t acrcvanalyzerdev.azurecr.io/cv-analyzer-frontend:latest -f Dockerfile .
docker push acrcvanalyzerdev.azurecr.io/cv-analyzer-frontend:latest
```

**2. Update Container Apps**:
```bash
# Update API container app
az containerapp update \
  --name app-cvanalyzer-api-dev \
  --resource-group rg-cvanalyzer-dev \
  --image acrcvanalyzerdev.azurecr.io/cv-analyzer-api:latest \
  --set-env-vars \
    "AzureStorage__ConnectionString=secretref:storage-connection-string" \
    "AzureStorage__QueueName=resume-analysis" \
    "DocumentIntelligence__Endpoint=https://swedencentral.api.cognitive.microsoft.com/" \
    "DocumentIntelligence__ApiKey=secretref:docintel-api-key"

# Update frontend container app
az containerapp update \
  --name app-cvanalyzer-frontend-dev \
  --resource-group rg-cvanalyzer-dev \
  --image acrcvanalyzerdev.azurecr.io/cv-analyzer-frontend:latest
```

**3. Verify deployment**:
```bash
# Check API health
curl https://app-cvanalyzer-api-dev.azurecontainerapps.io/health

# Check frontend
curl https://app-cvanalyzer-frontend-dev.azurecontainerapps.io/health

# Verify background worker started
az containerapp logs show \
  --name app-cvanalyzer-api-dev \
  --resource-group rg-cvanalyzer-dev \
  --follow \
  --type console
# Look for: "ResumeAnalysisWorker started"
```

### Post-Deployment Validation

- [ ] Upload test resume via UI
- [ ] Verify blob uploaded to storage account
- [ ] Verify queue message sent
- [ ] Verify Document Intelligence text extraction
- [ ] Verify GPT-4o analysis completion
- [ ] Verify candidate info displayed in UI
- [ ] Check Application Insights logs
- [ ] Monitor queue depth (should be near zero)
- [ ] Test error scenarios (invalid file, large file)
- [ ] Verify retry logic (manually corrupt a message)
- [ ] Check poison queue (should be empty)

---

## Rollback Plan

### Level 1: Feature Flag Rollback (No Deployment)
```csharp
// Add feature flag in appsettings.json
{
  "Features": {
    "UseAsyncProcessing": false  // Fallback to sync
  }
}

// In DependencyInjection.cs
if (configuration.GetValue<bool>("Features:UseAsyncProcessing"))
{
    services.AddHostedService<ResumeAnalysisWorker>();
}
```

### Level 2: Container App Revision Rollback
```bash
# List revisions
az containerapp revision list \
  --name app-cvanalyzer-api-dev \
  --resource-group rg-cvanalyzer-dev

# Activate previous revision
az containerapp revision activate \
  --name app-cvanalyzer-api-dev \
  --resource-group rg-cvanalyzer-dev \
  --revision app-cvanalyzer-api-dev--<previous-revision-id>
```

### Level 3: Database Rollback
```bash
# Rollback to previous migration
cd backend/src/CVAnalyzer.Infrastructure
dotnet ef database update <PreviousMigrationName> --project . --startup-project ../CVAnalyzer.API
```

### Level 4: Full Infrastructure Rollback
```bash
# Revert Terraform changes
cd terraform
git checkout <previous-commit-hash>
terraform apply -var-file="environments/dev.tfvars"
```

**Data Safety**:
- Blob storage data persists (no data loss)
- Queue messages can be drained manually
- Database changes can be rolled back via EF migrations
- Poison queue isolates failed messages (no data loss)

---

## Cost Monitoring

### Azure Cost Alerts

**Set budget alert**:
```bash
az consumption budget create \
  --budget-name "cv-analyzer-monthly-budget" \
  --amount 100 \
  --time-grain Monthly \
  --start-date 2025-11-01 \
  --end-date 2026-12-31 \
  --resource-group rg-cvanalyzer-dev \
  --notification-enabled true \
  --notification-threshold 80 \
  --contact-emails admin@example.com
```

### Cost Tracking Query
```kusto
// Daily cost breakdown
AzureCosts
| where ResourceGroup == "rg-cvanalyzer-dev"
| summarize TotalCost = sum(Cost) by ServiceName, bin(Date, 1d)
| render columnchart
```

---

## Acceptance Criteria

- [ ] **Unit Tests**: >80% coverage, all tests pass
- [ ] **Integration Tests**: Full async workflow test passes
- [ ] **E2E Tests**: Upload → poll → display workflow works in browser
- [ ] **Performance**: Upload responds < 2s, background processing < 30s
- [ ] **Accuracy**: Candidate info extraction >90% accurate (validated against test dataset)
- [ ] **Reliability**: <2% error rate after retries
- [ ] **Monitoring**: Application Insights dashboards configured
- [ ] **Cost**: Monthly spend < $80 (verified in Azure Cost Management)
- [ ] **Security**: No secrets in code, SAS tokens used, input validation enforced
- [ ] **Documentation**: API docs updated, deployment guide written
- [ ] **Rollback**: Tested rollback procedure (feature flag + revision activation)

---

## Success Metrics

### Week 1 Post-Deployment
- [ ] Upload success rate >95%
- [ ] Average processing time <20 seconds
- [ ] Zero production incidents
- [ ] User feedback collected (5+ users)

### Month 1 Post-Deployment
- [ ] 100+ resumes analyzed
- [ ] Cost within budget ($57-77/month)
- [ ] Error rate <2%
- [ ] Candidate info extraction accuracy >90%

---

## Known Issues & Limitations

1. **Document Intelligence F0 Tier**: Limited to 500 pages/month (use S0 for production)
2. **GPT-4o Rate Limits**: 10K requests/minute (should be sufficient for initial launch)
3. **Status Polling**: Frontend polls every 2s (could optimize with SignalR if needed)
4. **File Size**: 10MB limit (Azure Storage supports up to 5TB per blob if needed)
5. **Poison Queue**: Manual review required for stuck messages (no auto-retry after 5 attempts)

---

## Next Steps

1. **Execute all tests** (unit, integration, E2E, load)
2. **Review test results** with stakeholders
3. **Deploy to dev environment** following checklist
4. **User acceptance testing** (5+ test users)
5. **Deploy to production** with staged rollout (10% → 50% → 100%)
6. **Monitor for 1 week** before full release
7. **Collect user feedback** and iterate

---

## Appendix: Test Data

### Sample Resume Files

**Location**: `backend/tests/CVAnalyzer.IntegrationTests/TestData/`

Required test files:
- `sample-resume-john-doe.pdf` - Standard 2-page resume
- `sample-resume-jane-smith.docx` - Microsoft Word resume
- `sample-resume-large.pdf` - 9.5MB file (near limit)
- `sample-resume-invalid.txt` - Invalid file type
- `sample-resume-corrupted.pdf` - Corrupted PDF for error testing

**Expected Extraction Results** (for validation):
```json
{
  "fullName": "John Doe",
  "email": "john.doe@example.com",
  "phone": "+1-555-0123",
  "location": "San Francisco, CA",
  "skills": ["JavaScript", "TypeScript", "Angular", "Node.js", "Azure"],
  "yearsOfExperience": 5,
  "currentJobTitle": "Senior Software Engineer",
  "education": "Bachelor of Science in Computer Science"
}
```

---

**Document Version**: 1.0  
**Date**: November 13, 2025  
**Status**: Ready for Execution  
**Estimated Effort**: 1 day  
**Dependencies**: Tasks 2-5 complete
