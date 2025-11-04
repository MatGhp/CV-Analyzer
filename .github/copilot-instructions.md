## CV Analyzer — Copilot instructions for code changes

**Monorepo Structure**: This repository contains two microservices - .NET Backend (Clean Architecture) and Python AI Service (FastAPI + Agent Framework). Follow service-specific patterns below.

**🔐 IMPORTANT: Before making ANY changes, read `.github/security-guardrails.md` for security rules and best practices.**

---

## .NET Backend Service (`backend/`)

This service follows Clean Architecture (Domain / Application / Infrastructure / API) using .NET 9, MediatR (CQRS), FluentValidation and EF Core.

### Architecture Overview

- **Layer dependencies**: API → Infrastructure → Application → Domain (strict one-way). Domain has zero dependencies.
- **Entry point**: `backend/src/CVAnalyzer.API/Program.cs` — registers `AddApplication()` and `AddInfrastructure(configuration)`, configures Serilog (rolling file + console), Swagger, CORS "AllowAll", and global `ExceptionHandlingMiddleware`.
- **Core entities**: `Resume` (blob URL, content, score, status) and `Suggestion` (category, priority) in `backend/src/CVAnalyzer.Domain/Entities`.
- **Exception handling**: All unhandled exceptions are caught by `ExceptionHandlingMiddleware`, which transforms `ValidationException` to 400 BadRequest with structured error details.
- **AI Integration**: Calls Python AI Service via HTTP (`IAIResumeAnalyzerService` → `http://ai-service:8000/analyze`)

### Implementing New Features (CQRS Pattern)

- **Request location**: `backend/src/CVAnalyzer.Application/Features/<Feature>/Commands` (writes) or `.../Queries` (reads).
  - Example: `UploadResumeCommand.cs` is a `record` implementing `IRequest<Guid>`.
  - Pattern: `public record MyCommand(...) : IRequest<TResult>;`
- **Handler**: Same folder, named `MyCommandHandler.cs` implementing `IRequestHandler<MyCommand, TResult>`.
  - Inject `IApplicationDbContext` for DB access, `IBlobStorageService` for blob ops, `IAIResumeAnalyzerService` for AI analysis.
  - Example: `UploadResumeCommandHandler` creates Resume entity, calls blob service, saves to DB.
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
  - Scopes: `IApplicationDbContext`, `IBlobStorageService`, `IAIResumeAnalyzerService`.
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
  - Docker: `Server=sqlserver;Database=CVAnalyzerDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True`

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
  - `AIService:BaseUrl` — Python AI service endpoint (e.g., `http://ai-service:8000`)
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
- Local stack: `docker-compose up -d` (from repository root - runs .NET API + Python AI Service + SQL Server)
  - .NET API: `http://localhost:5000`
  - Python AI: `http://localhost:8000`
  - SQL: `localhost:1433` (sa/YourStrong@Passw0rd)
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

- **Primary resource**: `.github/security-guardrails.md` — **READ BEFORE ANY CODE CHANGES**.
- **Key principles**:
  - Never commit secrets (use Key Vault or env vars)
  - Validate all inputs (FluentValidation)
  - Use parameterized queries (EF Core handles this)
  - Log errors but sanitize sensitive data

---

## Python AI Service (`ai-service/`)

FastAPI microservice using Microsoft Agent Framework for AI-powered resume analysis with GPT-4o.

### Architecture Overview

- **Framework**: FastAPI + Uvicorn (async ASGI server)
- **AI Integration**: Microsoft Agent Framework (preview) with Azure AI Foundry
- **Model**: GPT-4o via Azure AI deployment
- **Authentication**: DefaultAzureCredential (managed identity or service principal)
- **Configuration**: Pydantic Settings with environment variables
- **Deployment**: Docker container with non-root user

### Key Components

**Configuration (`ai-service/app/config.py`)**:
- Singleton pattern with `@lru_cache`
- Environment variables: `AI_FOUNDRY_ENDPOINT`, `MODEL_DEPLOYMENT_NAME`, Azure credentials
- Validation via Pydantic Settings

**Models (`ai-service/app/models.py`)**:
- `ResumeAnalysisRequest`: Validates content (10-10000 chars), user_id
- `ResumeAnalysisResponse`: Score (0-100), optimized_content, suggestions[], metadata
- `Suggestion`: Category, description, priority (1-5)

**Agent (`ai-service/app/agent.py`)**:
- `ResumeAnalyzerAgent` class with Agent Framework integration
- `initialize()`: Creates AzureAIAgentClient and ChatAgent
- `analyze_resume()`: Main analysis method with structured output
- System instructions: Expert resume analyzer with ATS optimization, scoring criteria
- Response parsing: JSON extraction with fallback error handling
- Singleton via `get_agent()` function

**FastAPI App (`ai-service/app/main.py`)**:
- Lifespan context manager for agent init/cleanup
- `POST /analyze`: Resume analysis endpoint
- `GET /health`: Health check with AI connectivity status
- CORS middleware, global exception handler
- Structured logging

### API Endpoints

- `POST /analyze`: Analyze resume content
  - Request: `{"content": "...", "user_id": "..."}`
  - Response: Score, optimized content, suggestions, metadata
- `GET /health`: Health check
  - Response: `{"status": "healthy", "ai_connected": true/false}`
- `GET /`: Root endpoint with service info

### Development Workflow

**Local development:**
```bash
cd ai-service
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt --pre  # --pre for Agent Framework
cp .env.example .env  # Configure Azure credentials
python -m app.main
```

**Docker:**
```bash
# From repository root
docker-compose up ai-service
```

**Testing:**
```bash
pytest
```

### Dependencies

**Core:**
- `fastapi==0.115.5` - Web framework
- `uvicorn[standard]==0.32.1` - ASGI server
- `agent-framework-azure-ai>=0.1.0` - Microsoft Agent Framework (preview - requires `--pre`)
- `azure-identity==1.19.0` - Azure authentication
- `pydantic==2.10.3`, `pydantic-settings==2.6.1` - Data validation and settings

**Development:**
- `pytest`, `black`, `ruff`, `mypy`

### Environment Variables

Required:
- `AI_FOUNDRY_ENDPOINT`: Azure AI Foundry endpoint URL
- `MODEL_DEPLOYMENT_NAME`: GPT-4o deployment name (default: gpt-4o)

Authentication (choose one):
- **Service Principal**: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`
- **Managed Identity**: Automatic in Azure (no credentials needed)

Optional:
- `LOG_LEVEL`: INFO (default), DEBUG, WARNING, ERROR

### Integration with .NET Backend

The .NET backend calls this service via HTTP:

```csharp
// CVAnalyzer.Infrastructure/Services/AIResumeAnalyzerService.cs
var response = await _httpClient.PostAsJsonAsync("/analyze", request);
```

Configuration in appsettings.json:
```json
{
  "AIService": {
    "BaseUrl": "http://ai-service:8000"  // Docker
    // or "http://localhost:8000" for local dev
  }
}
```

### Common Patterns

**Agent Framework usage:**
```python
# Initialize agent
client = AzureAIAgentClient(endpoint, credential)
agent = client.agents.create_agent(
    model="gpt-4o",
    instructions="System prompt...",
    temperature=0.7
)

# Run analysis
thread = client.agents.create_thread()
message = client.agents.create_message(thread.id, content)
run = client.agents.create_and_process_run(thread.id, agent.id)
response = client.agents.list_messages(thread.id).data[0].content[0].text.value
```

**Error handling:**
```python
try:
    result = await agent.analyze_resume(content, user_id)
except Exception as e:
    logger.error(f"Analysis failed: {e}")
    raise HTTPException(status_code=500, detail="AI analysis failed")
```

### Security

- Non-root Docker user (UID 1000)
- DefaultAzureCredential for passwordless auth
- Input validation via Pydantic (10-10000 chars)
- Structured logging (no sensitive data)
- Health check endpoint for monitoring

### Key Files for Reference

| Purpose | File Path |
|---------|-----------|
| FastAPI application | `ai-service/app/main.py` |
| Agent Framework logic | `ai-service/app/agent.py` |
| Pydantic models | `ai-service/app/models.py` |
| Configuration | `ai-service/app/config.py` |
| Dependencies | `ai-service/requirements.txt` |
| Dockerfile | `ai-service/Dockerfile` |
| Documentation | `ai-service/README.md` |

---

**Need more detail?** Tell me which area to expand (e.g., testing patterns, EF migrations, AI analyzer service, specific Terraform modules, CI/CD setup) and I'll update this file with concrete examples from the codebase.

**Need more detail?** Tell me which area to expand (e.g., testing patterns, EF migrations, AI analyzer service, specific Terraform modules, CI/CD setup) and I'll update this file with concrete examples from the codebase.
