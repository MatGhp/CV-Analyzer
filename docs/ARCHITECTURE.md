# Architecture Guide - CV Analyzer

**Last Updated:** November 7, 2025

---

## Table of Contents

- [System Overview](#system-overview)
- [Backend Architecture (.NET)](#backend-architecture-net)
- [Frontend Architecture (Angular)](#frontend-architecture-angular)
- [Infrastructure Architecture (Azure)](#infrastructure-architecture-azure)
- [Data Flow](#data-flow)
- [Security Architecture](#security-architecture)

---

## System Overview

CV Analyzer is a two-service application for resume analysis using AI:

- **Frontend**: Angular 20 SPA (served by nginx)
- **Backend**: .NET 9 API + integrated Agent Service (Microsoft Agent Framework + Azure OpenAI)
- **Database**: Azure SQL Database
- **Infrastructure**: Azure Container Apps + Azure OpenAI (via AI Foundry / Azure OpenAI resource)

### Communication Flow

```
User → Frontend (Angular/nginx)
  ↓ (Internal DNS)
  Backend API + AgentService (.NET 9)
  ↓
  Microsoft Agent Framework (C#)
  ↓
  Azure OpenAI (GPT-4o family)
  ↓
  Structured Resume Analysis JSON
```

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

### Technology Stack

- **.NET 9** (LTS)
- **MediatR 13**: CQRS
- **FluentValidation 8.7**: Request validation
- **Entity Framework Core 9**: ORM
- **Azure.Identity**: Managed identity authentication
- **Serilog 9**: Structured logging
- **xUnit + NSubstitute**: Testing

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

- `GET /api/health` - Health check
- `POST /api/resumes` - Upload resume
- `GET /api/resumes/{id}` - Get resume with suggestions

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
