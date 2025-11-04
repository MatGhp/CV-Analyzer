# CV Analyzer - Microservices Platform

Enterprise-grade resume optimization platform with AI-powered analysis, built using microservices architecture with .NET 9 and Python.

## 🏗️ Architecture Overview

This monorepo contains two microservices that work together to provide intelligent resume analysis:

```
┌─────────────────────────────────────────────────────────────┐
│                     CV Analyzer Platform                     │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────────────────────────────────┐          │
│  │  Angular Frontend (frontend/)                │          │
│  │  - Angular 20 (Zoneless + Signals)          │          │
│  │  - Standalone Components                     │          │
│  │  - Nginx Reverse Proxy                       │          │
│  └────────────┬─────────────────────────────────┘          │
│               │ HTTP                                         │
│               ▼                                              │
│  ┌──────────────────────────────────────────────┐          │
│  │  .NET API (backend/)                         │          │
│  │  - Controllers & Routing                     │          │
│  │  - CQRS with MediatR                        │          │
│  │  - EF Core + SQL Database                   │          │
│  │  - Blob Storage                             │          │
│  │  - Security & Authentication                │          │
│  └────────────┬─────────────────────────────────┘          │
│               │ HTTP                                         │
│               ▼                                              │
│  ┌──────────────────────────────────────────────┐          │
│  │  Python AI Service (ai-service/)             │          │
│  │  - FastAPI REST API                          │          │
│  │  - Microsoft Agent Framework                 │          │
│  │  - Azure AI Foundry Client                   │          │
│  │  - GPT-4o Integration                        │          │
│  └────────────┬─────────────────────────────────┘          │
│               │                                              │
│               ▼                                              │
│       Azure AI Foundry (GPT-4o)                             │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

## 📁 Repository Structure

```
CV-Analyzer-Backend/
├── frontend/                   # Angular 20 Frontend
│   ├── src/app/
│   │   ├── core/              # Services, guards, interceptors
│   │   ├── features/          # Feature modules
│   │   └── shared/            # Shared components
│   ├── Dockerfile
│   ├── nginx.conf
│   └── README.md
│
├── backend/                    # .NET 9 Web API
│   ├── src/
│   │   ├── CVAnalyzer.API/
│   │   ├── CVAnalyzer.Application/
│   │   ├── CVAnalyzer.Domain/
│   │   └── CVAnalyzer.Infrastructure/
│   ├── tests/
│   ├── Dockerfile
│   ├── CVAnalyzer.sln
│   └── README.md
│
├── ai-service/                 # Python AI Service
│   ├── app/
│   │   ├── main.py            # FastAPI application
│   │   ├── agent.py           # Agent Framework logic
│   │   ├── models.py          # Pydantic models
│   │   └── config.py          # Configuration
│   ├── requirements.txt
│   ├── Dockerfile
│   └── README.md
│
├── terraform/                  # Infrastructure as Code (shared)
│   ├── main.tf
│   ├── variables.tf
│   ├── outputs.tf
│   └── modules/
│       ├── app-service/
│       ├── key-vault/
│       └── sql-database/
│
├── docker-compose.yml          # Local development orchestration
├── .env.example               # Environment variables template
└── .github/
    └── copilot-instructions.md # AI coding agent guidelines
```

## 🚀 Quick Start

### Prerequisites

- **Frontend**: Node.js 18+, npm
- **Backend**: .NET 9 SDK, Docker
- **AI Service**: Python 3.11+, Docker
- **Azure**: Azure CLI, Azure subscription
- **Database**: SQL Server (LocalDB for dev)

### Local Development (Docker Compose)

Run all services together:

```bash
# From repository root
docker-compose up -d
```

This starts:

- Angular Frontend on `http://localhost:4200`
- .NET API on `http://localhost:5000`
- Python AI Service on `http://localhost:8000`
- SQL Server on `localhost:1433`

Access:

- **Frontend Application**: `http://localhost:4200`
- **Swagger UI (API)**: `http://localhost:5000/swagger`
- **AI Service Docs**: `http://localhost:8000/docs`

### Frontend (Angular)

```bash
cd frontend/cv-analyzer-frontend
npm install
npm start
```

See [frontend/cv-analyzer-frontend/FRONTEND_README.md](frontend/cv-analyzer-frontend/FRONTEND_README.md) for detailed documentation.

### Backend (.NET API)

```bash
cd backend
dotnet restore
dotnet build
cd src/CVAnalyzer.API
dotnet run
```

See [backend/README.md](backend/README.md) for detailed documentation.

### AI Service (Python)

```bash
cd ai-service
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt --pre
python -m app.main
```

See [ai-service/README.md](ai-service/README.md) for detailed documentation.

## 🧪 Testing

### Backend Tests
```bash
cd backend
dotnet test
```

### AI Service Tests
```bash
cd ai-service
pytest
```

## 🌐 Azure Deployment

Deploy both services to Azure using Terraform:

```bash
cd terraform

# Set required environment variables
export TF_VAR_sql_admin_password="YourSecurePassword123!"

# Initialize Terraform
terraform init

# Deploy to development
terraform plan -var-file="environments/dev.tfvars"
terraform apply -var-file="environments/dev.tfvars"
```

This deploys:

- Azure Resource Group
- Azure SQL Database
- Azure Key Vault
- Azure App Service (.NET API)
- Azure Container Instance (Python AI Service)
- Azure AI Foundry Project
- GPT-4o Model Deployment

See [terraform/README.md](terraform/README.md) for complete deployment guide.

## 🔧 Technology Stack

### Backend (.NET API)
- **.NET 9**: Latest LTS framework
- **EF Core 9**: ORM with Code-First migrations
- **MediatR**: CQRS and mediator pattern
- **FluentValidation**: Request validation
- **Serilog**: Structured logging
- **Swagger/OpenAPI**: API documentation
- **xUnit**: Testing framework

### AI Service (Python)
- **FastAPI**: Modern async web framework
- **Microsoft Agent Framework**: AI agent orchestration
- **Azure AI Foundry**: Model hosting and deployment
- **GPT-4o**: Large language model
- **Pydantic**: Data validation
- **Uvicorn**: ASGI server

### Infrastructure
- **Azure SQL Database**: Cloud-native database
- **Azure Key Vault**: Secrets management
- **Azure Blob Storage**: File storage
- **Azure AI Foundry**: AI model hosting
- **Terraform**: Infrastructure as Code
- **Docker**: Containerization

## 📊 API Endpoints

### .NET API (Backend)
- `GET /api/health` - Health check
- `POST /api/resumes` - Upload resume
- `GET /api/resumes/{id}` - Get resume details

### Python AI Service
- `GET /health` - Health check with AI connectivity
- `POST /analyze` - Analyze resume content

## 🔐 Security

- Azure Key Vault for secrets management
- Managed Identity for passwordless authentication
- FluentValidation for input validation
- HTTPS enforcement
- CORS configuration
- Structured logging (no sensitive data)

See [backend/.github/security-guardrails.md](backend/.github/security-guardrails.md) for complete security guidelines.

## 🏗️ Architecture Patterns

### Backend
- **Clean Architecture**: Clear separation of concerns
- **CQRS**: Command Query Responsibility Segregation
- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: Loose coupling
- **Pipeline Behaviors**: Cross-cutting concerns

### AI Service
- **Microservices**: Specialized AI analysis service
- **Agent Pattern**: Microsoft Agent Framework
- **API Gateway Pattern**: FastAPI routing
- **Singleton Pattern**: Agent instance management

## 📈 Development Workflow

### Feature Development
1. Create feature branch: `git checkout -b feature/my-feature`
2. Develop in appropriate service (backend/ or ai-service/)
3. Run tests locally
4. Update documentation
5. Create pull request

### CI/CD
- Backend: `.github/workflows/backend-ci.yml` (planned)
- AI Service: `.github/workflows/ai-service-ci.yml` (planned)
- Terraform: Manual deployment for now

## 🤝 Contributing

1. Follow existing code structure and patterns
2. Read `.github/copilot-instructions.md` for coding guidelines
3. Follow security guardrails in `.github/security-guardrails.md`
4. Write tests for new features
5. Update documentation

## 📚 Documentation

- [Backend README](backend/README.md)
- [AI Service README](ai-service/README.md)
- [Terraform Guide](terraform/README.md)
- [Backend Architecture](backend/ARCHITECTURE.md)

## 🐛 Troubleshooting

### Docker Compose Issues
```bash
# Rebuild containers
docker-compose down
docker-compose build --no-cache
docker-compose up
```

### Backend Issues
- Ensure SQL Server is running
- Check Key Vault configuration
- Verify connection strings

### AI Service Issues
- Verify Azure AI Foundry endpoint
- Check authentication credentials
- Ensure Python dependencies installed with `--pre` flag

## 📄 License

MIT License - See [LICENSE](backend/LICENSE)

## 🎯 Roadmap

- [x] .NET API with Clean Architecture
- [x] Python AI Service with Agent Framework
- [x] Docker Compose local development
- [x] Terraform infrastructure
- [ ] Frontend application
- [ ] CI/CD pipelines
- [ ] Kubernetes deployment
- [ ] Monitoring and observability
- [ ] Performance optimization
- [ ] Advanced AI features

---

**Version**: 1.0.0  
**Last Updated**: November 4, 2025
