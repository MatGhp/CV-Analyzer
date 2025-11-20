# Local Development Secrets Management

**⚠️ CRITICAL: This document is tracked in git. NEVER commit actual API keys or connection strings!**

## Overview

This project uses a **dual-secrets strategy** for local development:

- **Docker Compose**: Secrets in `.env` file (gitignored)
- **.NET CLI (`dotnet run`)**: Secrets in `appsettings.Development.json` (gitignored)
- **Production**: Azure Key Vault + Managed Identity (no secrets in config)

## 🚀 Quick Start Guide

### Option 1: Docker Compose (Recommended)

**Prerequisites:**
- Docker Desktop installed and running
- Azure CLI installed (`az --version`)
- Logged into Azure CLI (`az login`)

**Step 1: Create `.env` file from template**

```powershell
# PowerShell
Copy-Item .env.example .env

# Or manually create .env file in repository root
```

**Step 2: Retrieve Azure credentials**

```powershell
# Azure Storage Connection String
az storage account show-connection-string `
  --name cvanalyzerdevp3nfnnux `
  --resource-group rg-cvanalyzer-dev `
  --query connectionString `
  --output tsv

# Azure OpenAI API Key
az cognitiveservices account keys list `
  --name ai-cvanalyzer-dev `
  --resource-group rg-cvanalyzer-dev `
  --query key1 `
  --output tsv

# Document Intelligence API Key
az cognitiveservices account keys list `
  --name cvanalyzer-dev-docintel `
  --resource-group rg-cvanalyzer-dev `
  --query key1 `
  --output tsv
```

**Step 3: Paste credentials into `.env` file**

Open `.env` in your editor and replace placeholders:

```bash
AZURE_STORAGE_CONNECTION_STRING=<paste connection string from step 2>
AGENT_API_KEY=<paste OpenAI key from step 2>
DOCUMENT_INTELLIGENCE_API_KEY=<paste Doc Intel key from step 2>
SQL_SA_PASSWORD=YourStrong@Passw0rd  # Or choose your own password
```

**Step 4: Start the full stack**

```powershell
docker-compose up -d
```

**Step 5: Verify services are running**

```powershell
# Check container status
docker-compose ps

# Test endpoints
curl http://localhost:4200  # Frontend
curl http://localhost:5000/health  # Backend API
```

**Access Points:**
- 🌐 **Frontend**: http://localhost:4200
- 🔧 **Backend API**: http://localhost:5000
- 🗄️ **SQL Server**: localhost:1433 (User: `sa`, Password: from `.env` file `SQL_SA_PASSWORD`)
- ☁️ **Azure Services**: Real dev environment (storage, AI, Document Intelligence)

### Option 2: .NET CLI (Backend Only)

**Prerequisites:**
- .NET 10 SDK installed
- SQL Server LocalDB or Docker SQL Server running
- Azure CLI logged in

**Step 1: Navigate to API project**

```powershell
cd backend/src/CVAnalyzer.API
```

**Step 2: Create/Update `appsettings.Development.json`**

Retrieve credentials (same Azure CLI commands as Option 1) and create the file:

```json
{
  "Agent": {
    "Endpoint": "https://ai-cvanalyzer-dev.openai.azure.com/",
    "Deployment": "gpt-4o",
    "ApiKey": "<paste Azure OpenAI key>",
    "Temperature": 0.7,
    "TopP": 0.95
  },
  "AzureStorage": {
    "ConnectionString": "<paste Azure Storage connection string>"
  },
  "DocumentIntelligence": {
    "Endpoint": "https://cvanalyzer-dev-docintel.cognitiveservices.azure.com/",
    "ApiKey": "<paste Document Intelligence key>"
  },
  "Queue": {
    "ResumeAnalysisQueueName": "resume-analysis"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CVAnalyzerDb;Trusted_Connection=True;"
  },
  "UseKeyVault": false
}
```

**Step 3: Run the backend**

```powershell
dotnet run
```

**Access:**
- Backend API: https://localhost:5001 (or check console output)
- Swagger UI: https://localhost:5001/swagger (Development only)

## Current Working Configuration (as of Nov 20, 2025)

### Azure Resources (Dev Environment)

| Resource Type | Resource Name | Endpoint/Details |
|--------------|---------------|------------------|
| Azure OpenAI | `ai-cvanalyzer-dev` | `https://ai-cvanalyzer-dev.openai.azure.com/` |
| Document Intelligence | `cvanalyzer-dev-docintel` | `https://cvanalyzer-dev-docintel.cognitiveservices.azure.com/` |
| Storage Account | `cvanalyzerdevp3nfnnux` | Blob, Queue, Table endpoints |
| SQL Server | `sql-cvanalyzer-dev` | `sql-cvanalyzer-dev.database.windows.net` |
| Resource Group | `rg-cvanalyzer-dev` | Sweden Central |

### Docker Compose Configuration

Secrets are stored in `.env` file (gitignored) and referenced in `docker-compose.yml`:

**`.env` file (gitignored - safe for real credentials):**
```bash
AZURE_STORAGE_CONNECTION_STRING=<your connection string>
AGENT_API_KEY=<your OpenAI key>
DOCUMENT_INTELLIGENCE_API_KEY=<your Doc Intel key>
```

**`docker-compose.yml` (tracked in git - NO real secrets):**
```yaml
api:
  environment:
    - Agent__Endpoint=https://ai-cvanalyzer-dev.openai.azure.com/
    - Agent__ApiKey=${AGENT_API_KEY}  # From .env file
    - DocumentIntelligence__ApiKey=${DOCUMENT_INTELLIGENCE_API_KEY}  # From .env
    - AzureStorage__ConnectionString=${AZURE_STORAGE_CONNECTION_STRING}  # From .env
```

**Security:** `.env` file is gitignored, `docker-compose.yml` contains NO secrets.

## Security Best Practices

### ✅ DO:
- Use `.env` file for Docker secrets (gitignored)
- Use `appsettings.Development.json` for local .NET development (gitignored)
- Store production secrets in Azure Key Vault (never in code)
- Regenerate keys if accidentally committed
- Use Managed Identity in production (no API keys)
- Keep `docker-compose.yml` free of real secrets (use environment variable references only)

### ❌ DON'T:
- Commit `.env` file (already gitignored)
- Commit `appsettings.Development.json` (already gitignored)
- Put real API keys in `docker-compose.yml` (use ${VARIABLE} references)
- Share API keys in chat/email (use Azure CLI commands instead)
- Use the same keys for dev and production

## 🔧 Troubleshooting

### Environment Variables Not Loading

**Symptom:** Container starts but fails to connect to Azure services

**Solution:**
```powershell
# Verify .env file exists in repository root
Get-Content .env

# Restart containers to pick up .env changes
docker-compose down
docker-compose up -d

# Check environment variables are loaded
docker-compose exec api printenv | Select-String "AGENT_API_KEY"
```

### "DefaultAzureCredential failed" Error

**Symptom:** Authentication errors in logs

**Cause:** Missing API key in configuration

**Solution:**
- **Docker:** Ensure `.env` file has `AGENT_API_KEY` set
- **dotnet run:** Ensure `appsettings.Development.json` has `ApiKey` property
- The app automatically uses API key (local) or Managed Identity (production)

### "Access denied due to invalid subscription key"

**Symptom:** 401 Unauthorized from Azure OpenAI or Document Intelligence

**Solution:**
```powershell
# Verify endpoint matches resource name
# CORRECT: https://ai-cvanalyzer-dev.openai.azure.com/
# WRONG: https://swedencentral.api.cognitive.microsoft.com/

# Regenerate and get fresh key
az cognitiveservices account keys list `
  --name ai-cvanalyzer-dev `
  --resource-group rg-cvanalyzer-dev
```

### "Name or service not known" (DNS Errors)

**Symptom:** Cannot resolve storage account hostname

**Cause:** Storage account name outdated or incorrect

**Solution:**
```powershell
# Get current storage account name
az storage account list --resource-group rg-cvanalyzer-dev --query "[].name" --output tsv

# Update .env with correct connection string
az storage account show-connection-string `
  --name <actual-name> `
  --resource-group rg-cvanalyzer-dev
```

### Container Unhealthy / Health Check Failing

**Symptom:** `docker-compose ps` shows "unhealthy" status

**Solution:**
```powershell
# Check container logs for errors
docker-compose logs api --tail 50

# Common causes:
# 1. Missing .env file → Create from .env.example
# 2. Invalid credentials → Re-run Azure CLI commands
# 3. Database not ready → Wait 30s and check again
```

### Database Connection Issues

**Symptom:** "Cannot connect to SQL Server" or "Database does not exist"

**Solution:**
```powershell
# For Docker Compose (SQL Server in container)
docker-compose logs sqlserver --tail 20

# For LocalDB (dotnet run)
sqllocaldb info mssqllocaldb
sqllocaldb start mssqllocaldb

# Apply migrations manually if needed
cd backend/src/CVAnalyzer.API
dotnet ef database update
```

## 🔄 Key Rotation Procedure

When Azure credentials are rotated (security best practice: every 90 days):

**Step 1: Regenerate keys in Azure**

```powershell
# Regenerate Azure OpenAI key
az cognitiveservices account keys regenerate `
  --name ai-cvanalyzer-dev `
  --resource-group rg-cvanalyzer-dev `
  --key-name key1

# Regenerate Document Intelligence key
az cognitiveservices account keys regenerate `
  --name cvanalyzer-dev-docintel `
  --resource-group rg-cvanalyzer-dev `
  --key-name key1

# Regenerate Storage Account key
az storage account keys renew `
  --account-name cvanalyzerdevp3nfnnux `
  --resource-group rg-cvanalyzer-dev `
  --key primary
```

**Step 2: Update local configuration**

```powershell
# For Docker: Update .env file (retrieve new keys with Azure CLI)
# For .NET CLI: Update appsettings.Development.json
```

**Step 3: Update CI/CD secrets**

Update GitHub Secrets in repository settings (Settings → Secrets → Actions)

**Step 4: Restart services**

```powershell
docker-compose down
docker-compose up -d
```

## 📚 Additional Resources

### Verification Commands

```powershell
# Verify .gitignore is working (should NOT show .env or appsettings.Development.json)
git status

# Test pre-commit hook (should block secrets)
git add -A
git commit -m "test"  # Should fail if real secrets detected

# Check Docker container logs
docker-compose logs api --tail 50
docker-compose logs frontend --tail 50

# Verify Azure CLI authentication
az account show
```

### Common Azure CLI Commands

```powershell
# List all resources in dev environment
az resource list --resource-group rg-cvanalyzer-dev --output table

# Check resource status
az cognitiveservices account show --name ai-cvanalyzer-dev --resource-group rg-cvanalyzer-dev
az storage account show --name cvanalyzerdevp3nfnnux --resource-group rg-cvanalyzer-dev

# Test connectivity to Azure services
az storage container list --account-name cvanalyzerdevp3nfnnux
```

## Related Files

- `.gitignore` - Excludes `.env` and `appsettings.Development.json`
- `.env` - Local Docker secrets (gitignored - NOT committed)
- `.env.example` - Template for .env file (tracked in git)
- `docker-compose.yml` - Local development stack (NO secrets, uses ${VARIABLES})
- `backend/src/CVAnalyzer.API/appsettings.json` - Template (no secrets)
- `backend/src/CVAnalyzer.API/appsettings.Development.json` - Local .NET secrets (gitignored)
- `.github/copilot-instructions.md` - Security rules and branch protection policy
