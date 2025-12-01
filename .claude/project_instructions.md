# CV-Analyzer - Claude Agent Project Instructions

## Project Overview

CV-Analyzer is an enterprise-grade resume optimization platform that combines AI-powered analysis with user account management. The application extracts candidate information from PDF resumes, provides ATS (Applicant Tracking System) scores, and delivers actionable improvement suggestions using Azure OpenAI GPT-4o.

**Key Capabilities**:
- Guest resume upload and analysis (24-hour retention)
- User authentication with JWT tokens
- Persistent resume storage for registered users
- AI-powered resume analysis with GPT-4o
- User dashboard with analytics

## Architecture

### Backend Architecture
- **Framework**: .NET 10 with C# 13
- **Pattern**: Clean Architecture + CQRS (Command Query Responsibility Segregation)
- **Mediator**: MediatR for command/query handling
- **Validation**: FluentValidation for request validation
- **ORM**: Entity Framework Core 9 with Code-First migrations

**Layer Structure**:
```
CVAnalyzer.Domain        → Core entities, interfaces (zero dependencies)
CVAnalyzer.Application   → Business logic, CQRS handlers, validators
CVAnalyzer.Infrastructure → Data access, Azure services, background workers
CVAnalyzer.API          → HTTP endpoints, middleware, DTOs
```

**Key Entities**:
- `User`: Authenticated users with email/password
- `Resume`: Upload records with AI analysis results
- `CandidateInfo`: Extracted candidate information
- `Suggestion`: Improvement recommendations
- `PromptTemplate`: AI system prompts

**Infrastructure Services**:
- `AIResumeAnalyzerService`: GPT-4o integration for analysis
- `DocumentIntelligenceService`: PDF text extraction
- `BlobStorageService`: File storage in Azure Blob
- `ResumeQueueService`: Async message queuing
- `ResumeAnalysisWorker`: Background processing worker
- `AnonymousDataCleanupService`: Guest data cleanup (24h)

### Frontend Architecture
- **Framework**: Angular 20 (standalone components)
- **Language**: TypeScript 5.9 (strict mode)
- **Styling**: TailwindCSS 3.4 + DaisyUI 4.12
- **State Management**: Angular Signals + RxJS 7.8
- **Data Fetching**: TanStack Angular Query
- **Icons**: Lucide Angular

**Feature Structure**:
```
core/          → Guards, interceptors, services, models
features/      → Feature modules (auth, dashboard, resume-upload, resume-analysis)
shared/        → Reusable components
environments/  → Configuration
```

**Key Services**:
- `AuthService`: Authentication, user management (signal-based state)
- `ResumeService`: Resume operations

**Route Protection**:
- `authGuard`: Requires JWT token (protects dashboard)
- `guestGuard`: Redirects authenticated users away from login/register

## Technology Stack

### Backend
| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET | 10 | Web API framework |
| Entity Framework Core | 9 | ORM |
| Azure SQL Server | 2022 | Database |
| Azure OpenAI | GPT-4o | AI analysis |
| Azure Document Intelligence | 4.1 | PDF processing |
| Azure Blob Storage | 12.19 | File storage |
| Azure Storage Queues | 12.17 | Async processing |
| MediatR | 13 | CQRS mediator |
| FluentValidation | 11.11 | Validation |
| BCrypt.Net | 4.0 | Password hashing |
| Serilog | 9.0 | Logging |
| Application Insights | 2.22 | Telemetry |

### Frontend
| Technology | Version | Purpose |
|-----------|---------|---------|
| Angular | 20.3 | SPA framework |
| TypeScript | 5.9 | Type safety |
| TailwindCSS | 3.4 | Styling |
| DaisyUI | 4.12 | Component library |
| RxJS | 7.8 | Reactive programming |
| Lucide Angular | 0.469 | Icons |
| TanStack Query | 5.62 | Data fetching |

## Development Guidelines

### Code Conventions

#### Backend (C#)
- **Naming**: PascalCase for public members, camelCase for private fields with underscore prefix
- **Async**: Always use `async/await` for I/O operations, suffix methods with `Async`
- **Nullability**: Enable nullable reference types, use `?` for nullable parameters
- **Validation**: Implement `FluentValidation` validators for all commands
- **Error Handling**: Use domain-specific exceptions, handle in global exception middleware
- **Dependency Injection**: Register services in `DependencyInjection.cs` extension methods

**Example CQRS Handler**:
```csharp
public class CreateResumeCommandHandler : IRequestHandler<CreateResumeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateResumeCommandHandler> _logger;

    public CreateResumeCommandHandler(
        IApplicationDbContext context,
        ILogger<CreateResumeCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateResumeCommand request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

#### Frontend (TypeScript/Angular)
- **Naming**: camelCase for variables/functions, PascalCase for classes/interfaces
- **Components**: Use standalone components, avoid NgModules
- **State**: Prefer signals over observables for synchronous state
- **Services**: Injectable with `providedIn: 'root'`
- **Types**: Always define interfaces for data models
- **Observables**: Use async pipe in templates, avoid manual subscriptions

**Example Signal-Based Service**:
```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private tokenSignal = signal<string | null>(null);
  readonly isAuthenticated = computed(() => !!this.tokenSignal());

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', credentials).pipe(
      tap(response => this.tokenSignal.set(response.token))
    );
  }
}
```

### File Organization

#### Backend
- **Commands**: `Features/{Feature}/Commands/{Action}Command.cs`
- **Queries**: `Features/{Feature}/Queries/{Action}Query.cs`
- **Handlers**: Co-locate with command/query (e.g., `CreateResumeCommandHandler.cs`)
- **Validators**: `{CommandName}Validator.cs` (same folder as command)
- **DTOs**: `API/Models/{Feature}/{Name}Dto.cs`
- **Controllers**: `API/Controllers/{Feature}Controller.cs`

#### Frontend
- **Components**: `features/{feature}/{component-name}/{component-name}.component.ts`
- **Services**: `core/services/{service-name}.service.ts`
- **Models**: `core/models/{domain}.model.ts`
- **Guards**: `core/guards/{guard-name}.guard.ts`

### Database Migrations

Always create migrations when modifying entities:
```bash
cd backend/src/CVAnalyzer.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../CVAnalyzer.API
dotnet ef database update --startup-project ../CVAnalyzer.API
```

### Testing Requirements

#### Backend Tests
- **Unit Tests**: Test handlers, services, validators in isolation
- **Integration Tests**: Test full request pipeline with in-memory database
- **Coverage Target**: 80%+ for business logic

**Test Structure**:
```csharp
public class CreateResumeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesResume()
    {
        // Arrange
        var handler = new CreateResumeCommandHandler(/*...*/);
        var command = new CreateResumeCommand { /* ... */ };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }
}
```

#### Frontend Tests
- **Unit Tests**: Test components, services, pipes
- **Test Framework**: Karma + Jasmine
- **Mocking**: Use Angular TestBed for DI

### Security Guidelines

#### Authentication & Authorization
- **JWT Tokens**: 60-minute expiration, stored in localStorage
- **Password Hashing**: BCrypt with cost factor 12
- **Password Requirements**: 8+ chars, uppercase, lowercase, digit, special character
- **API Protection**: Use `[Authorize]` attribute on protected endpoints

#### Secret Management
- **NEVER** commit secrets to version control
- Use Azure Key Vault for production secrets
- Use `appsettings.Development.json` (git-ignored) for local secrets
- Use environment variables in Docker/CI/CD

**Sensitive Values**:
- JWT secret keys
- Azure OpenAI API keys
- Azure Storage connection strings
- Database connection strings

#### Pre-Commit Hooks
- Gitleaks scanning enabled (`.gitleaks.toml`)
- Runs automatically before commits
- Prevents secret leakage

### API Design

#### RESTful Conventions
- **GET**: Retrieve resources (idempotent)
- **POST**: Create resources
- **PUT**: Update entire resource
- **PATCH**: Partial update
- **DELETE**: Remove resource

#### Response Format
```json
{
  "success": true,
  "data": { /* ... */ },
  "error": null
}
```

**Error Response**:
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": { /* ... */ }
  }
}
```

#### HTTP Status Codes
- **200 OK**: Successful GET/PUT/PATCH
- **201 Created**: Successful POST
- **204 No Content**: Successful DELETE
- **400 Bad Request**: Validation errors
- **401 Unauthorized**: Missing/invalid JWT
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Unhandled exceptions

### Background Processing

#### Queue-Based Architecture
1. Upload request → Synchronous file upload to Blob Storage
2. Queue message → Async analysis request
3. Background worker → Polls queue, processes analysis
4. Database update → Saves results

**Worker Pattern**:
```csharp
public class ResumeAnalysisWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await _queueService.DequeueMessageAsync(stoppingToken);
            if (message != null)
            {
                await ProcessResumeAsync(message, stoppingToken);
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

### AI Integration

#### Azure OpenAI GPT-4o
- **Model**: `gpt-4o` (Azure deployment)
- **Temperature**: 0.7 (balanced creativity)
- **TopP**: 0.95 (nucleus sampling)
- **Function Calling**: Structured JSON output

**Analysis Flow**:
1. Extract text from PDF (Document Intelligence)
2. Build system prompt (from PromptTemplate)
3. Call GPT-4o with function calling
4. Parse structured response
5. Save suggestions and candidate info

**System Prompt Structure**:
```
You are a professional resume analyzer. Analyze the provided resume and return:
1. ATS score (0-100)
2. Candidate information (name, email, skills, etc.)
3. Improvement suggestions (categorized by priority)
```

### Configuration Management

#### Backend Configuration
- `appsettings.json`: Default/shared settings
- `appsettings.Development.json`: Local development (git-ignored)
- `appsettings.Production.json`: Production overrides
- Environment variables: Override for containerized deployments

#### Frontend Configuration
- `environment.ts`: Development API URL
- `environment.prod.ts`: Production API URL
- `proxy.conf.json`: Local dev proxy to avoid CORS

**Environment Files**:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5167/api'
};
```

### Docker & Deployment

#### Local Development with Docker
```bash
docker-compose up -d  # Start all services
docker-compose logs -f api  # View API logs
docker-compose down  # Stop all services
```

#### Docker Services
- **frontend**: Angular app on nginx (port 80)
- **api**: .NET API (port 5000)
- **sql**: SQL Server 2022 (port 1433)

#### Production Deployment
- **Azure App Service**: Backend API
- **Azure Static Web Apps**: Frontend SPA
- **Azure SQL Database**: Managed database
- **Terraform**: Infrastructure as Code (see `terraform/`)

### CI/CD Pipeline

#### GitHub Actions Workflows
- **Backend CI**: Build, test, publish .NET app
- **Frontend CI**: Build, test Angular app
- **Terraform**: Deploy Azure infrastructure
- **Pre-commit**: Secret scanning, linting

**Branch Strategy**:
- `main`: Production-ready code
- `feature/*`: Feature development
- `bugfix/*`: Bug fixes
- `hotfix/*`: Production hotfixes

**Commit Convention**:
```
feat: Add user dashboard component
fix: Resolve JWT token expiration issue
docs: Update architecture documentation
test: Add unit tests for AuthService
refactor: Simplify resume upload logic
```

### Performance Optimization

#### Backend
- **Async I/O**: Use async/await for all I/O operations
- **Query Optimization**: Use `.AsNoTracking()` for read-only queries
- **Caching**: Consider response caching for expensive operations
- **Connection Pooling**: EF Core manages automatically

#### Frontend
- **Lazy Loading**: Load feature modules on demand
- **Change Detection**: Use OnPush strategy for components
- **RxJS Operators**: Use `shareReplay()` for shared observables
- **Image Optimization**: Lazy load images, use WebP format

### Accessibility

#### Frontend Requirements
- **Semantic HTML**: Use appropriate HTML5 elements
- **ARIA Labels**: Add labels for screen readers
- **Keyboard Navigation**: Ensure all interactions are keyboard-accessible
- **Color Contrast**: WCAG AA compliance (4.5:1 ratio)

### Logging & Monitoring

#### Backend Logging (Serilog)
```csharp
_logger.LogInformation("Resume {ResumeId} uploaded by user {UserId}", resumeId, userId);
_logger.LogWarning("Failed to extract text from PDF {FileName}", fileName);
_logger.LogError(ex, "Error analyzing resume {ResumeId}", resumeId);
```

#### Application Insights
- Automatic request/dependency tracking
- Custom telemetry for business events
- Exception tracking

### Documentation Requirements

When adding new features:
1. Update relevant documentation in `docs/`
2. Add JSDoc/XML comments for public APIs
3. Update README.md if adding new dependencies
4. Document breaking changes in CHANGELOG.md

### Common Patterns

#### Adding a New Feature (Backend)
1. Create command/query in `Application/Features/{Feature}/`
2. Create validator class
3. Create handler class implementing `IRequestHandler<TRequest, TResponse>`
4. Register in DI (if needed)
5. Create controller endpoint in `API/Controllers/`
6. Add unit tests in `CVAnalyzer.UnitTests/`
7. Add integration tests if needed

#### Adding a New Feature (Frontend)
1. Generate component: `ng generate component features/{feature}/{component}`
2. Define route in `app.routes.ts`
3. Create service if needed: `ng generate service core/services/{service}`
4. Define TypeScript interfaces in `core/models/`
5. Add to navigation if needed
6. Write unit tests

### Troubleshooting

#### Common Issues
- **CORS errors**: Check CORS configuration in `Program.cs`, verify proxy settings
- **JWT validation fails**: Check secret key, issuer, audience match in both API and config
- **Database migration errors**: Ensure connection string is correct, run `dotnet ef database update`
- **Azure service errors**: Verify API keys in Key Vault, check service health

#### Debug Logs
- **Backend**: Set `Logging:LogLevel:Default` to `Debug` in appsettings.json
- **Frontend**: Use `ng serve --verbose` for detailed build output

## Project-Specific Context

### Current Development Status
- **Branch**: `feature/user-story-1-guest-upload`
- **User Story 2**: Authentication system (11/16 acceptance criteria complete, 69%)
- **Pending Features**: Email verification, registration prompt modal

### Known Limitations
- Guest resume data expires after 24 hours
- JWT tokens expire after 60 minutes (no refresh token yet)
- Email verification not implemented (planned)
- Multi-language support partially implemented

### Future Roadmap
- **Durable Agents**: Migrate to stateful AI agents (see `docs/DURABLE_AGENTS_ROADMAP.md`)
- **Email Verification**: Complete email verification flow
- **Refresh Tokens**: Implement token refresh mechanism
- **Resume Versioning**: Track multiple versions of same resume
- **Analytics Dashboard**: Advanced statistics and trends

## Quick Reference

### Run Backend Locally
```bash
cd backend/src/CVAnalyzer.API
dotnet restore
dotnet ef database update --project ../CVAnalyzer.Infrastructure
dotnet run
```

### Run Frontend Locally
```bash
cd frontend
npm install
npm start  # Runs on http://localhost:4200
```

### Run Tests
```bash
# Backend
cd backend
dotnet test

# Frontend
cd frontend
npm test
```

### Create Migration
```bash
cd backend/src/CVAnalyzer.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../CVAnalyzer.API
```

### Docker Quick Start
```bash
docker-compose up -d
# Access frontend at http://localhost
# Access API at http://localhost:5000
```

## Additional Resources

- **Architecture**: See `docs/ARCHITECTURE.md`
- **Security**: See `docs/SECURITY.md`
- **Terraform**: See `docs/TERRAFORM.md`
- **Git Workflow**: See `docs/GIT_WORKFLOW.md`
- **DevOps**: See `docs/DEVOPS.md`
