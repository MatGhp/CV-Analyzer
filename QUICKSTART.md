# Quick Start Guide - CV Analyzer Platform

Get the CV Analyzer platform running in 5 minutes with Docker Compose.

## Prerequisites

- **Docker Desktop** (with Docker Compose)
- **Azure OpenAI** resource with GPT-4o deployment
- **Azure Document Intelligence** resource
- **Azure Storage Account** with blob and queue services
- **Azure SQL Database**
- **Azure credentials** (API keys for local dev, Managed Identity for production)

## Step 1: Clone and Configure

```bash
# Clone the repository
git clone https://github.com/MatGhp/CV-Analyzer-Backend.git
cd CV-Analyzer-Backend

# Copy environment template
cp .env.example .env
```

## Step 2: Configure Azure Services

Edit `backend/src/CVAnalyzer.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-sql-server.database.windows.net;Database=cvanalyzer-db-dev;User Id=cvadmin_dev;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True"
  },
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_STORAGE;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net"
  },
  "DocumentIntelligence": {
    "Endpoint": "https://YOUR_DOCINTEL.cognitiveservices.azure.com/",
    "ApiKey": "YOUR_KEY"
  },
  "Agent": {
    "Endpoint": "https://swedencentral.api.cognitive.microsoft.com/",
    "Deployment": "gpt-4o",
    "ApiKey": "YOUR_OPENAI_KEY",
    "Temperature": 0.7,
    "TopP": 0.95
  },
  "Queue": {
    "ResumeAnalysisQueueName": "resume-analysis"
  }
}
```

**Get Azure Keys**:
```powershell
# Azure OpenAI
az cognitiveservices account keys list --name ai-cvanalyzer-dev --resource-group rg-cvanalyzer-dev

# Document Intelligence
az cognitiveservices account keys list --name cvanalyzer-dev-docintel --resource-group rg-cvanalyzer-dev

# Storage Account
az storage account keys list --account-name cvanalyzerdevs4b3 --resource-group rg-cvanalyzer-dev
```

## Step 3: Build and Run

```bash
# Build all services (first time only)
docker-compose build

# Start all services
docker-compose up -d
```

This starts 3 services:
- **Frontend** (Angular) → `http://localhost:4200`
- **Backend** (.NET API + AgentService) → `http://localhost:5000`
- **SQL Server** → `localhost:1433`

## Step 4: Verify Services

Check all services are healthy:

```bash
docker-compose ps
```

Expected output:
```
NAME                STATUS         PORTS
frontend            Up (healthy)   0.0.0.0:4200->80/tcp
api                 Up             0.0.0.0:5000->8080/tcp
ai-service          Up (healthy)   0.0.0.0:8000->8000/tcp
sqlserver           Up             0.0.0.0:1433->1433/tcp
```

## Step 5: Access the Application

- **Frontend**: http://localhost:4200
- **API Documentation**: http://localhost:5000/swagger
 

## Common Commands

```bash
# View logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f frontend
docker-compose logs -f api

# Stop all services
docker-compose down

# Rebuild specific service
docker-compose build frontend
docker-compose up -d frontend

# Remove all data (including database)
docker-compose down -v
```

## PowerShell Helper Script

Use the PowerShell script for easier management:

```powershell
# Build and start
.\docker-manage.ps1 -Build -Up

# View logs
.\docker-manage.ps1 -Logs

# Stop services
.\docker-manage.ps1 -Down

# Production mode
.\docker-manage.ps1 -Environment prod -Up
```

## Troubleshooting

### Agent calls fail

Check API logs and Agent configuration in `appsettings.*.json`:
```bash
docker-compose logs api
```

### Database connection errors

Verify SQL Server is running:
```bash
docker-compose ps sqlserver
```

Check connection string in API logs:
```bash
docker-compose logs api | grep "Connection"
```

### Frontend can't reach API

Verify nginx proxy configuration in `frontend/nginx.conf`:
- API proxy: `/api/` → `http://api:8080/api/`

### Port conflicts

If ports are already in use, modify `docker-compose.yml`:
```yaml
ports:
  - "8080:80"  # Change 4200 to 8080 for frontend
  - "5001:8080"  # Change 5000 to 5001 for API
```

## Development Workflow

### Run services individually

**Frontend only** (requires backend running):
```bash
cd frontend
npm install
npm start
# Access at http://localhost:4200
```

**Backend only**:
```bash
cd backend
dotnet run --project src/CVAnalyzer.API
# Access at http://localhost:5000
```

 

### Database migrations

```bash
# Apply migrations
docker-compose exec api dotnet ef database update

# Create new migration
cd backend
dotnet ef migrations add MigrationName --project src/CVAnalyzer.Infrastructure --startup-project src/CVAnalyzer.API
```

## Next Steps

- Read [Architecture Documentation](docs/ARCHITECTURE.md)
- Configure [Azure Infrastructure with Terraform](terraform/README.md)
- Review [Backend Documentation](backend/README.md)
- Review [Frontend Documentation](frontend/README.md)
- Read [Agent Framework Guide](docs/AGENT_FRAMEWORK.md)

## Support

For issues:
1. Check service logs: `docker-compose logs -f <service-name>`
2. Verify `.env` configuration
3. Ensure Azure resources are deployed
4. Review service-specific README files

## License

MIT License - See [LICENSE](LICENSE) file
