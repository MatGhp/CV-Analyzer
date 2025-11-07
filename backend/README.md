# CV Analyzer Backend

A clean, enterprise-grade Web API for the CV Analyzer platform built with .NET 9, Clean Architecture principles, and an integrated Agent Service (Microsoft Agent Framework + Azure OpenAI).

**Note**: This is part of a monorepo. See the [root README](../README.md) and `docs/ARCHITECTURE.md` for full platform context. The former separate Python AI service has been replaced by the integrated AgentService.

## Architecture

This project follows Clean Architecture with clear separation of concerns:

- **Domain Layer**: Core business entities and exceptions
- **Application Layer**: Business logic, CQRS patterns with MediatR, FluentValidation
- **Infrastructure Layer**: EF Core 9, Azure integrations (SQL, Key Vault), external services
- **API Layer**: Controllers, middleware, Serilog logging

## Technology Stack

- **.NET 9**: Latest LTS framework
- **EF Core 9**: ORM with Code-First migrations
- **Azure SQL Database**: Cloud-native database
- **Azure Key Vault**: Secure secrets management
- **Serilog**: Structured logging
- **MediatR**: CQRS and mediator pattern
- **FluentValidation**: Request validation
- **Swagger/OpenAPI**: API documentation
- **xUnit**: Testing framework

## Getting Started

### Prerequisites

- .NET 9 SDK
- Docker and Docker Compose (for containerized deployment)
- SQL Server (LocalDB for development)
- Azure CLI (for cloud deployment)

### Local Development

1. Clone the repository (from repo root):
   ```bash
   git clone https://github.com/MatGhp/CV.Analyzer.git
   cd CV.Analyzer/backend
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

4. Run the API:
   ```bash
   cd src/CVAnalyzer.API
   dotnet run
   ```

5. Access Swagger UI at `https://localhost:5001/swagger`

### Docker Deployment

For local development with all services (API + AI Service + SQL Server):

```bash
# From repository root
cd ..
docker-compose up -d
```

Access the API at `http://localhost:5000`

**Note**: Docker Compose configuration is at the repository root level and starts the frontend, backend API (with AgentService), and SQL Server.

### Running Tests

```bash
dotnet test
```

## Azure Deployment

Infrastructure as Code using Terraform is provided at the repository root.

See [../terraform/README.md](../terraform/README.md) for deployment instructions.

## Project Structure

```
├── src/
│   ├── CVAnalyzer.Domain/          # Core entities and exceptions
│   ├── CVAnalyzer.Application/     # Business logic and interfaces
│   ├── CVAnalyzer.Infrastructure/  # Data access and external services
│   └── CVAnalyzer.API/            # Web API controllers and middleware
├── tests/
│   ├── CVAnalyzer.UnitTests/      # Unit tests
│   └── CVAnalyzer.IntegrationTests/ # Integration tests
├── terraform/                      # Azure infrastructure
├── Dockerfile                      # Production container
└── docker-compose.yml             # Local development stack
```

## Core API Endpoints (Representative)

- `GET /api/health` - Health check
- `POST /api/resumes` - Upload a resume
- `GET /api/resumes/{id}` - Get resume by ID (includes suggestions)
- `POST /api/resumes/{id}/analyze` - Triggers agent-based resume analysis

## Configuration

Key configuration settings in `appsettings.json` / environment variables:

- `ConnectionStrings:DefaultConnection` - Database connection
- `UseKeyVault` - Enable Azure Key Vault integration
- `KeyVault:Uri` - Key Vault URI
- `Agent:Endpoint` - Azure OpenAI endpoint
- `Agent:Deployment` - Model deployment name (e.g. gpt-4o-mini)

## Agent Framework Usage

See `docs/AGENT_FRAMEWORK.md` for detailed usage, DI patterns, and migration notes.

## License

MIT License.