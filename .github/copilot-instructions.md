## CV Analyzer — Copilot instructions for code changes

This project follows Clean Architecture (Domain / Application / Infrastructure / API) using .NET 9, MediatR (CQRS), FluentValidation and EF Core. The guidance below is focused, actionable and tied to concrete files in this repo so an AI coding agent can be productive immediately.

- Big picture
  - Layers: Domain (entities/exceptions), Application (CQRS features, validators, behaviors), Infrastructure (EF Core DbContext, blob and AI services, KeyVault), API (controllers, middleware, Serilog).
  - Entry point: `src/CVAnalyzer.API/Program.cs` — registers `AddApplication()` and `AddInfrastructure(configuration)` and configures Serilog, Swagger and CORS.

- Where to implement features
  - MediatR requests live under: `src/CVAnalyzer.Application/Features/*/Commands` and `.../Queries` (example: UploadResumeCommand & handler in `Features/Resumes/Commands`).
  - Validators use FluentValidation and filename convention `*Validator.cs` next to the request (example: `UploadResumeCommandValidator.cs`). Validation is wired by `AddValidatorsFromAssembly` and a pipeline behavior `ValidationBehavior<,>` registered via `AddApplication()`.
  - To add a new request/handler: create a record or class in `Application/Features/<Feature>/Commands` or `Queries`; name handler `XxxHandler` implementing `IRequestHandler<...>`; add validator if needed.

- Dependency injection patterns
  - Application DI: `src/CVAnalyzer.Application/DependencyInjection.cs` — MediatR registration is automatic (RegisterServicesFromAssembly + pipeline behavior). No manual handler registration needed.
  - Infrastructure DI: `src/CVAnalyzer.Infrastructure/DependencyInjection.cs` — typical place to register `IApplicationDbContext`, `IBlobStorageService`, `IAIResumeAnalyzerService`, DbContext, and Key Vault retrieval. If adding infrastructure services, extend this file.
  - Key Vault: enabled when `UseKeyVault` is `true` in configuration; secret lookup uses `DefaultAzureCredential()` and expects a secret named `DatabaseConnectionString` (see `AddInfrastructure`).

- Data access and patterns
  - EF DbContext: `CVAnalyzer.Infrastructure.Persistence.ApplicationDbContext` (registered in Infrastructure DI). Use `IApplicationDbContext` from Application.Common.Interfaces in handlers.
  - Use `.Include(...)` where needed (see `GetResumeByIdQueryHandler`). Follow existing patterns for entity navigation and suggestions.

- Controllers & endpoints
  - Controllers are thin: `src/CVAnalyzer.API/Controllers/ResumesController.cs` delegates to MediatR. Follow pattern: validate inputs, create request object, call `_mediator.Send(...)`, return appropriate ActionResult (CreatedAtAction, Ok, NotFound).
  - Health endpoint: `GET /api/health` implemented in `HealthController`.

- Logging & config
  - Serilog configured in `Program.cs` and uses `logs/cvanalyzer-.log` (rolling). Read logging settings from `appsettings.json`/`appsettings.Development.json`.
  - Common config keys: `ConnectionStrings:DefaultConnection`, `UseKeyVault`, `KeyVault:Uri`.

- Build, run, test (developer workflows)
  - Restore/build/run locally:
    - `dotnet restore` (repo root)
    - `dotnet build` (repo root)
    - `cd src/CVAnalyzer.API` then `dotnet run` (API hosts on HTTPS; Swagger available in Development at `/swagger`).
  - Tests: run `dotnet test` from solution root; unit tests under `tests/CVAnalyzer.UnitTests`, integration tests under `tests/CVAnalyzer.IntegrationTests`.
  - Docker: `docker-compose up -d` (local stack). Dockerfile exists for production container.

- Examples & quick copyable snippets
  - Create a command handler skeleton:
    - Request: `src/CVAnalyzer.Application/Features/<Feature>/Commands/MyCommand.cs` (record or class implementing `IRequest<T>`)
    - Handler: `MyCommandHandler : IRequestHandler<MyCommand, T>` in same folder
    - Validator: `MyCommandValidator : AbstractValidator<MyCommand>` in same folder

- Integration notes & common pitfalls
  - MediatR auto-registration means moving files is safe as long as they remain in the Application assembly.
  - When enabling Key Vault, app startup will attempt to fetch `DatabaseConnectionString` via `SecretClient` with `DefaultAzureCredential` — ensure environment provides credentials (or disable `UseKeyVault` during local dev).
  - Blob storage and AI analyzer interfaces are registered in Infrastructure DI — inspect `IBlobStorageService` and `IAIResumeAnalyzerService` implementations before changing behavior.

- Files to consult for examples
  - `src/CVAnalyzer.API/Program.cs` — host, Serilog, Swagger, CORS, health checks
  - `src/CVAnalyzer.Application/DependencyInjection.cs` — MediatR + validation behavior
  - `src/CVAnalyzer.Infrastructure/DependencyInjection.cs` — DbContext, KeyVault, service registrations
  - `src/CVAnalyzer.Application/Features/Resumes/*` — canonical example of command/query/validator/handler
  - `src/CVAnalyzer.API/Controllers/ResumesController.cs` — controller → MediatR usage

If any of these files or behaviors are out-of-date or you want additional examples (e.g., tests, EF migrations, or the AI analyzer implementation), tell me which area to expand and I will update the instructions.
