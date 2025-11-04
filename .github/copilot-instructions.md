## CV Analyzer — Copilot instructions for code changes

This project follows Clean Architecture (Domain / Application / Infrastructure / API) using .NET 9, MediatR (CQRS), FluentValidation and EF Core. The guidance below is focused, actionable and tied to concrete files in this repo so an AI coding agent can be productive immediately.

**🔐 IMPORTANT: Before making ANY changes, read `.github/security-guardrails.md` for security rules and best practices.**

### Architecture Overview

- **Layer dependencies**: API → Infrastructure → Application → Domain (strict one-way). Domain has zero dependencies.
- **Entry point**: `src/CVAnalyzer.API/Program.cs` — registers `AddApplication()` and `AddInfrastructure(configuration)`, configures Serilog (rolling file + console), Swagger, CORS "AllowAll", and global `ExceptionHandlingMiddleware`.
- **Core entities**: `Resume` (blob URL, content, score, status) and `Suggestion` (category, priority) in `Domain/Entities`.
- **Exception handling**: All unhandled exceptions are caught by `ExceptionHandlingMiddleware`, which transforms `ValidationException` to 400 BadRequest with structured error details.

### Implementing New Features (CQRS Pattern)

- **Request location**: `src/CVAnalyzer.Application/Features/<Feature>/Commands` (writes) or `.../Queries` (reads).
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

- **Application layer** (`src/CVAnalyzer.Application/DependencyInjection.cs`):
  - `AddMediatR` with `RegisterServicesFromAssembly` — auto-discovers all handlers.
  - `AddOpenBehavior(typeof(ValidationBehavior<,>))` — injects validation pipeline.
  - `AddValidatorsFromAssembly` — auto-discovers all FluentValidation validators.
  - **No manual handler registration needed** — just add files in correct namespace.
- **Infrastructure layer** (`src/CVAnalyzer.Infrastructure/DependencyInjection.cs`):
  - Registers `ApplicationDbContext` with SQL Server connection string (from config or Key Vault).
  - Scopes: `IApplicationDbContext`, `IBlobStorageService`, `IAIResumeAnalyzerService`.
  - **Key Vault**: When `UseKeyVault=true`, fetches `DatabaseConnectionString` secret via `DefaultAzureCredential`. Fallback to config on error (logs warning).
  - **Local dev**: Set `UseKeyVault=false` and use `ConnectionStrings:DefaultConnection` from appsettings.

### Data Access and EF Core

- **DbContext**: `CVAnalyzer.Infrastructure.Persistence.ApplicationDbContext` implements `IApplicationDbContext`.
  - Entities: `Resume`, `Suggestion` (1:N relationship).
  - Always use `IApplicationDbContext` interface in handlers, not concrete type.
- **Querying**: Use `.Include(r => r.Suggestions)` for navigation properties (see `GetResumeByIdQueryHandler.cs`).
- **Migrations**: Run from solution root: `dotnet ef migrations add <Name> --project src/CVAnalyzer.Infrastructure --startup-project src/CVAnalyzer.API`.
- **Connection strings**:
  - Dev fallback: `Server=(localdb)\\mssqllocaldb;Database=CVAnalyzerDb;Trusted_Connection=True;MultipleActiveResultSets=true`
  - Docker: `Server=sqlserver;Database=CVAnalyzerDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True`

### Controllers & API Endpoints

- **Controller pattern**: Thin controllers delegate to MediatR (`src/CVAnalyzer.API/Controllers/ResumesController.cs`).
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
  - Serilog settings in `appsettings.json` / `appsettings.Development.json`.


### Build, Run & Test Workflows

**Local development:**
1. Restore dependencies: `dotnet restore` (solution root)
2. Build solution: `dotnet build`
3. Run API: `cd src/CVAnalyzer.API` → `dotnet run`
   - API hosts on HTTPS (port varies, check console output)
   - Swagger UI: `https://localhost:<port>/swagger` (Development only)
   - Uses LocalDB by default: `(localdb)\\mssqllocaldb`

**Docker deployment:**
- Local stack: `docker-compose up -d` (runs API + SQL Server 2022)
  - API: `http://localhost:5000`
  - SQL: `localhost:1433` (sa/YourStrong@Passw0rd)
  - Volume: `sqlserver-data` for persistence
- Production: `docker build -f Dockerfile -t cvanalyzer-api .`

**Testing:**
- Run all tests: `dotnet test` (solution root)
- Unit tests: `tests/CVAnalyzer.UnitTests` (5 tests currently)
- Integration tests: `tests/CVAnalyzer.IntegrationTests` (1 test currently)
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
| MediatR + validation setup | `src/CVAnalyzer.Application/DependencyInjection.cs` |
| DbContext + Key Vault | `src/CVAnalyzer.Infrastructure/DependencyInjection.cs` |
| Command example | `src/CVAnalyzer.Application/Features/Resumes/Commands/UploadResumeCommand.cs` |
| Query with includes | `src/CVAnalyzer.Application/Features/Resumes/Queries/GetResumeByIdQueryHandler.cs` |
| Validator example | `src/CVAnalyzer.Application/Features/Resumes/Commands/UploadResumeCommandValidator.cs` |
| Controller pattern | `src/CVAnalyzer.API/Controllers/ResumesController.cs` |
| Global exception handling | `src/CVAnalyzer.API/Middleware/ExceptionHandlingMiddleware.cs` |
| Validation pipeline | `src/CVAnalyzer.Application/Behaviors/ValidationBehavior.cs` |
| Startup configuration | `src/CVAnalyzer.API/Program.cs` |
| Terraform module example | `terraform/modules/app-service/main.tf` |

### Security & Compliance

- **Primary resource**: `.github/security-guardrails.md` — **READ BEFORE ANY CODE CHANGES**.
- **Review summary**: `SECURITY_REVIEW.md` — comprehensive security audit results.
- **Code review**: `CODE_REVIEW_SUMMARY.md` — security fixes applied.
- **Key principles**:
  - Never commit secrets (use Key Vault or env vars)
  - Validate all inputs (FluentValidation)
  - Use parameterized queries (EF Core handles this)
  - Log errors but sanitize sensitive data

---

**Need more detail?** Tell me which area to expand (e.g., testing patterns, EF migrations, AI analyzer service, specific Terraform modules, CI/CD setup) and I'll update this file with concrete examples from the codebase.
