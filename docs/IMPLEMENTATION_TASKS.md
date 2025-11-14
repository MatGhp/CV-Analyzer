# CV Analyzer Implementation Tasks

**Document Version**: 1.0  
**Date**: November 13, 2025  
**Total Tasks**: 10 (6-8 days estimated)

---

## Task 1: Infrastructure Setup - Azure Resources

**Objective**: Provision all required Azure resources for CV processing feature.

**Scope**:
- Deploy Azure Document Intelligence (FormRecognizer, S0 tier)
- Configure Azure Blob Storage with private 'resumes' container
- Configure managed identity for Container Apps
- Update Container Apps environment variables

**Deliverables**:
- Terraform module: `terraform/modules/ai-foundry/main.tf` updated with Document Intelligence resource
- Storage account with 30-day retention policy
- Environment variables configured:
  - `AzureStorage:ConnectionString`
  - `AzureStorage:QueueName=resume-analysis`
  - `DocumentIntelligence:Endpoint`
  - `DocumentIntelligence:ApiKey`

**Acceptance Criteria**:
- [ ] Document Intelligence resource deployed and accessible
- [ ] Blob Storage container created and private
- [ ] Managed identity has Cognitive Services User role
- [ ] Environment variables set in Container Apps
- [ ] Storage Queue auto-creates on app startup

**Estimated Time**: 1 day

---

## Task 2: Domain & Database Schema Updates

**Objective**: Add domain entities and database schema for CV processing.

**Scope**:
- Create `CandidateInfo` entity with navigation to `Resume`
- Update `Resume` entity with new properties
- Add `ResumeStatus` enum
- Create EF Core migration
- Add JSON value converter for Skills list

**Deliverables**:
- `backend/src/CVAnalyzer.Domain/Entities/CandidateInfo.cs`
- `backend/src/CVAnalyzer.Domain/Common/ResumeStatus.cs` (enum)
- Updated `Resume.cs` with:
  - `BlobUrlWithSas` property
  - `Status` property (ResumeStatus enum)
  - `UpdatedAt` property
- Migration: `AddCVProcessingFeature`
- `ApplicationDbContext` updated with:
  - `CandidateInfo` DbSet
  - JSON value converter configuration for Skills

**Acceptance Criteria**:
- [ ] CandidateInfo entity created with all required fields
- [ ] Resume entity updated with Status, BlobUrlWithSas, UpdatedAt
- [ ] ResumeStatus enum: Pending, Processing, Analyzed, Failed
- [ ] EF Core migration generated and applied
- [ ] Skills list serializes to JSON in database
- [ ] Foreign key relationship: CandidateInfo.ResumeId → Resume.Id

**Estimated Time**: 0.5 day

---

## Task 3: Infrastructure Services - Storage & Document Intelligence

**Objective**: Implement Azure service wrappers for blob storage, Document Intelligence, and queue.

**Scope**:
- Implement `BlobStorageService` with SAS token generation
- Implement `DocumentIntelligenceService` for text extraction
- Implement `ResumeQueueService` wrapper
- Update `DependencyInjection.cs` with service registrations

**Deliverables**:
- `backend/src/CVAnalyzer.Infrastructure/Services/BlobStorageService.cs`
  - `UploadFileAsync(Stream, string)` → Returns blob URL + SAS URL
  - `GenerateSasUrlAsync(BlobClient)` → Uses user delegation key
  - `DeleteFileAsync(string)` → Cleanup method
- `backend/src/CVAnalyzer.Infrastructure/Services/DocumentIntelligenceService.cs`
  - `ExtractTextFromDocumentAsync(string blobUrlWithSas)` → Returns extracted text
  - Uses prebuilt-read model
  - Handles RequestFailedException
- `backend/src/CVAnalyzer.Infrastructure/Services/ResumeQueueService.cs`
  - `SendMessageAsync(ResumeAnalysisMessage)` → Sends to queue
  - `ReceiveMessagesAsync(int maxMessages)` → Polls queue
- `backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs` updated with:
  - BlobServiceClient registration
  - QueueClient registration with CreateIfNotExists()
  - DocumentAnalysisClient registration
  - Service implementations (Scoped/Singleton as appropriate)

**Acceptance Criteria**:
- [ ] BlobStorageService uploads files and generates 24h SAS tokens
- [ ] SAS tokens use user delegation key (works with managed identity)
- [ ] DocumentIntelligenceService extracts text from PDF/DOCX
- [ ] ResumeQueueService sends/receives queue messages
- [ ] Queue auto-creates on first use (resume-analysis + poison queue)
- [ ] All services registered in DI container

**Estimated Time**: 1 day

---

## Task 4: Background Worker - Two-Stage AI Pipeline

**Objective**: Implement BackgroundService to process resumes asynchronously.

**Scope**:
- Create `ResumeAnalysisWorker` as BackgroundService
- Implement two-stage pipeline: Document Intelligence → GPT-4o
- Add transaction boundaries with status rollback
- Configure visibility timeout retry logic
- Implement poison queue handling

**Deliverables**:
- `backend/src/CVAnalyzer.Infrastructure/Services/ResumeAnalysisWorker.cs`
  - Polls queue every 2 seconds
  - Stage 1: Extract text via DocumentIntelligenceService
  - Stage 2: Analyze text via ResumeAnalyzerAgent
  - Updates Resume status: Pending → Processing → Analyzed/Failed
  - Try-catch with status rollback on errors
  - Moves messages with DequeueCount > 5 to poison queue
- Updated `ResumeAnalysisAgent.cs`:
  - Method signature: `AnalyzeResumeAsync(string extractedText, string userId)`
  - Uses `CompleteAsync()` not `CompleteAsync<T>()`
  - Manual JSON deserialization
  - Accepts extracted text instead of blob URL

**Acceptance Criteria**:
- [ ] Worker starts automatically with application
- [ ] Polls queue with 2-second interval
- [ ] Extracts text using Document Intelligence
- [ ] Analyzes text using GPT-4o
- [ ] Updates database with results
- [ ] Status rollback on failure (Processing → Failed)
- [ ] Visibility timeout: 5 minutes
- [ ] Max retries: 5 attempts
- [ ] Failed messages moved to poison queue
- [ ] Message deleted after successful processing

**Estimated Time**: 1.5 days

---

## Task 5: Application Layer - CQRS Commands & Queries

**Objective**: Update application logic for async resume processing.

**Scope**:
- Refactor `UploadResumeCommandHandler` for queue pattern
- Add file validation
- Create `GetResumeStatusQuery` for polling
- Update response DTOs

**Deliverables**:
- Updated `backend/src/CVAnalyzer.Application/Features/Resumes/Commands/UploadResumeCommandHandler.cs`:
  - Upload file to blob storage
  - Generate SAS token
  - Create Resume entity with Status=Pending
  - Send message to queue
  - Return 202 Accepted with resumeId
- Updated `UploadResumeCommandValidator.cs`:
  - MaxFileSizeBytes: 10MB
  - AllowedExtensions: .pdf, .docx
  - Optional: Magic bytes validation
- `backend/src/CVAnalyzer.Application/Features/Resumes/Queries/GetResumeStatusQuery.cs`:
  - Returns: { resumeId, status, candidateInfo?, createdAt, updatedAt }
  - Includes CandidateInfo when status=Analyzed
- Response DTOs:
  - `UploadResumeResponse` (resumeId, status, message)
  - `ResumeStatusResponse` (status, candidateInfo, timestamps)

**Acceptance Criteria**:
- [ ] Upload returns 202 Accepted immediately
- [ ] Resume created with Status=Pending
- [ ] Queue message sent with resumeId and userId
- [ ] File validation rejects files >10MB
- [ ] File validation rejects non-PDF/DOCX files
- [ ] GetResumeStatusQuery returns current status
- [ ] CandidateInfo included when status=Analyzed

**Estimated Time**: 1 day

---

## Task 6: API Layer - Controllers & Configuration

**Objective**: Update API endpoints and configuration for async processing.

**Scope**:
- Update `ResumesController` with async endpoints
- Add configuration to appsettings.json
- Update API documentation

**Deliverables**:
- Updated `backend/src/CVAnalyzer.API/Controllers/ResumesController.cs`:
  - `POST /api/resumes/upload` → Returns 202 Accepted
  - `GET /api/resumes/{id}/status` → Returns current status
  - `GET /api/resumes/{id}` → Returns full resume with analysis (when complete)
- Updated `appsettings.json`:
  ```json
  {
    "AzureStorage": {
      "ConnectionString": "",
      "ContainerName": "resumes",
      "QueueName": "resume-analysis"
    },
    "DocumentIntelligence": {
      "Endpoint": "",
      "ApiKey": ""
    },
    "FileUpload": {
      "MaxFileSizeBytes": 10485760,
      "AllowedExtensions": [".pdf", ".docx"]
    },
    "BackgroundWorker": {
      "PollingIntervalSeconds": 2,
      "MaxMessagesPerPoll": 10,
      "VisibilityTimeoutMinutes": 5,
      "MaxRetryCount": 5
    }
  }
  ```

**Acceptance Criteria**:
- [ ] Upload endpoint returns 202 Accepted
- [ ] Status endpoint returns real-time processing status
- [ ] Full resume endpoint works for completed analyses
- [ ] Configuration sections added
- [ ] Swagger documentation updated

**Estimated Time**: 0.5 day

---

## Task 7: Frontend - Models & Services

**Objective**: Add TypeScript models and service methods for async workflow.

**Scope**:
- Update resume models
- Implement status polling in ResumeService
- Add error handling

**Deliverables**:
- Updated `frontend/src/app/core/models/resume.model.ts`:
  ```typescript
  export enum ResumeStatus {
    Pending = 0,
    Processing = 1,
    Analyzed = 2,
    Failed = 3
  }
  
  export interface UploadResponse {
    resumeId: string;
    status: ResumeStatus;
    message: string;
  }
  
  export interface ResumeStatusResponse {
    resumeId: string;
    status: ResumeStatus;
    candidateInfo?: CandidateInfo;
    createdAt: string;
    updatedAt: string;
  }
  ```
- Updated `frontend/src/app/core/services/resume.service.ts`:
  - `uploadResume(file, userId)` → Returns UploadResponse
  - `getResumeStatus(id)` → Returns ResumeStatusResponse
  - `pollResumeStatus(id)` → Observable with interval(2000) + takeUntilDestroyed()
  - Polling stops when status is Analyzed or Failed

**Acceptance Criteria**:
- [ ] ResumeStatus enum matches backend
- [ ] UploadResponse interface defined
- [ ] ResumeStatusResponse interface defined
- [ ] getResumeStatus() method implemented
- [ ] pollResumeStatus() uses interval(2000)
- [ ] takeUntilDestroyed() prevents memory leaks
- [ ] Polling stops on terminal status

**Estimated Time**: 0.5 day

---

## Task 8: Frontend - Components for Async UI

**Objective**: Update components to handle async workflow with loading states.

**Scope**:
- Update upload component for 202 Accepted response
- Update analysis component with progress UI
- Create CandidateInfoCardComponent

**Deliverables**:
- Updated `frontend/src/app/features/resume-upload/resume-upload.component.ts`:
  - Handle 202 Accepted response
  - Store resumeId
  - Navigate to analysis page
  - Show "Upload successful, analyzing..." message
- Updated `frontend/src/app/features/resume-analysis/resume-analysis.component.ts`:
  - Start polling on component init
  - Show progress bar during Processing
  - Two-stage spinner: "Extracting text..." → "Analyzing..."
  - Display results when Analyzed
  - Show error message when Failed
- `frontend/src/app/shared/components/candidate-info-card/candidate-info-card.component.ts`:
  - Display: name, email, phone, location
  - Display: skills as badges (Tailwind)
  - Display: years of experience, current job title, education
  - Responsive layout (mobile/tablet/desktop)

**Acceptance Criteria**:
- [ ] Upload redirects to analysis page
- [ ] Progress bar shows during Processing
- [ ] Two-stage loading indicator
- [ ] CandidateInfo displayed when complete
- [ ] Error handling for Failed status
- [ ] Responsive design works on all devices
- [ ] Memory leaks prevented (takeUntilDestroyed)

**Estimated Time**: 1.5 days

---

## Task 9: Testing - Unit & Integration Tests

**Objective**: Add comprehensive tests for new functionality.

**Scope**:
- Unit tests for services
- Integration tests for workflow
- Component tests for frontend

**Deliverables**:
- `backend/tests/CVAnalyzer.UnitTests/Services/BlobStorageServiceTests.cs`:
  - Test file upload
  - Test SAS token generation
  - Test file deletion
  - Mock BlobServiceClient
- `backend/tests/CVAnalyzer.UnitTests/Services/DocumentIntelligenceServiceTests.cs`:
  - Test text extraction
  - Test error handling
  - Mock DocumentAnalysisClient
- `backend/tests/CVAnalyzer.IntegrationTests/Features/ResumeUploadTests.cs`:
  - Test: Upload → Queue message created
  - Test: Worker processes message
  - Test: Database updated correctly
  - Test: Status endpoint returns correct data
- Frontend component tests:
  - Upload component tests
  - Analysis component tests
  - CandidateInfoCard tests

**Acceptance Criteria**:
- [ ] BlobStorageService unit tests pass
- [ ] DocumentIntelligenceService unit tests pass
- [ ] Integration test: Full workflow (upload → process → complete)
- [ ] Integration test: File validation
- [ ] Integration test: Error scenarios
- [ ] Frontend component tests pass
- [ ] Test coverage >70%

**Estimated Time**: 1 day

---

## Task 10: Deployment & Monitoring Setup

**Objective**: Deploy to dev environment and configure monitoring.

**Scope**:
- Apply Terraform changes
- Deploy updated application
- Configure Azure Monitor
- Verify end-to-end functionality

**Deliverables**:
- Terraform applied with Document Intelligence resource
- Container Apps updated with new environment variables
- Latest container images deployed
- Azure Monitor configured:
  - Queue metrics (depth, message age, dequeue count)
  - Document Intelligence metrics (calls, page count, latency)
  - Alerts for queue depth >100
  - Budget alert for Document Intelligence at $10/month
- Smoke tests passed:
  - Upload PDF resume
  - Verify text extraction
  - Verify GPT-4o analysis
  - Check database updates
  - Verify frontend displays results

**Acceptance Criteria**:
- [ ] Terraform apply successful (dev environment)
- [ ] All resources provisioned
- [ ] Container Apps running with new config
- [ ] Queue auto-creates on startup
- [ ] Azure Monitor dashboards configured
- [ ] Alerts set up and tested
- [ ] End-to-end smoke test passes
- [ ] PDF extraction accuracy verified
- [ ] DOCX extraction accuracy verified
- [ ] GPT-4o analysis quality verified

**Estimated Time**: 1 day

---

## Implementation Order & Dependencies

```
Task 1 (Infrastructure)
   ↓
Task 2 (Domain/Database)
   ↓
Task 3 (Services) ← Required by Task 4
   ↓
Task 4 (Background Worker) ← Depends on Task 3
   ↓
Task 5 (CQRS) ← Depends on Task 3
   ↓
Task 6 (API) ← Depends on Task 5
   ↓
Task 7 (Frontend Models) ← Depends on Task 6
   ↓
Task 8 (Frontend Components) ← Depends on Task 7
   ↓
Task 9 (Testing) ← Depends on Tasks 3-8
   ↓
Task 10 (Deployment) ← Depends on all previous tasks
```

---

## Key Technical Decisions Captured

1. **Two-Stage AI Pipeline**: Document Intelligence (extraction) → GPT-4o (analysis)
   - Reason: GPT-4o cannot read PDF/DOCX binary formats directly
   
2. **Azure Storage Queue + BackgroundService**: Native .NET async processing
   - Reason: Simpler than Hangfire, Container Apps compatible, persistent queue
   
3. **User Delegation Key SAS Tokens**: Works with managed identity
   - Reason: No account keys needed, better security
   
4. **Visibility Timeout Retry**: 5-minute timeout, 5 max retries
   - Reason: Handles transient failures, poison queue for permanent failures
   
5. **Agent Framework API**: `CompleteAsync()` + manual JSON deserialization
   - Reason: Correct API usage (generic method doesn't exist in current version)

---

## Rollback Strategy

- **Task 1-2**: Can roll back Terraform and database migration
- **Task 3-6**: Feature flag to disable async processing (fall back to sync)
- **Task 7-8**: Frontend changes can be reverted via Git
- **Task 9**: Tests don't affect production
- **Task 10**: Blue-green deployment for zero-downtime rollback

---

## Success Criteria

- [ ] Upload response time < 2 seconds (202 Accepted)
- [ ] Background processing time: 10-45 seconds (including extraction)
- [ ] Extraction accuracy: 90%+ for email, name, phone
- [ ] Analysis completion rate: 98%+ (with retries)
- [ ] No memory leaks in frontend (polling cleaned up)
- [ ] Cost stays within $57-77/month estimate
- [ ] All 10 tasks completed and verified
