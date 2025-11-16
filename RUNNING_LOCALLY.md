# Running Backend Locally with Azure Resources

**Audience**: Developers setting up local development environment with live Azure resources

**Alternative**: For quick Docker-based setup, see [`QUICKSTART.md`](QUICKSTART.md)

---

## Prerequisites ✅

- [x] **Azure CLI** installed and logged in (`az login`)
- [x] **.NET 10 SDK** installed ([download](https://dotnet.microsoft.com/download/dotnet/10.0))
- [x] **Azure resources** deployed via Terraform (`rg-cvanalyzer-dev`)
- [x] **SQL Server firewall** configured with your IP address

## Configuration

**Azure Resources (Dev Environment)**:

- **Storage Account**: `cvanalyzerdevs4b3` (Sweden Central)
- **Document Intelligence**: `cvanalyzer-dev-docintel`
- **SQL Server**: `sql-cvanalyzer-dev.database.windows.net`
- **Database**: `cvanalyzer-db-dev`
- **Azure OpenAI**: `ai-cvanalyzer-dev` (AIServices account)
  - Endpoint: `https://swedencentral.api.cognitive.microsoft.com/`
  - Deployment: `gpt-4o`
  - Model: `gpt-4o-2024-08-06`

## Setup Steps

### 1. Set SQL Server Password

You need to set the SQL admin password in `appsettings.Local.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=sql-cvanalyzer-dev.database.windows.net;Database=cvanalyzer-db-dev;User Id=cvadmin_dev;Password=YOUR_SQL_PASSWORD_HERE;TrustServerCertificate=True;Encrypt=True"
}
```

**Replace `YOUR_SQL_PASSWORD_HERE`** with the actual password you used when deploying the SQL Server.

> **Don't know the password?** You can reset it:
> ```powershell
> az sql server update --name sql-cvanalyzer-dev --resource-group rg-cvanalyzer-dev --admin-password "YourNewPassword123!"
> ```

### 2. Run Database Migrations

```powershell
cd backend/src/CVAnalyzer.API
dotnet ef database update --project ../CVAnalyzer.Infrastructure
```

This will create the necessary tables in your Azure SQL Database.

### 3. Configure Azure OpenAI

Add to `appsettings.Development.json`:

```json
"Agent": {
  "Endpoint": "https://swedencentral.api.cognitive.microsoft.com/",
  "Deployment": "gpt-4o",
  "ApiKey": "YOUR_API_KEY_HERE",
  "Temperature": 0.7,
  "TopP": 0.95
}
```

**Get API Key**:
```powershell
az cognitiveservices account keys list --name ai-cvanalyzer-dev --resource-group rg-cvanalyzer-dev --query key1 -o tsv
```

> **⚠️ Security Note**: 
> - **Local Development**: API keys are acceptable for simplicity
> - **Production/CI/CD**: ALWAYS use Managed Identity (no keys in code)
> - **Never commit** API keys to Git (pre-commit hook will block)
> - See [`docs/SECURITY.md`](docs/SECURITY.md) for full guidelines

### 4. Start the API

```powershell
# Set environment to use Development configuration
$env:ASPNETCORE_ENVIRONMENT = "Local"

# Run the API
dotnet run
```

The API will start on `https://localhost:5001` and `http://localhost:5000`.

### 4. Verify Health Endpoint

```powershell
curl http://localhost:5000/api/health
```

Expected response:
```json
{
  "status": "Healthy",
  "timestamp": "2025-11-13T19:30:00Z"
}
```

## What's Configured

### Azure Storage
- **Connection**: Using connection string authentication
- **Container**: `resumes` (already exists)
- **Queue**: `resume-analysis` (already exists)
- **Poison Queue**: `resume-analysis-poison`

### Document Intelligence
- **Endpoint**: `https://swedencentral.api.cognitive.microsoft.com/`
- **Authentication**: API Key (already configured)
- **Purpose**: PDF parsing for resume content extraction

### SQL Database
- **Server**: `sql-cvanalyzer-dev.database.windows.net`
- **Database**: `cvanalyzer-db-dev`
- **Firewall**: Your IP (193.27.220.9) is allowed

## Alternative: Use Managed Identity (Optional)

Instead of connection strings, you can use Azure Managed Identity:

1. Update `appsettings.Local.json`:
```json
"AzureStorage": {
  "UseManagedIdentity": true,
  "AccountName": "cvanalyzerdevs4b3"
},
"DocumentIntelligence": {
  "Endpoint": "https://swedencentral.api.cognitive.microsoft.com/",
  "UseManagedIdentity": true
}
```

2. Ensure your Azure CLI is logged in (already done ✅)

## Testing the API

### Upload a Resume (Example)

```powershell
$file = Get-Content "path/to/resume.pdf" -Raw -AsByteStream
$boundary = [System.Guid]::NewGuid().ToString()
$headers = @{"Content-Type" = "multipart/form-data; boundary=$boundary"}
$body = @"
--$boundary
Content-Disposition: form-data; name="file"; filename="resume.pdf"
Content-Type: application/pdf

$file
--$boundary
Content-Disposition: form-data; name="userId"

test-user-123
--$boundary--
"@

Invoke-RestMethod -Uri "http://localhost:5000/api/resumes/upload" -Method Post -Headers $headers -Body $body
```

### Get Resume Analysis

```powershell
# Replace {id} with the GUID returned from upload
curl http://localhost:5000/api/resumes/{id}
```

## Troubleshooting

### SQL Connection Issues

**Error**: "Cannot open server 'sql-cvanalyzer-dev' requested by the login"

**Solution**: Your IP might have changed. Update firewall:
```powershell
$myIP = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content
az sql server firewall-rule update --server sql-cvanalyzer-dev --resource-group rg-cvanalyzer-dev --name "LocalDevelopment" --start-ip-address $myIP --end-ip-address $myIP
```

### Storage Connection Issues

**Error**: "No valid combination of account information found"

**Solution**: Verify the connection string in `appsettings.Local.json` is complete and not truncated.

### Document Intelligence Issues

**Error**: "Access denied" or "401 Unauthorized"

**Solution**: Verify the API key is correct:
```powershell
az cognitiveservices account keys list --name cvanalyzer-dev-docintel --resource-group rg-cvanalyzer-dev
```

## Security Note

⚠️ **IMPORTANT**: `appsettings.Local.json` contains secrets and is already added to `.gitignore`. Never commit this file to Git!

The file contains:
- Storage account access key
- Document Intelligence API key
- SQL database credentials

## Next Steps

Once the API is running locally:

1. **Test with Swagger**: Navigate to `https://localhost:5001/swagger`
2. **Run AgentService**: Follow similar setup for `CVAnalyzer.AgentService` project
3. **Connect Frontend**: Update Angular proxy config to point to `http://localhost:5000`

## Cost Optimization

When running locally with Azure resources:
- **SQL Database**: Charges per hour (consider pausing when not in use)
- **Storage**: Pay per GB stored and transactions
- **Document Intelligence**: Pay per document analyzed
- **Container Apps**: Can scale to zero (no cost when idle)

To pause SQL Database:
```powershell
az sql db pause --name cvanalyzer-db-dev --server sql-cvanalyzer-dev --resource-group rg-cvanalyzer-dev
```

To resume:
```powershell
az sql db resume --name cvanalyzer-db-dev --server sql-cvanalyzer-dev --resource-group rg-cvanalyzer-dev
```
