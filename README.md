# CV Analyzer

Enterprise-grade resume optimization platform powered by Angular 20 (frontend) and .NET 9 (backend + integrated Agent Service using Microsoft Agent Framework + Azure OpenAI).

## 📚 Documentation Index

Primary docs live under `docs/`. Start here:

| Doc | Purpose |
|-----|---------|
| [docs/README.md](docs/README.md) | Full documentation index & navigation |
| [Architecture](docs/ARCHITECTURE.md) | System & code architecture overview |
| [Security](docs/SECURITY.md) | Guardrails, secret management, hooks |
| [DevOps](docs/DEVOPS.md) | Pipelines, environments, troubleshooting |
| [Terraform](docs/TERRAFORM.md) | Infrastructure as Code details |
| [Agent Framework](docs/AGENT_FRAMEWORK.md) | Using Microsoft Agent Framework in C# |
| [Refactoring Summary](docs/REFACTORING_SUMMARY.md) | KISS improvements & rationale |
| [Git Workflow](docs/GIT_WORKFLOW.md) | Streamlined branching, commits & secret-safe practices |

## 🏗️ High-Level Architecture

```
User → Angular Frontend (nginx)
       ↓ (HTTP proxy: /api/*)
       .NET API (CQRS + Clean Architecture)
       ↓
       Integrated AgentService (Azure.AI.OpenAI SDK)
       ↓
       Azure OpenAI GPT-4o (Function Calling)
       ↓
       Azure Document Intelligence (text extraction)
       ↓
       Structured JSON resume analysis
```

**Background Processing**: Queue-based async analysis via Azure Storage Queues + BackgroundService worker.
**Local Dev**: Frontend proxy configuration routes `/api/*` to backend.

## 🚀 Quick Start (Local)

### Prerequisites
- .NET 9 SDK
- Node.js 20+
- Azure CLI (logged in: `az login`)
- Azure resources deployed (see `terraform/`)

### Backend Setup
```powershell
cd backend/src/CVAnalyzer.API

# Configure appsettings.Development.json with:
# - Azure OpenAI endpoint and API key
# - Azure Storage connection string
# - Document Intelligence endpoint and key
# - SQL connection string

# Run migrations
dotnet ef database update --project ../CVAnalyzer.Infrastructure

# Start backend (port 5167)
dotnet run
```

### Frontend Setup
```bash
cd frontend
npm install
npm start  # Port 4200 with proxy to backend
```

### Docker Compose (Full Stack)
```bash
docker-compose up -d
# Frontend: http://localhost:4200
# API: http://localhost:5000
# SQL: localhost:1433
```

For detailed setup see `RUNNING_LOCALLY.md`.

## 🔐 Security Snapshot

- Managed identity & Azure OpenAI (no API keys in code)
- Pre-commit secret scanning hook
- CI secret scanning gate blocks deployments
- All inputs validated (FluentValidation) before handlers run

Full details: `docs/SECURITY.md`.

## 🧪 Tests

Backend & Agent logic:
```bash
cd backend
dotnet test
```

## 🤝 Contributing

1. Read `docs/SECURITY.md` and `.github/copilot-instructions.md`
2. Follow Clean Architecture & Angular signals patterns
3. Add/adjust tests with each behavioral change
4. Update docs when adding new public endpoints or infra
5. Use Conventional Commits (template: `.gitmessage.txt`); configure: `git config commit.template .gitmessage.txt`
6. See `docs/GIT_WORKFLOW.md` for branching & rebase guidance

## 📄 License

MIT License — see `LICENSE`.

---
Last Updated: November 7, 2025
