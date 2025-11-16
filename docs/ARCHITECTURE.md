# Architecture Guide - CV Analyzer

**Last Updated:** November 7, 2025

---

## Table of Contents

- [System Overview](#system-overview)
- [Architecture Evolution](#architecture-evolution)
- [Backend Architecture (.NET)](#backend-architecture-net)
- [Frontend Architecture (Angular)](#frontend-architecture-angular)
- [Infrastructure Architecture (Azure)](#infrastructure-architecture-azure)
- [Data Flow](#data-flow)
- [Security Architecture](#security-architecture)

---

## Architecture Evolution

### Current State (v1.0)
The application uses a **queue-based background processing architecture** with Azure Storage Queues and a custom `ResumeAnalysisWorker` BackgroundService. This provides reliable async processing but requires manual state management.

### Future State (v2.0 - Planned)
Migration to **Microsoft Agent Framework Durable Agents** will enable:
- 🔄 Stateful multi-turn conversations (iterative resume refinement)
- 🤝 Multi-agent orchestrations (specialized agents for different tasks)
- 💪 Fault-tolerant workflows with automatic checkpointing
- 🔍 Visual debugging via Durable Task Scheduler dashboard
- ⚡ Serverless scaling (Azure Functions Flex Consumption)
- 💰 30-40% cost reduction

**See**: [Durable Agents Roadmap](DURABLE_AGENTS_ROADMAP.md) for detailed migration plan (3-4 week effort).

---

## System Overview

CV Analyzer is a **three-tier application** for AI-powered resume analysis:

### Components
- **Frontend**: Angular 20 SPA (zoneless architecture, standalone components)
- **Backend**: .NET 10 API with Clean Architecture + CQRS pattern
- **AI Service**: Integrated AgentService using Azure.AI.OpenAI SDK (not Microsoft Agent Framework yet)
- **Database**: Azure SQL Database (EF Core)
- **Queue**: Azure Storage Queues (async background processing)
- **Blob Storage**: Azure Storage (resume file storage)
- **AI Model**: Azure OpenAI GPT-4o via Azure AI Foundry

### Architecture Pattern
**Current (v1.0)**: Queue-based async processing with custom BackgroundService worker

**Planned (v2.0)**: Durable Agents with stateful orchestrations (see [Durable Agents Roadmap](DURABLE_AGENTS_ROADMAP.md))

### Communication Flow

```
User → Frontend (Angular SPA)
  ↓ (HTTP: /api/*)
  Backend API (.NET 9 with Clean Architecture)
  ↓
  Queue Message (Azure Storage Queue)
  ↓
  Background Worker (BackgroundService)
  ↓
  Document Intelligence (PDF → Text extraction)
  ↓
  AgentService (Azure.AI.OpenAI SDK)
  ↓
  Azure OpenAI GPT-4o (Function Calling)
  ↓
  Structured Resume Analysis JSON
  ↓
  Database Update (EF Core)
  ↓
  Frontend Polls Status → Displays Results
```

**Key Design Decisions**:
- **Async Processing**: Long-running AI analysis handled via queue + background worker
- **Function Calling**: Azure OpenAI responds with structured JSON via function call (not text content)
- **Text Extraction**: Azure Document Intelligence extracts text from PDFs before AI analysis
- **Status Polling**: Frontend polls `/api/resumes/{id}/status` endpoint for progress

---

## Backend Architecture (.NET)

### Clean Architecture Layers

```
API → Infrastructure → Application → Domain
```

**Domain (Core Business Logic)**:
- Zero external dependencies
- Entities: `Resume`, `Suggestion`
- Exceptions: `NotFoundException`, `ValidationException`

**Application (Use Cases)**:
- Depends only on Domain
- **MediatR**: CQRS pattern
- **FluentValidation**: Input validation
- Features organized by use case (Commands/Queries)

**Infrastructure (External Concerns)**:
- Depends on Application + Domain
- **Entity Framework Core**: Data access
- **Azure Services**: AI Foundry integration
- Persistence: `ApplicationDbContext`

**API (Presentation)**:
- Depends on all layers
- **ASP.NET Core 9**: Web API
- **Serilog**: Structured logging
- **Swagger**: API documentation
- Controllers: Thin delegates to MediatR

### Key Design Patterns

1. **CQRS**: Commands (write) and Queries (read) separated
2. **Repository Pattern**: `IApplicationDbContext` abstraction
3. **Validation Pipeline**: Automatic FluentValidation via MediatR behavior
4. **Exception Handling**: Global middleware transforms exceptions to HTTP responses
5. **Background Processing**: Queue-based async processing with BackgroundService
6. **Function Calling**: Azure OpenAI structured outputs via deprecated Functions API

### Background Processing Architecture

**Components**:
- `IResumeQueueService`: Enqueues resume analysis messages to Azure Storage Queue
- `ResumeAnalysisWorker`: BackgroundService that polls queue every 30 seconds
- `ResumeAnalysisOrchestrator`: Coordinates text extraction and AI analysis
- `IAIResumeAnalyzerService`: Wraps AgentService for dependency injection

**Flow**:
1. Upload handler enqueues message: `{ ResumeId, UserId }`
2. Worker polls queue, receives message with visibility timeout (60s)
3. Orchestrator extracts text via Document Intelligence
4. Orchestrator calls AgentService for AI analysis
5. Results saved to database, message deleted from queue
6. On error: Message becomes visible again (max 5 retries)

**Retry Strategy**:
- Max retries: 5
- Visibility timeout: 60 seconds
- Exponential backoff via Azure Storage Queue built-in mechanism
- Failed messages moved to poison queue after max retries

### Technology Stack

- **.NET 9** (LTS)
- **MediatR 13**: CQRS
- **FluentValidation 8.7**: Request validation
- **Entity Framework Core 9**: ORM
- **Azure.AI.OpenAI 2.1.0**: Azure OpenAI SDK (function calling)
- **Azure.AI.DocumentIntelligence**: PDF text extraction
- **Azure.Storage.Queues**: Background job queue
- **Azure.Identity**: Managed identity authentication
- **Serilog 9**: Structured logging
- **xUnit + NSubstitute**: Testing

### AgentService Implementation

**Location**: `backend/src/CVAnalyzer.AgentService/`

**Key Components**:
- `ResumeAnalysisAgent`: Core AI agent using Azure.AI.OpenAI SDK
- `AgentServiceOptions`: Configuration (Endpoint, Deployment, ApiKey, Temperature, TopP)
- `ResumeAnalysisRequest/Response`: Domain models for analysis

**Function Calling Pattern**:
```csharp
var options = new ChatCompletionsOptions {
    DeploymentName = "gpt-4o",
    Functions = { new FunctionDefinition {
        Name = "resume_analysis",
        Parameters = BinaryData.FromString(JsonSchema)
    }}
};

// Response comes as FunctionCall, not Content
var message = completion.Value.Choices.First().Message;
string jsonPayload = message.FunctionCall.Arguments;
var result = JsonSerializer.Deserialize<AgentResponse>(jsonPayload);
```

**Why Function Calling?**
- Guarantees structured JSON output (vs unreliable text parsing)
- Built-in schema validation by Azure OpenAI
- Better token efficiency than "respond with JSON" prompts

### Document Intelligence Integration

**Purpose**: Extract text from uploaded PDF resumes before AI analysis

**Implementation**: `CVAnalyzer.Infrastructure/Services/DocumentIntelligenceService.cs`

**Process**:
1. Resume PDF uploaded to Azure Blob Storage
2. Worker retrieves blob URL from database
3. Document Intelligence analyzes document via URL
4. Text content extracted (with page numbers, confidence scores)
5. Extracted text passed to AgentService for AI analysis

**Key Methods**:
```csharp
public async Task<string> ExtractTextFromPdfAsync(string blobUrl) {
    var operation = await _client.AnalyzeDocumentFromUriAsync(
        WaitUntil.Completed, 
        "prebuilt-read",  // Built-in read model
        new Uri(blobUrl)
    );
    
    var result = operation.Value;
    return string.Join("\n", result.Pages.SelectMany(p => p.Lines.Select(l => l.Content)));
}
```

**Output Example**:
```
Extracted 6627 characters from 2 pages in document https://cvanalyzerdevs4b3.blob.core.windows.net/resumes/...
```

### Database Schema

**Resume Table**:
- `Id` (Guid, PK)
- `UserId` (string, indexed)
- `FileName` (string)
- `BlobStorageUrl` (string)
- `OriginalContent` (string)
- `OptimizedContent` (string, nullable)
- `Status` (string)
- `Score` (double, nullable)
- `CreatedAt`, `UpdatedAt` (DateTime)

**Suggestion Table**:
- `Id` (Guid, PK)
- `ResumeId` (Guid, FK, indexed)
- `Category` (string)
- `Description` (string)
- `Priority` (int)
- `CreatedAt`, `UpdatedAt` (DateTime)

### API Endpoints

**Health & Status**:
- `GET /api/health` - Health check

**Resume Management**:
- `POST /api/resumes/upload` - Upload resume (multipart/form-data)
  - Returns: `{ id: "<resume-guid>" }` with 202 Accepted
- `GET /api/resumes/{id}` - Get resume details with suggestions
- `GET /api/resumes/{id}/status` - Poll analysis status
  - Returns: `{ status: "pending|processing|completed|failed", progress: 0-100 }`
- `GET /api/resumes/{id}/analysis` - Get full analysis results
  - Returns: Score, optimized content, candidate info, suggestions

**Request/Response Models**:
```csharp
// Upload
UploadResumeCommand { string UserId, string FileName, Stream FileStream }

// Analysis Result
ResumeAnalysisResponse {
  int Score,              // 0-100 ATS score
  string OptimizedContent,
  CandidateInfoDto CandidateInfo,
  ResumeSuggestion[] Suggestions,
  Dictionary<string, string> Metadata
}
```

---

## Frontend Architecture (Angular)

### Modern Angular 20 Features

- **Zoneless Architecture**: No zone.js dependency
- **Standalone Components**: No NgModules
- **Signals**: Reactive state management (`signal()`, `computed()`, `effect()`)
- **Lazy Loading**: Route-based code splitting

### Folder Structure

```
src/app/
├── core/                    # Singleton services (app-wide)
│   ├── guards/             # Route guards (auth, permissions)
│   ├── interceptors/       # HTTP interceptors (API, auth, error)
│   ├── models/             # Domain models and interfaces
│   └── services/           # Singleton services (API clients)
├── features/               # Feature modules (lazy-loaded)
│   ├── resume-upload/
│   └── resume-analysis/
└── shared/                 # Reusable components/directives/pipes
    ├── components/
    ├── directives/
    └── pipes/
```

### Key Technologies

- **Angular 20**: Latest with zoneless change detection
- **TypeScript (Strict)**: Enhanced type safety
- **SCSS**: Component-scoped styles
- **RxJS**: Async operations and HTTP calls
- **HttpClient**: API communication
- **Functional Interceptors**: Modern HTTP interception

### State Management

**Signals Pattern**:
```typescript
// Component state
data = signal<Resume[]>([]);
filteredData = computed(() => this.data().filter(d => d.active));

// Side effects
effect(() => {
  console.log('Data changed:', this.data());
});
```

### Service Pattern

```typescript
@Injectable({ providedIn: 'root' })
export class ResumeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/resumes`;
  
  uploadResume(request: UploadResumeRequest): Observable<{ id: string }> {
    const formData = new FormData();
    formData.append('file', request.file);
    return this.http.post<{ id: string }>(`${this.apiUrl}/upload`, formData);
  }
}
```

### Deployment

- **Multi-stage Docker build**: Node 20 → nginx 1.25-alpine
- **Static assets**: Served by nginx
- **API proxy**: `/api/*` → Backend via internal DNS
- **Health check**: `/health` endpoint

---

## Infrastructure Architecture (Azure)

### Azure Resources

**Resource Group** (`rg-cvanalyzer-{env}`):
- Container for all environment resources

**Container Registry** (`acrcvanalyzer{env}`):
- Docker images for frontend + API
- Basic SKU
- System-assigned identity for authentication

**AI Foundry** (`aih-cvanalyzer-{env}`):
- AI Hub + AI Project
- GPT-4o deployment
- Managed identity access from API

**SQL Database**:
- Server: `sql-cvanalyzer-{env}`
- Database: `sqldb-cvanalyzer-{env}`
- Standard S0 tier
- Connection string in environment variables

**Container Apps Environment** (`cae-cvanalyzer-{env}`):
- Consumption-based plan
- Internal DNS for service-to-service communication
- Log Analytics integration

**Container Apps**:
1. **Frontend** (`ca-cvanalyzer-frontend`):
   - nginx + Angular
   - 0.5 CPU, 1GB RAM
   - Auto-scale: 1-3 replicas
   - Port 80

2. **API** (`ca-cvanalyzer-api`):
   - .NET 9 + Agent Framework
   - 1 CPU, 2GB RAM
   - Auto-scale: 1-5 replicas
   - Port 8080

### Internal DNS Architecture

**Problem**: Multi-environment service communication

**Solution**: Container Apps Internal DNS

```nginx
# frontend/nginx.conf (same in all environments)
location /api/ {
    proxy_pass http://ca-cvanalyzer-api:8080/api/;
}
```

**How it works**:
- Apps in same Container Apps Environment auto-resolve names
- Format: `http://{app-name}:{port}`
- Azure provides environment-scoped DNS
- Same configuration works across dev/test/prod

**Benefits**:
- ✅ Zero configuration
- ✅ No environment variables needed
- ✅ Same Docker image everywhere
- ✅ Faster (internal communication)
- ✅ Secure (traffic stays in environment)

### Security Architecture

**Managed Identities**:
- System-assigned for both apps
- ACR Pull role (container image access)
- Azure AI Developer role (API → AI Foundry)
- No credentials in code

**Network Security**:
- Internal DNS for service communication
- HTTPS enforced (minimum TLS 1.2)
- SQL: Private endpoint (prod), Azure services access (dev/test)

**Secrets Management**:
- Connection strings in Container App environment variables
- Sensitive Terraform variables marked `sensitive = true`
- No secrets in container images

**Resource Locks** (Production):
- `CanNotDelete` on resource group
- Prevents accidental deletion

---

## Data Flow

### Resume Upload Flow

1. **User uploads file** (Frontend)
2. **FormData POST** → `/api/resumes` (via internal DNS)
3. **Validation** (FluentValidation)
4. **Handler creates Resume entity** (Application layer)
5. **EF Core saves to SQL** (Infrastructure layer)
6. **Agent Framework analysis** (AgentService → AI Foundry)
7. **Response** → Frontend

### Internal Communication

```
Frontend Container
  ↓ (HTTP: ca-cvanalyzer-api:8080)
API Container
  ↓ (Managed Identity)
AI Foundry (GPT-4o)
  ↓
Analysis Results
  ↓ (EF Core)
SQL Database
```

### External Access

```
Internet
  ↓ (HTTPS)
Container Apps Public Endpoint
  ↓
Frontend: ca-cvanalyzer-frontend.{random}.swedencentral.azurecontainerapps.io
API: ca-cvanalyzer-api.{random}.swedencentral.azurecontainerapps.io
```

---

## Security Architecture

### Authentication & Authorization

- **Frontend**: (Future) Azure AD B2C integration
- **API**: ASP.NET Core authentication middleware
- **Azure Resources**: Managed identity (passwordless)

### Input Validation

```csharp
// Command validation
public class UploadResumeCommandValidator : AbstractValidator<UploadResumeCommand>
{
    public UploadResumeCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .Must(BeValidFileType).WithMessage("Only PDF files")
            .Must(BeValidFileSize).WithMessage("Max 10MB");
    }
}
```

### Error Handling

```csharp
// Global exception middleware
catch (ValidationException ex)
{
    return BadRequest(new { message, errors });
}
catch (NotFoundException ex)
{
    return NotFound(new { message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unhandled exception");
    return StatusCode(500, new { message = "Internal server error" });
}
```

### Secrets Management

- **Development**: Local configuration or User Secrets
- **Azure**: Environment variables in Container Apps
- **SQL Password**: Terraform variable (never committed)
- **AI Keys**: Managed identity (no keys needed)

### Best Practices Implemented

- ✅ Clean Architecture (separation of concerns)
- ✅ SOLID principles
- ✅ Dependency Injection
- ✅ Async/await for scalability
- ✅ Structured logging
- ✅ Input validation (all requests)
- ✅ Infrastructure as Code (Terraform)
- ✅ Container security (non-root users)
- ✅ Managed identities (passwordless)
- ✅ Health checks
- ✅ Multi-stage Docker builds (minimal images)

---

## Development Workflow

### Local Development

**Backend**:
```bash
cd backend
dotnet restore
dotnet build
dotnet test
cd src/CVAnalyzer.API
dotnet run
```

**Frontend**:
```bash
cd frontend
npm install
npm start  # http://localhost:4200
```

**Full Stack (Docker)**:
```bash
docker-compose up -d
# Frontend: http://localhost:4200
# API: http://localhost:5000
# SQL: localhost:1433
```

### Azure Deployment

**Infrastructure**:
```bash
cd terraform
terraform init
terraform plan -var-file="environments/dev.tfvars"
terraform apply -var-file="environments/dev.tfvars"
```

**Application** (via GitHub Actions):
- Push to main → CI/CD pipeline
- Build Docker images
- Push to ACR
- Deploy to Container Apps
- Health checks

---

## Project Statistics

- **Total Projects**: 6 (4 source + 2 test)
- **Backend Files**: 29 C# files
- **Frontend**: Angular 20 with signals
- **Infrastructure**: Terraform (modular)
- **Test Coverage**: Unit + Integration tests
- **Docker Images**: Multi-stage builds (Alpine base ~180MB)

---

## Related Documentation

- **Security**: `docs/SECURITY.md` - Security best practices
- **DevOps**: `docs/DEVOPS.md` - CI/CD and deployment
- **Terraform**: `docs/TERRAFORM.md` - Infrastructure details
- **Copilot Instructions**: `.github/copilot-instructions.md` - AI coding guidelines

---

**For detailed implementation guidance, refer to the related documentation above.**
