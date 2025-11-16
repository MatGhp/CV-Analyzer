## CV Analyzer — Copilot instructions for code changes

**Monorepo Structure**: This repository contains two main components - Angular Frontend and .NET Backend (Clean Architecture + integrated AgentService). Follow service-specific patterns below.

**🔐 IMPORTANT: Before making ANY changes, read `docs/SECURITY.md` for security rules and best practices.**

### 📋 Table of Contents

1. [Angular Frontend](#angular-frontend-frontend) - Modern Angular 20 with signals & zoneless architecture
2. [.NET Backend Service](#net-backend-service-backend) - Clean Architecture with CQRS pattern
3. [.NET AgentService](#net-agentservice-backendsrccvanalyzeragentservice) - AI-powered resume analysis

---

## Angular Frontend (`frontend/`)

Modern Angular 20 application with zoneless architecture, standalone components, and signals.

### Architecture Overview

- **Framework**: Angular 20 with zoneless change detection (no zone.js)
- **Components**: Standalone components only (no NgModules)
- **State Management**: Signals with `signal()`, `computed()`, `effect()`
- **Routing**: Client-side routing with lazy loading
- **Styling**: SCSS with component-scoped styles
- **HTTP**: HttpClient with functional interceptors
- **Build**: Multi-stage Docker build (Node 20 → nginx 1.25-alpine)
- **Deployment**: Nginx serving static assets + reverse proxy to backend/AI service

### Folder Structure (Best Practices)

```
frontend/src/app/
├── core/                    # Singleton services, guards, interceptors (app-wide)
│   ├── guards/             # Route guards (auth, permissions)
│   ├── interceptors/       # HTTP interceptors (API, auth, error handling)
│   ├── models/             # Domain models and interfaces
│   └── services/           # Singleton services (API clients, state management)
├── features/               # Feature modules (lazy-loaded)
│   ├── resume-upload/      # Resume upload feature
│   └── resume-analysis/    # Analysis results feature
└── shared/                 # Reusable components, directives, pipes
    ├── components/         # Shared UI components (buttons, cards, etc.)
    ├── directives/         # Shared directives
    └── pipes/              # Shared pipes (date, currency, etc.)
```

### Creating New Features

**Step-by-step guide for adding new Angular features following best practices**

**1. Feature Component (lazy-loaded):**
```bash
ng generate component features/my-feature --standalone
```

**2. Component Pattern (modern Angular):**

**⚠️ CRITICAL: ALWAYS use separate template and style files:**
- **NEVER** use inline `template:` or `styles:` in @Component decorator
- **ALWAYS** use `templateUrl:` and `styleUrl:` with separate `.html` and `.scss` files
- Inline templates/styles are only acceptable for tiny (<5 lines) utility components
- This improves readability, syntax highlighting, and maintainability

```typescript
import { Component, signal, computed, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-my-feature',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-feature.component.html',  // ✅ Separate HTML file
  styleUrl: './my-feature.component.scss'      // ✅ Separate SCSS file
})
export class MyFeatureComponent {
  // Use inject() instead of constructor injection
  private readonly myService = inject(MyService);
  
  // Use signals for reactive state
  data = signal<MyData[]>([]);
  filteredData = computed(() => this.data().filter(d => d.active));
  
  // Use effect for side effects
  constructor() {
    effect(() => {
      console.log('Data changed:', this.data());
    });
  }
}
```

**3. Service Pattern:**
```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class MyService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/my-resource`;
  
  getAll(): Observable<MyData[]> {
    return this.http.get<MyData[]>(this.apiUrl);
  }
}
```

**4. Route Configuration (`app.routes.ts`):**
```typescript
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'my-feature',
    loadComponent: () => import('./features/my-feature/my-feature').then(m => m.MyFeatureComponent)
  }
];
```

### HTTP Services & API Integration

- **Service location**: `core/services/*.service.ts`
- **Pattern**: Use `inject(HttpClient)` instead of constructor injection
- **Base URL**: Configured in `environments/environment.ts`
- **Interceptor**: Global API interceptor in `core/interceptors/api.interceptor.ts`
- **Error handling**: Centralized in interceptor, component-specific via RxJS `catchError`

**Example API Service:**
```typescript
@Injectable({ providedIn: 'root' })
export class ResumeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/resumes`;

  uploadResume(request: UploadResumeRequest): Observable<{ id: string }> {
    const formData = new FormData();
    formData.append('file', request.file);
    formData.append('userId', request.userId);
    return this.http.post<{ id: string }>(`${this.apiUrl}/upload`, formData);
  }

  getResume(id: string): Observable<Resume> {
    return this.http.get<Resume>(`${this.apiUrl}/${id}`);
  }
}
```

### Environment Configuration

**Development** (`src/environments/environment.ts`):
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  aiServiceUrl: 'http://localhost:8000'
};
```

**Production** (`src/environments/environment.prod.ts`):
```typescript
export const environment = {
  production: true,
  apiUrl: '/api',  // Nginx proxies to backend
  aiServiceUrl: '/ai'  // Nginx proxies to AI service
};
```

### Nginx Configuration

**Key features in `frontend/nginx.conf`:**
- **Reverse proxy** to backend services (`/api/` → `http://api:5000/api/`)
- **Note**: AgentService is integrated within backend, not a separate service
- **SPA routing**: Serves `index.html` for all routes (`try_files $uri $uri/ /index.html`)
- **Static asset caching**: 1 year for JS/CSS/images
- **Security headers**: X-Frame-Options, X-Content-Type-Options, X-XSS-Protection
- **Health check**: `/health` endpoint returns 200

### Docker Build & Deployment

**Dockerfile** (`frontend/Dockerfile`):
- **Stage 1 (Build)**: Node 20-alpine, `npm ci`, `npm run build --configuration=production`
- **Stage 2 (Serve)**: nginx:1.25-alpine, copy built app from stage 1
- **Output**: `dist/cv-analyzer-frontend/browser/` → `/usr/share/nginx/html`
- **Port**: Exposes 80, mapped to 4200 in docker-compose
- **Health check**: `wget --spider http://localhost/health`

**Build commands:**
```bash
# Development
cd frontend
npm install
npm start  # http://localhost:4200 with proxy to backend

# Docker build
docker build -t cv-analyzer-frontend ./frontend

# Production build (local)
npm run build -- --configuration=production
```

### App Configuration

**Key file**: `src/app/app.config.ts`

```typescript
import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { apiInterceptor } from './core/interceptors/api.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),  // Modern zoneless architecture
    provideRouter(routes),
    provideHttpClient(withInterceptors([apiInterceptor]))
  ]
};
```

### Common Patterns & Pitfalls

**Quick snippets:**
```typescript
// Signal state with computed and effect
const count = signal(0);
const doubled = computed(() => count() * 2);
effect(() => console.log('Count:', count()));

// Standalone component template
@Component({
  selector: 'app-my-component',
  standalone: true,
  imports: [CommonModule, RouterLink, MyOtherComponent],
  template: `
    <div>{{ data() }}</div>
    <button (click)="increment()">+</button>
  `
})

// HTTP request with error handling
this.http.get<Data>(url).pipe(
  catchError(error => {
    console.error('Request failed:', error);
    return of(null);
  })
).subscribe(data => this.data.set(data));

// FormData for file uploads
const formData = new FormData();
formData.append('file', fileInput.files[0]);
this.http.post('/api/upload', formData).subscribe();
```

**Common pitfalls:**
- **Zone.js removed**: Don't use `NgZone` or zone-dependent libraries (use signals instead)
- **Imports required**: Must explicitly import all dependencies in `imports: []` array
- **Signals over RxJS**: Prefer signals for component state, RxJS for async operations/HTTP
- **Environment files**: Must configure in `angular.json` under `fileReplacements` for build
- **Proxy config**: `proxy.conf.json` only works in dev mode (`ng serve`), not in Docker build

### Development Workflow

**Local development (with backend):**
```bash
cd frontend
npm start
# Proxy configured in proxy.conf.json:
# /api/* -> http://localhost:5000
# /ai/* -> http://localhost:8000
```

**Docker Compose (full stack):**
```bash
# From repository root
docker-compose up -d
# Frontend: http://localhost:4200
# Backend + AgentService: http://localhost:5000
# SQL Server: localhost:1433
# All services use Azure Storage (connection string in appsettings)
```

### Key Files for Reference

| Purpose | File Path |
|---------|-----------|
| App configuration | `frontend/src/app/app.config.ts` |
| Routes | `frontend/src/app/app.routes.ts` |
| Main component | `frontend/src/app/app.ts` |
| Domain models | `frontend/src/app/core/models/resume.model.ts` |
| Resume service | `frontend/src/app/core/services/resume.service.ts` |
| API interceptor | `frontend/src/app/core/interceptors/api.interceptor.ts` |
| Dev environment | `frontend/src/environments/environment.ts` |
| Prod environment | `frontend/src/environments/environment.prod.ts` |
| Nginx config | `frontend/nginx.conf` |
| Dockerfile | `frontend/Dockerfile` |
| Proxy config (dev) | `frontend/proxy.conf.json` |
| Package.json | `frontend/package.json` |
| Angular config | `frontend/angular.json` |

---

## .NET Backend Service (`backend/`)

This service follows Clean Architecture (Domain / Application / Infrastructure / API) using .NET 10, MediatR (CQRS), FluentValidation and EF Core.

### Architecture Overview

- **Framework Version**: .NET 10 (all projects)
- **Layer dependencies**: API → Infrastructure → Application → Domain (strict one-way). Domain has zero dependencies.
- **Entry point**: `backend/src/CVAnalyzer.API/Program.cs` — registers `AddApplication()` and `AddInfrastructure(configuration)`, configures Serilog (rolling file + console), Swagger, CORS "AllowAll", and global `ExceptionHandlingMiddleware`.
- **Core entities**: `Resume` (blob URL, content, score, status), `CandidateInfo` (extracted resume details), and `Suggestion` (category, priority) in `backend/src/CVAnalyzer.Domain/Entities`.
- **Exception handling**: All unhandled exceptions are caught by `ExceptionHandlingMiddleware`, which transforms `ValidationException` to 400 BadRequest with structured error details.
- **AI Integration**: Integrated `CVAnalyzer.AgentService` project within backend uses Azure.AI.OpenAI SDK (v1.0.0-beta.13) with Azure OpenAI GPT-4o deployment
- **Background Processing**: Queue-based async resume analysis via `ResumeAnalysisWorker` (BackgroundService) + Azure Storage Queues + Document Intelligence

### Implementing New Features (CQRS Pattern)

- **Request location**: `backend/src/CVAnalyzer.Application/Features/<Feature>/Commands` (writes) or `.../Queries` (reads).
  - Example: `UploadResumeCommand.cs` is a `record` implementing `IRequest<Guid>`.
  - Pattern: `public record MyCommand(...) : IRequest<TResult>;`
- **Handler**: Same folder, named `MyCommandHandler.cs` implementing `IRequestHandler<MyCommand, TResult>`.
  - Inject `IApplicationDbContext` for DB access, `IBlobStorageService` for blob ops, `IResumeQueueService` for queuing analysis, `IAIResumeAnalyzerService` for direct AI calls (orchestrator).
  - Example: `UploadResumeCommandHandler` creates Resume entity, calls blob service, enqueues analysis request via `IResumeQueueService`, saves to DB.
- **Validator**: Same folder, named `MyCommandValidator.cs` extending `AbstractValidator<MyCommand>`.
  - Example: `UploadResumeCommandValidator` checks UserId not empty, FileName ≤255 chars, FileStream not null.
  - Auto-discovered by `AddValidatorsFromAssembly` in `DependencyInjection.cs`.
  - Validation executes automatically via `ValidationBehavior<,>` pipeline before handler runs; throws `ValidationException` on failure.

### Dependency Injection Patterns

- **Application layer** (`backend/src/CVAnalyzer.Application/DependencyInjection.cs`):
  - `AddMediatR` with `RegisterServicesFromAssembly` — auto-discovers all handlers.
  - `AddOpenBehavior(typeof(ValidationBehavior<,>))` — injects validation pipeline.
  - `AddValidatorsFromAssembly` — auto-discovers all FluentValidation validators.
  - **No manual handler registration needed** — just add files in correct namespace.
- **Infrastructure layer** (`backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs`):
  - Registers `ApplicationDbContext` with SQL Server connection string (from config or Key Vault).
  - Scopes: `IApplicationDbContext`, `IBlobStorageService`, `IAIResumeAnalyzerService`, `IResumeQueueService`, `IDocumentIntelligenceService`.
  - Singletons: `OpenAIClient`, `ResumeAnalysisAgent` (from AgentService project).
  - Background services: `ResumeAnalysisWorker` (hosted service processing queue).
  - **Key Vault**: When `UseKeyVault=true`, fetches `DatabaseConnectionString` secret via `DefaultAzureCredential`. Fallback to config on error (logs warning).
  - **Local dev**: Set `UseKeyVault=false` and use `ConnectionStrings:DefaultConnection` from appsettings.

### Data Access and EF Core

- **DbContext**: `CVAnalyzer.Infrastructure.Persistence.ApplicationDbContext` implements `IApplicationDbContext`.
  - Entities: `Resume`, `Suggestion` (1:N relationship).
  - Always use `IApplicationDbContext` interface in handlers, not concrete type.
- **Querying**: Use `.Include(r => r.Suggestions)` for navigation properties (see `GetResumeByIdQueryHandler.cs`).
- **Migrations**: Run from backend directory: `dotnet ef migrations add <Name> --project src/CVAnalyzer.Infrastructure --startup-project src/CVAnalyzer.API`.
- **Connection strings**:
  - Dev fallback: `Server=(localdb)\\mssqllocaldb;Database=CVAnalyzerDb;Trusted_Connection=True;MultipleActiveResultSets=true`
  - Docker: `Server=sqlserver;Database=CVAnalyzerDb;User Id=sa;Password=<PASSWORD_PLACEHOLDER>;TrustServerCertificate=True`

### Controllers & API Endpoints

- **Controller pattern**: Thin controllers delegate to MediatR (`backend/src/CVAnalyzer.API/Controllers/ResumesController.cs`).
  - Validate inputs, create request object, call `_mediator.Send(...)`, return ActionResult.
  - Example: `Upload` method accepts `IFormFile`, creates `UploadResumeCommand` with `file.OpenReadStream()`, returns `CreatedAtAction`.
- **Health endpoint**: `GET /api/health` via `HealthController` (also mapped in Program.cs with `MapHealthChecks("/health")`).
- **Error responses**: `ExceptionHandlingMiddleware` transforms exceptions:
  - `ValidationException` → 400 with `{ message, statusCode, errors: [{PropertyName, ErrorMessage}] }`
  - Domain exceptions (if added) → handle similarly in middleware switch statement.

### Logging & Configuration

- **Serilog**: Configured in `Program.cs` with `ReadFrom.Configuration`.
  - Console + rolling file: `logs/cvanalyzer-.log` (daily).
  - Use `ILogger<T>` injection in handlers/controllers; avoid static `Log.Logger` except startup/shutdown.
- **Config keys**:
  - `ConnectionStrings:DefaultConnection` — SQL connection string.
  - `UseKeyVault` (true/false) — enable Azure Key Vault secret retrieval.
  - `KeyVault:Uri` — Key Vault endpoint (e.g., `https://kv-cvanalyzer-dev.vault.azure.net/`).
  - `Agent:Endpoint` — Azure OpenAI endpoint (e.g., `https://<resource>.openai.azure.com/`)
  - `Agent:Deployment` — Azure OpenAI deployment name (e.g., `gpt-4o`)
  - `Agent:Temperature` — Temperature setting (default: 0.7)
  - `Agent:TopP` — TopP/NucleusSampling (default: 0.95)
  - `AzureStorage:ConnectionString` — Azure Storage connection string for blobs and queues
  - `Queue:ResumeAnalysisQueueName` — Queue name for resume analysis (default: `resume-analysis`)
  - Serilog settings in `appsettings.json` / `appsettings.Development.json`.


### Build, Run & Test Workflows

**Local development:**
1. Restore dependencies: `dotnet restore` (from backend/ directory)
2. Build solution: `dotnet build`
3. Run API: `cd src/CVAnalyzer.API` → `dotnet run`
   - API hosts on HTTPS (port varies, check console output)
   - Swagger UI: `https://localhost:<port>/swagger` (Development only)
   - Uses LocalDB by default: `(localdb)\\mssqllocaldb`

**Docker deployment:**
- Local stack: `docker-compose up -d` (from repository root - runs Angular Frontend + .NET API + AgentService + SQL Server)
  - Frontend: `http://localhost:4200`
  - .NET API + AgentService: `http://localhost:5000`
  - SQL: `localhost:1433` (sa/<PASSWORD_PLACEHOLDER>)
  - Volume: `sqlserver-data` for persistence
- Production: `docker build -f Dockerfile -t cvanalyzer-api .` (from backend/ directory)

**Testing:**
- Run all tests: `dotnet test` (from backend/ directory)
- Unit tests: `backend/tests/CVAnalyzer.UnitTests`
- Integration tests: `backend/tests/CVAnalyzer.IntegrationTests`
- Test frameworks: xUnit, FluentAssertions, NSubstitute

**Database migrations:**
- Add migration: `dotnet ef migrations add <MigrationName> --project src/CVAnalyzer.Infrastructure --startup-project src/CVAnalyzer.API`
- Update database: `dotnet ef database update --project src/CVAnalyzer.Infrastructure --startup-project src/CVAnalyzer.API`

### Infrastructure as Code (Terraform)

- **Location**: `terraform/` directory with modular structure.
- **Environments**: dev, test, prod — use `terraform apply -var-file="environments/<env>.tfvars"`.
- **Naming convention**: `{resource-type}-cvanalyzer-{environment}` (e.g., `app-cvanalyzer-dev`, `sql-cvanalyzer-dev`, `kv-cvanalyzer-dev`).
- **Resources per environment**:
  - Resource Group
  - App Service Plan + Linux Web App (Docker container)
  - SQL Server + Database
  - Key Vault (stores DB connection string)
  - Managed Identity for Copilot (`mi-copilot-coding-agent` with Reader permissions)
- **SQL password**: Must set `TF_VAR_sql_admin_password` env var before apply (never commit!).
  - PowerShell: `$env:TF_VAR_sql_admin_password = "YourPassword"`
  - Bash: `export TF_VAR_sql_admin_password="YourPassword"`
- **Modules**: `app-service/`, `key-vault/`, `sql-database/` — simple, focused, no circular dependencies (KISS principle).
- **State management**: Currently local; for teams use Azure Storage backend (see `terraform-instructions.md`).

### Azure MCP Integration (GitHub Copilot)

- **Purpose**: Copilot coding agent can query Azure resources, check deployment status, suggest infrastructure improvements.
- **Managed Identity**: `mi-copilot-coding-agent` provides passwordless access with Reader permissions.
- **Usage**: Natural language queries like "Show me my App Service configuration" or "Check SQL database status".


### Common Patterns & Pitfalls

**Quick snippets:**
```csharp
// New command request (record preferred)
public record MyCommand(string Param1, int Param2) : IRequest<Guid>;

// Handler skeleton
public class MyCommandHandler : IRequestHandler<MyCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public MyCommandHandler(IApplicationDbContext context) => _context = context;
    
    public async Task<Guid> Handle(MyCommand request, CancellationToken ct)
    {
        // Implementation
        await _context.SaveChangesAsync(ct);
        return entityId;
    }
}

// Validator
public class MyCommandValidator : AbstractValidator<MyCommand>
{
    public MyCommandValidator()
    {
        RuleFor(x => x.Param1).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Param2).GreaterThan(0);
    }
}
```

**Common pitfalls:**
- **Key Vault credentials**: When `UseKeyVault=true`, ensure Azure credentials available (Azure CLI login, managed identity, or environment vars). For local dev, set `UseKeyVault=false`.
- **MediatR discovery**: Moving handler files is safe as long as they stay in `CVAnalyzer.Application` assembly. Namespace changes don't break auto-registration.
- **EF includes**: Missing `.Include()` causes null navigation properties. Check `GetResumeByIdQueryHandler` for pattern.
- **Stream disposal**: When passing `Stream` to commands (like `UploadResumeCommand`), handler is responsible for disposal or must consume before disposal.
- **Validation timing**: FluentValidation runs BEFORE handler via pipeline behavior; no need to manually validate in handlers.

### Key Files for Reference

| Purpose | File Path |
|---------|-----------|
| MediatR + validation setup | `backend/src/CVAnalyzer.Application/DependencyInjection.cs` |
| DbContext + Key Vault | `backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs` |
| Command example | `backend/src/CVAnalyzer.Application/Features/Resumes/Commands/UploadResumeCommand.cs` |
| Query with includes | `backend/src/CVAnalyzer.Application/Features/Resumes/Queries/GetResumeByIdQueryHandler.cs` |
| Validator example | `backend/src/CVAnalyzer.Application/Features/Resumes/Commands/UploadResumeCommandValidator.cs` |
| Controller pattern | `backend/src/CVAnalyzer.API/Controllers/ResumesController.cs` |
| Global exception handling | `backend/src/CVAnalyzer.API/Middleware/ExceptionHandlingMiddleware.cs` |
| Validation pipeline | `backend/src/CVAnalyzer.Application/Behaviors/ValidationBehavior.cs` |
| Startup configuration | `backend/src/CVAnalyzer.API/Program.cs` |
| Terraform module example | `terraform/modules/app-service/main.tf` |

### Security & Compliance

- **Primary resource**: `docs/SECURITY.md` — **READ BEFORE ANY CODE CHANGES**.
- **Key principles**:
  - Never commit secrets (use Key Vault or env vars)
  - Validate all inputs (FluentValidation)
  - Use parameterized queries (EF Core handles this)
  - Log errors but sanitize sensitive data

---

## .NET AgentService (`backend/src/CVAnalyzer.AgentService/`)

Integrated C# project using Azure.AI.OpenAI SDK for AI-powered resume analysis with GPT-4o.

### Architecture Overview

- **Framework**: Minimal ASP.NET Core API (Kestrel)
- **AI Integration**: Azure.AI.OpenAI SDK with Azure OpenAI (GPT-4o deployment)
- **Authentication**: DefaultAzureCredential (managed identity or Azure CLI)
- **Configuration**: IOptions pattern with `AgentServiceOptions`
- **Deployment**: Runs as integrated service within backend container

### Key Components

**Configuration** (`AgentServiceOptions.cs`):
- Bind from `appsettings.json` section `Agent`
- Properties: `Endpoint`, `Deployment`, `Temperature`, `TopP`
- Validates presence of Endpoint and Deployment at runtime

**Models** (`Models/`):
- `ResumeAnalysisRequest`: Validates content (10-10000 chars), user_id
- `ResumeAnalysisResponse`: Score (0-100), optimized_content, candidate_info, suggestions[], metadata
- `CandidateInfoDto`: Extracted resume details (name, email, phone, skills, experience)
- `ResumeSuggestion`: Category, description, priority (1-5)

**Agent** (`ResumeAnalysisAgent.cs`):
- Singleton service registered in DI container
- Uses `OpenAIClient` with `DefaultAzureCredential`
- `AnalyzeAsync()`: Main analysis method with structured JSON output
- System instructions: Expert resume analyzer with ATS optimization, scoring criteria
- Response parsing: JSON extraction with fallback error handling via `System.Text.Json`

**API** (`Program.cs`):
- Minimal API endpoints (no controllers)
- `GET /`: Service info endpoint
- `GET /health`: Health check with AI connectivity status
- `POST /analyze`: Resume analysis endpoint with validation
- Startup registration via `AgentStartup.ConfigureServices()`

### API Endpoints

- `POST /analyze`: Analyze resume content
  - Request: `{"content": "...", "userId": "..."}`
  - Response: Score, optimized content, candidate info, suggestions, metadata
- `GET /health`: Health check
  - Response: `{"status": "healthy", "aiConnected": true/false}`
- `GET /`: Root endpoint with service info

### Development Workflow

**Local development:**
```bash
cd backend/src/CVAnalyzer.AgentService
dotnet run
# Service runs on http://localhost:5001 (or configured port)
```

**Docker (integrated with backend):**
```bash
# From repository root
docker-compose up -d
# AgentService runs on same port as API (5000) but different path
```

**Testing:**
```bash
cd backend
dotnet test --filter "FullyQualifiedName~AgentService"
```

### Dependencies

**Core:**
- `Azure.AI.OpenAI` — Azure OpenAI SDK
- `Azure.Identity` — DefaultAzureCredential
- `Microsoft.Extensions.Options` — Configuration binding

### Configuration

Required appsettings.json section:
```json
{
  "Agent": {
    "Endpoint": "https://<resource>.openai.azure.com/",
    "Deployment": "gpt-4o",
    "Temperature": 0.7,
    "TopP": 0.95
  }
}
```

**Authentication** (DefaultAzureCredential auto-resolves in this order):
1. **Environment Variables**: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET` (service principal)
2. **Managed Identity**: Automatic in Azure Container Apps/App Service (production)
3. **Azure CLI**: `az login` credentials (local development)
4. **Visual Studio/VS Code**: Cached credentials from IDE

**Note**: API keys are NOT supported for security reasons. All authentication uses DefaultAzureCredential only.

### Integration with Main Backend

The backend infrastructure calls AgentService internally:

```csharp
// CVAnalyzer.Infrastructure/DependencyInjection.cs
services.AddSingleton<OpenAIClient>(sp => {
    var options = sp.GetRequiredService<IOptions<AgentServiceOptions>>().Value;
    return new OpenAIClient(new Uri(options.Endpoint), new DefaultAzureCredential());
});
services.AddSingleton<ResumeAnalysisAgent>();

// CVAnalyzer.Infrastructure/Services/AIResumeAnalyzerService.cs
// Orchestrator calls ResumeAnalysisAgent.AnalyzeAsync() directly
```

### Common Patterns

**Agent usage:**
```csharp
// In handler or orchestrator
var request = new ResumeAnalysisRequest 
{ 
    Content = resumeContent, 
    UserId = userId 
};

var result = await _agent.AnalyzeAsync(request, cancellationToken);
// Returns: ResumeAnalysisResponse with score, suggestions, candidate info
```

**Error handling:**
```csharp
try
{
    var result = await _agent.AnalyzeAsync(request, ct);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("not configured"))
{
    _logger.LogError("Agent not configured: {Message}", ex.Message);
    throw;
}
catch (RequestFailedException ex)
{
    _logger.LogError(ex, "Azure OpenAI request failed");
    throw new InvalidOperationException("AI analysis failed", ex);
}
```

### Security

- DefaultAzureCredential for passwordless auth
- Input validation via DataAnnotations (10-10000 chars)
- Structured logging (no sensitive data in logs)
- Singleton OpenAIClient for connection pooling
- Health check endpoint for monitoring

### Background Processing

Resume analysis is processed asynchronously:

1. **Upload**: Handler creates Resume entity, uploads blob, enqueues analysis request
2. **Queue**: `IResumeQueueService` adds message to Azure Storage Queue
3. **Worker**: `ResumeAnalysisWorker` (BackgroundService) polls queue, calls orchestrator
4. **Orchestrator**: `ResumeAnalysisOrchestrator` extracts text (Document Intelligence), analyzes (AgentService), saves results
5. **Result**: Updates Resume entity with score, suggestions, optimized content

See `backend/src/CVAnalyzer.Infrastructure/BackgroundServices/ResumeAnalysisWorker.cs` for implementation.

### Key Files for Reference

| Purpose | File Path |
|---------|-----------|
| AgentService API | `backend/src/CVAnalyzer.AgentService/Program.cs` |
| Agent logic | `backend/src/CVAnalyzer.AgentService/ResumeAnalysisAgent.cs` |
| Models | `backend/src/CVAnalyzer.AgentService/Models/` |
| Configuration | `backend/src/CVAnalyzer.AgentService/AgentServiceOptions.cs` |
| Startup | `backend/src/CVAnalyzer.AgentService/AgentStartup.cs` |
| Background worker | `backend/src/CVAnalyzer.Infrastructure/BackgroundServices/ResumeAnalysisWorker.cs` |
| Orchestrator | `backend/src/CVAnalyzer.Infrastructure/Services/ResumeAnalysisOrchestrator.cs` |

---

## Summary

This monorepo contains two tightly integrated services:

1. **Angular Frontend** (`frontend/`) - User interface with Angular 20, zoneless architecture
2. **.NET Backend + AgentService** (`backend/`) - Business logic with Clean Architecture + CQRS + integrated AI analysis

**Development workflow:**
- Local: Run each service independently or use docker-compose
- Production: Multi-container deployment with nginx reverse proxy (Azure Container Apps)

**Key principles:**
- Frontend: Signals for state, standalone components, lazy loading
- Backend: CQRS with MediatR, automatic validation, strict architecture layers
- AgentService: Azure OpenAI SDK, DefaultAzureCredential, structured JSON output
- Background processing: Queue-based async analysis with Document Intelligence + AI
- All: Security-first (no secrets in code, input validation, error handling)

**Need more detail?** Tell me which area to expand (e.g., testing patterns, EF migrations, Angular components, AgentService integration, queue processing, Terraform modules, CI/CD setup) and I'll provide concrete examples from the codebase.
