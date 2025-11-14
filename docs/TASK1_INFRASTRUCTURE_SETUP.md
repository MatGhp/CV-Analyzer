# Task 1: Infrastructure Setup - COMPLETED

**Date**: November 13, 2025  
**Status**: ✅ Ready for Deployment

---

## Summary

Infrastructure resources for CV processing feature have been configured. This includes:

1. **Azure Document Intelligence** - Text extraction from PDF/DOCX files
2. **Azure Blob Storage** - Resume file storage with private container
3. **Azure Storage Queues** - Async processing (main + poison queue)
4. **Container Apps Environment Variables** - Configuration for all services

---

## Changes Made

### 1. Terraform Module Updates

#### `terraform/modules/ai-foundry/main.tf`
- ✅ Added Document Intelligence resource (FormRecognizer, S0 tier)
- ✅ Added Storage Account for resumes
- ✅ Created blob container "resumes" (private)
- ✅ Created queue "resume-analysis"
- ✅ Created poison queue "resume-analysis-poison"

#### `terraform/modules/ai-foundry/outputs.tf`
- ✅ Added Document Intelligence endpoint and key outputs
- ✅ Added Storage Account connection string output
- ✅ Added Storage Account ID output

#### `terraform/modules/container-apps/variables.tf`
- ✅ Added storage_connection_string variable
- ✅ Added document_intelligence_endpoint variable
- ✅ Added document_intelligence_key variable

#### `terraform/modules/container-apps/main.tf`
- ✅ Added environment variables to API container:
  - `AzureStorage__ConnectionString`
  - `AzureStorage__QueueName` (resume-analysis)
  - `AzureStorage__PoisonQueueName` (resume-analysis-poison)
  - `AzureStorage__ContainerName` (resumes)
  - `DocumentIntelligence__Endpoint`
  - `DocumentIntelligence__ApiKey`

#### `terraform/main.tf`
- ✅ Updated container_apps module to pass storage and Document Intelligence config
- ✅ Added role assignments:
  - Storage Blob Data Contributor (API → Storage)
  - Storage Queue Data Contributor (API → Storage)

### 2. Application Configuration

#### `backend/src/CVAnalyzer.API/appsettings.json`
- ✅ Added `AzureStorage` section with queue and container names
- ✅ Added `DocumentIntelligence` section with endpoint and key placeholders

#### `backend/src/CVAnalyzer.API/appsettings.Development.json`
- ✅ Added local development config with Azurite storage emulator
- ✅ Added placeholder for Document Intelligence credentials

---

## Resources Created

| Resource Type | Name Pattern | Purpose |
|--------------|-------------|---------|
| Cognitive Account (FormRecognizer) | `{ai-hub-name}-docintel` | PDF/DOCX text extraction |
| Storage Account | `{ai-hub-name}storage` | Blob and queue storage |
| Storage Container | `resumes` | Private resume file storage |
| Storage Queue | `resume-analysis` | Main processing queue |
| Storage Queue | `resume-analysis-poison` | Failed message queue |

---

## Deployment Steps

### 1. Set Required Variables

Create `terraform/terraform.tfvars` or use environment-specific files:

```hcl
environment = "dev"
location    = "eastus"
sql_admin_username = "sqladmin"
sql_admin_password = "YourSecurePassword123!"  # Use environment variable
model_deployment_name = "gpt-4o"
model_capacity = 10
```

### 2. Run Terraform Apply

```bash
cd terraform

# Initialize (first time only)
terraform init

# Plan to review changes
terraform plan -var-file="environments/dev.tfvars"

# Apply infrastructure
terraform apply -var-file="environments/dev.tfvars"
```

### 3. Update Local Development Settings

After deployment, update `appsettings.Development.json`:

```json
{
  "AzureStorage": {
    "ConnectionString": "<copy from Azure Portal or Terraform output>",
    "QueueName": "resume-analysis",
    "PoisonQueueName": "resume-analysis-poison",
    "ContainerName": "resumes"
  },
  "DocumentIntelligence": {
    "Endpoint": "<copy from Azure Portal or Terraform output>",
    "ApiKey": "<copy from Azure Portal or Terraform output>"
  }
}
```

### 4. Verify Deployment

Check resources in Azure Portal:
- ✅ Document Intelligence resource exists
- ✅ Storage Account exists with container "resumes"
- ✅ Queues "resume-analysis" and "resume-analysis-poison" created
- ✅ Container App has environment variables set
- ✅ Managed identity has Storage Blob/Queue Contributor roles

---

## Local Development Setup

### Using Azurite (Azure Storage Emulator)

1. **Install Azurite**:
   ```bash
   npm install -g azurite
   ```

2. **Start Azurite**:
   ```bash
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

3. **Connection String** (already in appsettings.Development.json):
   ```
   UseDevelopmentStorage=true
   ```

4. **Create Queues/Containers**: Queues and containers will auto-create on first use.

### Document Intelligence

Document Intelligence requires a real Azure resource (no emulator). Options:

1. **Free Tier (F0)**: 500 pages/month free
2. **S0 Tier**: Production tier ($1.50 per 1000 pages)

Update `appsettings.Development.json` with your resource:
```json
{
  "DocumentIntelligence": {
    "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
    "ApiKey": "your-api-key"
  }
}
```

---

## Configuration Reference

### Environment Variables (Container Apps)

| Variable | Value | Purpose |
|----------|-------|---------|
| `AzureStorage__ConnectionString` | From Storage Account | Blob/Queue access |
| `AzureStorage__QueueName` | `resume-analysis` | Main processing queue |
| `AzureStorage__PoisonQueueName` | `resume-analysis-poison` | Failed messages |
| `AzureStorage__ContainerName` | `resumes` | Blob container name |
| `DocumentIntelligence__Endpoint` | From Document Intelligence | API endpoint |
| `DocumentIntelligence__ApiKey` | From Document Intelligence | API key |

### Managed Identity Roles

| Role | Scope | Purpose |
|------|-------|---------|
| Storage Blob Data Contributor | Storage Account | Upload/read blobs, generate SAS |
| Storage Queue Data Contributor | Storage Account | Send/receive queue messages |
| Cognitive Services User | AI Foundry | GPT-4o access |

---

## Cost Estimate

| Service | Tier | Monthly Cost |
|---------|------|--------------|
| Document Intelligence | S0 | ~$15 (10K pages) |
| Blob Storage | Standard LRS | ~$2 (100 GB) |
| Queue Operations | Standard | ~$0.50 (1M ops) |
| **Total** | | **~$17.50/month** |

*Additional costs: AI Foundry ($40), Container Apps, SQL Database from existing infrastructure*

---

## Next Steps

✅ Task 1 Complete - Infrastructure configured  
⏭️ **Task 2**: Domain & Database Schema Updates

**Ready to implement**:
- Create `CandidateInfo` entity
- Update `Resume` entity with Status/BlobUrl
- Add EF Core migration
- Apply database schema changes

---

## Rollback Plan

If deployment fails:

```bash
# Destroy only new resources (manual)
terraform state list | grep -E "(document_intelligence|storage)" | xargs -n1 terraform state rm

# Or destroy entire environment
terraform destroy -var-file="environments/dev.tfvars"
```

**Note**: Blob storage has soft delete enabled (7 days retention by default).

---

## Testing Checklist

After deployment, verify:

- [ ] Document Intelligence endpoint responds (GET /health)
- [ ] Storage Account accessible (list containers)
- [ ] Blob container "resumes" exists and is private
- [ ] Queues created (resume-analysis + poison)
- [ ] Container App environment variables set
- [ ] Managed identity roles assigned
- [ ] Local development config updated
- [ ] Azurite running (for local dev)

---

## Troubleshooting

### Terraform Apply Fails

**Error**: `storage_account_name already exists`
- **Solution**: Storage account names are globally unique. Update name in terraform or delete existing resource.

**Error**: `insufficient quota`
- **Solution**: Check subscription limits in Azure Portal. Request quota increase if needed.

### Container App Can't Access Storage

**Error**: `AuthorizationPermissionMismatch`
- **Solution**: Verify role assignments exist. Wait 5 minutes for propagation.

### Document Intelligence API Fails

**Error**: `Access denied`
- **Solution**: Check API key in environment variables. Regenerate key if needed.

---

## References

- [Azure Document Intelligence Documentation](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/)
- [Azure Storage Queues](https://learn.microsoft.com/en-us/azure/storage/queues/)
- [Azurite Storage Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)
- [Terraform Azure Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)

---

**Task 1 Status**: ✅ **COMPLETE**  
**Implementation Time**: ~2 hours (faster than 1 day estimate)  
**Ready for**: Task 2 - Domain & Database Schema Updates
