# CV Analyzer - Deployment Guide

## Overview

This repository uses GitHub Actions for automated CI/CD with three main workflows:

1. **CI Build and Test** (`ci.yml`) - Builds and tests both frontend and backend
2. **Infrastructure Deployment** (`infra-deploy.yml`) - Manages Azure infrastructure via Terraform
3. **Application Deployment** (`app-deploy.yml`) - Builds Docker images and deploys to Azure Container Apps

## Prerequisites

### GitHub Secrets

The following secrets must be configured in your GitHub repository (Settings > Secrets and variables > Actions):

| Secret Name | Description | Example/Notes |
|------------|-------------|---------------|
| `AZURE_CREDENTIALS` | Full Azure service principal JSON | Complete JSON output from `az ad sp create-for-rbac --sdk-auth` |
| `AZURE_CLIENT_ID` | Service principal client ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_CLIENT_SECRET` | Service principal secret | `xxxxx~xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_TENANT_ID` | Azure tenant ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `SQL_ADMIN_PASSWORD` | SQL Server admin password | Must meet Azure complexity requirements (12+ chars, upper/lower/number/special) |

### GitHub Environments

Create the following environments in GitHub (Settings > Environments):
- `dev`
- `test`
- `prod`

For production, add protection rules:
- ✅ Required reviewers
- ✅ Wait timer (optional)
- ✅ Deployment branches: main only

## Deployment Workflows

### 1. CI Build and Test

**Trigger:** Push or PR to `main` branch, or manual dispatch

**What it does:**
- Sets up .NET 9 and Node.js 20
- Restores dependencies for both projects
- Builds backend (Release configuration)
- Runs backend unit tests
- **Publishes** backend to `./backend/publish`
- Builds frontend (production configuration)
- Runs frontend tests (headless Chrome)
- Uploads build artifacts

**Artifacts:**
- `backend-artifacts` → `backend/publish/`
- `frontend-artifacts` → `frontend/dist/cv-analyzer-frontend/browser/`

### 2. Infrastructure Deployment

**Trigger:** 
- Push to `main` with changes in `terraform/**`
- Manual dispatch (select environment)

**What it does:**
- Initializes Terraform with Azure backend
- Validates and formats Terraform code
- Creates execution plan
- Applies infrastructure changes (auto-approve on main)

**Resources managed:**
- Resource Group
- SQL Database + Server
- Azure Container Registry (per environment)
- Azure AI Foundry + GPT-4o deployment
- Container Apps Environment
- Container Apps (Frontend + API)
- Role assignments (ACR Pull, AI Foundry access)

**Concurrency:** Prevents parallel Terraform runs per environment

### 3. Application Deployment

**Trigger:**
- Automatically after successful CI build (main branch only)
- Manual dispatch (select environment)

**What it does:**
- Checks out source code
- Logs into Azure and ACR
- Builds Docker images for API and Frontend
- Pushes images to environment-specific ACR
- Updates Container Apps with new images
- Waits for apps to stabilize (30s)
- Verifies API health (`/api/health`)
- Verifies Frontend health (root URL)
- Generates deployment summary

**Environment-aware:**
- ACR: `acrcvanalyzer{environment}` (e.g., `acrcvanalyzerdev`)
- Resource Group: `rg-cvanalyzer-{environment}`
- Container Apps: `ca-cvanalyzer-api`, `ca-cvanalyzer-frontend`

**Concurrency:** Prevents parallel deployments per environment

**Health checks:**
- API: 5 retries with 10s intervals
- Frontend: 5 retries with 10s intervals
- Deployment fails if health checks don't pass

## Deployment Process

### Initial Setup (One-time)

1. **Create Service Principal:**
   ```bash
   az login
   subscriptionId=$(az account show --query id -o tsv)
   az ad sp create-for-rbac --name "cv-analyzer-github" \
     --role contributor \
     --scopes /subscriptions/$subscriptionId \
     --sdk-auth
   ```

2. **Add GitHub Secrets:**
   - Copy the entire JSON output to `AZURE_CREDENTIALS`
   - Extract individual values for other secrets
   - Add SQL admin password

3. **Create GitHub Environments:**
   - Go to Settings > Environments
   - Create `dev`, `test`, `prod`
   - Configure protection rules for `prod`

4. **Deploy Infrastructure:**
   ```bash
   # From local machine
   cd terraform
   terraform init -reconfigure
   terraform plan -var-file="environments/dev.tfvars" -out=tfplan
   terraform apply tfplan
   ```

   Or use GitHub Actions:
   - Go to Actions > Infrastructure Deployment
   - Click "Run workflow"
   - Select environment: `dev`

### Regular Deployments

**Option 1: Automatic (Recommended)**
1. Make changes to code
2. Commit and push to `main` branch
3. CI workflow runs automatically
4. On success, deployment to `dev` starts automatically
5. Monitor progress in Actions tab

**Option 2: Manual Deployment**
1. Go to Actions > Application Deployment
2. Click "Run workflow"
3. Select environment (dev/test/prod)
4. Click "Run workflow"
5. Workflow builds and deploys from current main branch

### Promoting to Test/Prod

1. Verify deployment in `dev` environment
2. Go to Actions > Application Deployment
3. Click "Run workflow"
4. Select environment: `test` or `prod`
5. If required, approve the deployment
6. Monitor deployment and health checks

## Monitoring Deployments

### Workflow Summary

Each deployment generates a summary with:
- Environment name
- Git commit SHA
- Deployed URLs (Frontend, API, Health)

### Health Check Results

The deployment workflow verifies:
- ✅ API is responding at `/api/health`
- ✅ Frontend is serving the application
- ❌ Deployment fails if health checks don't pass

### Accessing Deployed Apps

After deployment, access your apps at:

**Dev Environment:**
- Frontend: `https://ca-cvanalyzer-frontend.{random}.swedencentral.azurecontainerapps.io`
- API: `https://ca-cvanalyzer-api.{random}.swedencentral.azurecontainerapps.io`
- Health: `https://ca-cvanalyzer-api.{random}.swedencentral.azurecontainerapps.io/api/health`

URLs are displayed in the deployment summary.

## Rollback Procedure

If a deployment fails or causes issues:

1. **Quick Rollback:**
   - Find the last successful workflow run
   - Click "Re-run jobs"
   - This redeploys the previous commit

2. **Manual Rollback:**
   ```bash
   # Get previous image SHA
   az acr repository show-tags \
     --name acrcvanalyzerdev \
     --repository cvanalyzer-api \
     --orderby time_desc \
     --output table

   # Update Container App to previous image
   az containerapp update \
     --name ca-cvanalyzer-api \
     --resource-group rg-cvanalyzer-dev \
     --image acrcvanalyzerdev.azurecr.io/cvanalyzer-api:{previous-sha}
   ```

3. **Code Revert:**
   - Revert the problematic commit
   - Push to main
   - Automatic deployment will trigger

## Troubleshooting

### CI Build Fails

**Backend build errors:**
- Check .NET 9 SDK compatibility
- Verify NuGet package versions
- Review test failures

**Frontend build errors:**
- Check Node.js 20 compatibility
- Run `npm ci` locally to verify dependencies
- Review Angular build configuration

### Terraform Apply Fails

**State lock errors:**
```bash
terraform force-unlock {LOCK_ID}
```

**Resource conflicts:**
- Check Azure portal for existing resources
- Verify naming conventions (must be globally unique)
- Review Terraform plan output

**Authentication errors:**
- Verify service principal has Contributor role
- Check secret values are correct
- Ensure subscription ID matches

### Deployment Fails

**ACR login fails:**
- Verify ACR admin is enabled
- Check ACR exists for target environment
- Verify service principal has AcrPush role

**Container App update fails:**
- Check Container App exists
- Verify image was pushed to ACR
- Review Container App logs in Azure Portal

**Health checks fail:**
- Check Container App logs for startup errors
- Verify environment variables are correct
- Ensure SQL connection string is valid
- Check AI Foundry endpoint configuration

### Viewing Logs

**GitHub Actions:**
- Go to Actions tab
- Click on workflow run
- Expand each step to view logs

**Azure Container Apps:**
```bash
az containerapp logs show \
  --name ca-cvanalyzer-api \
  --resource-group rg-cvanalyzer-dev \
  --follow
```

**Azure Portal:**
- Navigate to Container App
- Click "Log stream" in left menu
- Or use "Diagnose and solve problems"

## Security Best Practices

1. **Never commit secrets** - Use GitHub Secrets only
2. **Rotate credentials** - Update service principal secrets periodically
3. **Use managed identities** - Container Apps use system-assigned identities
4. **Review access** - Regularly audit who can trigger deployments
5. **Protect production** - Always require approvals for prod deployments
6. **Monitor deployments** - Set up alerts for failed deployments

## Cost Optimization

**Dev Environment:**
- Min replicas: 0 (scale-to-zero enabled)
- ACR SKU: Basic
- SQL: Basic tier
- AI: 10K tokens/min capacity

**Production:**
- Min replicas: 1 (always available)
- ACR SKU: Standard or Premium
- SQL: Standard tier with geo-replication
- AI: Higher capacity based on load

## Support

For issues or questions:
1. Check workflow logs in GitHub Actions
2. Review this deployment guide
3. Check Azure resource logs in Portal
4. Open an issue in the repository
