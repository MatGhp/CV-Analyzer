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

## 🏗️ High-Level Architecture

```
User → Angular Frontend (nginx)
       ↓ (internal DNS: http://ca-cvanalyzer-api:8080)
       .NET API + AgentService (CQRS + Microsoft Agent Framework)
       ↓
       Azure OpenAI (GPT-4o deployment)
       ↓
       Structured JSON resume analysis
```

Internal communication uses Azure Container Apps internal DNS — no environment-specific URLs required.

## 🚀 Quick Start (Local)

```bash
# From repo root
docker-compose up -d

# Frontend: http://localhost:4200
# API (+ AgentService): http://localhost:5000
# SQL Server: localhost:1433
```

> NOTE: Set sensitive values (e.g. SQL admin password) via a local environment variable or secret manager. Do not commit example commands containing credential variable names.

For detailed setup (manual service runs, migrations, testing) see `docs/README.md`.

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

## 📄 License

MIT License — see `LICENSE`.

---
Last Updated: November 7, 2025
