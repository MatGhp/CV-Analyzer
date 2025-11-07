# Terraform Infrastructure Guide - CV Analyzer

**Last Updated:** November 7, 2025  
**Terraform Version:** >= 1.7.4

---

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Project Structure](#project-structure)
- [Module Architecture](#module-architecture)
- [Environment Management](#environment-management)
- [State Management](#state-management)
- [Deployed Resources](#deployed-resources)
- [Security Best Practices](#security-best-practices)
- [Workflow Integration](#workflow-integration)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

---

## Overview

This directory contains Terraform configuration for deploying CV Analyzer infrastructure to Azure with support for multiple environments (dev, test, prod).

### Core Principles

- **KISS (Keep It Simple, Stupid):** Prioritize simplicity. Achieve results with fewer resources and simpler logic.
- **Security First:** All infrastructure follows security guardrails (see `docs/SECURITY.md`).
- **Environment Isolation:** Each environment (dev/test/prod) is independently deployable and isolated.

### Prerequisites

- Terraform >= 1.7.4
- Azure CLI configured with appropriate subscription
- Azure service principal with Contributor role
- SQL admin password (never commit!)

---

## Quick Start

### 1. Set SQL Admin Password

Set a strong password via a local environment variable (avoid committing the exact variable name or command). Refer to `variables.tf` for the variable name. You may use the token `PASSWORD_PLACEHOLDER` in discussions/examples; the pre-commit hook allowlists it.

### 2. Initialize Terraform

```bash
cd terraform
terraform init
```

### 3. Deploy an Environment

**Development:**
```bash
terraform plan -var-file="environments/dev.tfvars" -out=tfplan
terraform apply tfplan
```

**Test:**
```bash
terraform plan -var-file="environments/test.tfvars" -out=tfplan
terraform apply tfplan
```

**Production:**
```bash
terraform plan -var-file="environments/prod.tfvars" -out=tfplan
terraform apply tfplan
```

### 4. View Outputs

```bash
terraform output
```

---

## Project Structure

```
terraform/
├── main.tf              # Root module - resource group + module calls
├── variables.tf         # Input variables (environment, location, SQL credentials)
├── outputs.tf           # Output values (URLs, FQDNs)
├── providers.tf         # Azure provider configuration
├── resource-locks.tf    # Production resource locks
├── versions.tf          # Terraform & provider version constraints
├── terraform.tfvars.example  # Example values (safe to commit)
├── environments/        # Environment-specific variable files
│   ├── dev.tfvars      # Development environment
│   ├── test.tfvars     # Test environment
│   └── prod.tfvars     # Production environment
└── modules/
    ├── acr/            # Azure Container Registry
    ├── ai-foundry/     # Azure AI Foundry + GPT-4o deployment
    ├── container-apps/ # Container Apps Environment + Apps
    └── sql-database/   # Azure SQL Server + Database
```

---

## Module Architecture

### Design Principles

**Single Responsibility:** Each module manages one type of Azure resource or closely related resources.

### Available Modules

#### 1. Azure Container Registry (`modules/acr/`)

Creates environment-specific container registry for Docker images.

**Resources:**
- Azure Container Registry (Basic SKU)
- Admin user enabled for deployments

**Outputs:**
- `acr_name` - Registry name
- `acr_login_server` - Login server URL

#### 2. Azure AI Foundry (`modules/ai-foundry/`)

Provisions AI Hub and GPT-4o model deployment.

**Resources:**
- AI Hub
- AI Project
- GPT-4o model deployment

**Outputs:**
- `ai_hub_id` - AI Hub resource ID
- `ai_project_id` - AI Project resource ID
- `model_deployment_name` - GPT-4o deployment name

#### 3. Container Apps (`modules/container-apps/`)

Creates Container Apps Environment with Frontend and API applications.

**Resources:**
- Container Apps Environment (Consumption plan)
- Frontend Container App (nginx + Angular)
- API Container App (.NET 9)
- System-assigned managed identities
- ACR pull role assignments

**Outputs:**
- `frontend_url` - Frontend HTTPS URL
- `api_url` - API HTTPS URL
- `api_identity_principal_id` - API managed identity ID

#### 4. SQL Database (`modules/sql-database/`)

Provisions SQL Server and application database.

**Resources:**
- SQL Server (admin auth)
- SQL Database (Standard S0 tier)
- Firewall rule (Azure services access)

**Outputs:**
- `sql_server_fqdn` - Fully qualified domain name
- `sql_database_name` - Database name
- `connection_string` - Full connection string (marked sensitive)

### Avoiding Circular Dependencies

**Problem:** Module A needs output from Module B, and Module B needs output from Module A.

**Solution:** Break cycles with independent resources or data sources.

**Example:** App Service needs Key Vault URI, Key Vault needs App Service identity for access policy.
- Create App Service first (outputs identity)
- Pass identity to Key Vault module or create separate access policy resource

---

## Environment Management

### Multi-Environment Strategy

Use environment-specific `.tfvars` files for configuration. **Do NOT use Terraform workspaces.**

### Environment Files

Located in `terraform/environments/`:

**dev.tfvars:**
```hcl
environment = "dev"
location    = "swedencentral"
app_name    = "cvanalyzer"
```

**test.tfvars:**
```hcl
environment = "test"
location    = "swedencentral"
app_name    = "cvanalyzer"
```

**prod.tfvars:**
```hcl
environment = "prod"
location    = "swedencentral"
app_name    = "cvanalyzer"
```

### Deploying Environments

```bash
# Always specify var-file and save plan
terraform plan -var-file="environments/{env}.tfvars" -out=tfplan
terraform apply tfplan
```

### Resource Naming Convention

Format: `{resource-type}-{app-name}-{environment}`

Examples:
- `rg-cvanalyzer-dev` - Resource Group
- `acrcvanalyzerdev` - Container Registry (no hyphens, max 50 chars)
- `sql-cvanalyzer-dev` - SQL Server
- `ca-cvanalyzer-api` - Container App API

**Key Vault naming:** Maximum 24 characters, alphanumeric and hyphens only.

---

## State Management

### Remote Backend

Terraform state is stored in Azure Storage for team collaboration.

**Backend configuration** (`providers.tf`):
```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "tfstatecvanalyzer"
    container_name       = "tfstate"
    key                  = "cvanalyzer-{environment}.tfstate"
  }
}
```

### State File Isolation

- `tfstate/cvanalyzer-dev.tfstate` - Development
- `tfstate/cvanalyzer-test.tfstate` - Test
- `tfstate/cvanalyzer-prod.tfstate` - Production

### State Security

⚠️ **Never commit `.tfstate` files** - they contain:
- Connection strings
- Passwords (even if marked sensitive)
- API keys
- Resource IDs

Ensure `.tfstate` and `.tfstate.*` are in `.gitignore`.

---

## Deployed Resources

Each environment creates the following resources:

### Core Infrastructure

1. **Resource Group** (`rg-cvanalyzer-{env}`)
   - Container for all environment resources
   - Tags: Application, Environment

2. **Container Registry** (`acrcvanalyzer{env}`)
   - Basic SKU
   - Admin user enabled
   - Docker image storage

3. **SQL Server** (`sql-cvanalyzer-{env}`)
   - Admin authentication
   - TLS 1.2 minimum
   - Public access (dev/test), private endpoint (prod)

4. **SQL Database** (`sqldb-cvanalyzer-{env}`)
   - Standard S0 tier
   - 10 GB max size
   - Automated backups

5. **AI Foundry Hub** (`aih-cvanalyzer-{env}`)
   - Machine learning workspace
   - GPT-4o deployment

### Application Layer

6. **Container Apps Environment** (`cae-cvanalyzer-{env}`)
   - Consumption-based plan
   - Log Analytics workspace integration

7. **Frontend Container App** (`ca-cvanalyzer-frontend`)
   - nginx + Angular 20
   - 0.5 CPU, 1GB RAM
   - Auto-scaling: 1-3 replicas
   - Port 80

8. **API Container App** (`ca-cvanalyzer-api`)
   - .NET 9 + Agent Framework
   - 1 CPU, 2GB RAM
   - Auto-scaling: 1-5 replicas
   - Port 8080
   - Environment variables:
     - `ConnectionStrings__DefaultConnection`
     - `AI_FOUNDRY_ENDPOINT`
     - `MODEL_DEPLOYMENT_NAME`

### Security & Identity

9. **Managed Identities** (System-assigned)
   - Frontend identity (for ACR pull)
   - API identity (for ACR pull + AI Foundry access)

10. **Role Assignments**
    - ACR Pull role for both apps
    - Azure AI Developer role for API
    - Cognitive Services User role for API

### Production-Only Resources

11. **Resource Locks** (`CanNotDelete`)
    - Resource group lock
    - SQL Server lock
    - Prevents accidental deletion

---

## Security Best Practices

### Secrets Management

**Never commit secrets:**
- ❌ `*.tfvars` files with real values
- ❌ `terraform.tfstate` files
- ❌ Hardcoded subscription IDs
- ❌ Passwords in Terraform code

**Always use:**
- ✅ `terraform.tfvars.example` with dummy values
- ✅ Environment variables: `TF_VAR_{variable_name}`
- ✅ `sensitive = true` for password variables
- ✅ Azure Key Vault for runtime secrets

### Variable Validation

**Example: Password validation**
```hcl
variable "sql_admin_password" {
  type      = string
  sensitive = true
  
  validation {
    condition     = length(var.sql_admin_password) >= 12
    error_message = "Password must be at least 12 characters"
  }
}
```

### Environment-Specific Security

**Development:**
- Relaxed firewall rules (Azure services access)
- Public SQL access enabled
- No resource locks

**Production:**
- Private endpoints for SQL
- Threat detection enabled
- Resource locks (`CanNotDelete`)
- Key Vault purge protection
- Network ACLs

### Managed Identity

**Always use managed identity** for Azure resource access:

```hcl
resource "azurerm_linux_web_app" "api" {
  identity {
    type = "SystemAssigned"
  }
}
```

No credentials needed in code - Azure handles authentication automatically.

---

## Workflow Integration

### GitHub Actions

Terraform is integrated with GitHub Actions for automated infrastructure deployment.

**Workflow:** `.github/workflows/infra-deploy.yml`

**Triggers:**
- Push to `main` with changes in `terraform/**`
- Manual dispatch (select environment)

**Steps:**
1. Checkout repository
2. Setup Terraform
3. Azure login (service principal)
4. Terraform init (with backend config)
5. Terraform validate
6. Terraform plan
7. Terraform apply (auto-approve on main)

**Environment variables:**
- `ARM_CLIENT_ID` - Service principal client ID
- `ARM_CLIENT_SECRET` - Service principal secret
- `ARM_SUBSCRIPTION_ID` - Azure subscription ID
- `ARM_TENANT_ID` - Azure tenant ID
- `TF_VAR_sql_admin_password` - SQL admin password

**Concurrency control:** Prevents parallel Terraform runs per environment.

### Manual Deployment

From local machine:

```bash
# Set Azure credentials
az login
az account set --subscription "{subscription-id}"

# Set sensitive variables (locally only; do not commit commands)
# Example: set a local environment variable for the SQL admin password

# Deploy
cd terraform
terraform init -reconfigure
terraform plan -var-file="environments/dev.tfvars" -out=tfplan
terraform apply tfplan
```

---

## Troubleshooting

### Common Issues

#### Subscription Not Found

```bash
az account show
az account set --subscription <YOUR_SUBSCRIPTION_ID>
```

#### Key Vault Name Conflict

Key Vault names are globally unique. If deployment fails:

```bash
# List soft-deleted vaults
az keyvault list-deleted

# Purge if in soft-deleted state
az keyvault purge --name kv-cvanalyzer-dev
```

#### Terraform State Lock

If state is locked:

```bash
# Wait for other operations to complete, or force unlock
terraform force-unlock <lock-id>
```

#### ACR Name Too Long

Container Registry names:
- Max 50 characters
- Alphanumeric only (no hyphens)
- Must be globally unique

Solution: Shorten app name or remove hyphens.

#### Circular Dependency Error

**Error:** "Cycle: module.A, module.B"

**Solution:**
- Remove direct dependencies between modules
- Use data sources instead of module outputs
- Create independent resources for cross-module access

### Validation Commands

```bash
# Format code
terraform fmt -recursive

# Validate syntax
terraform validate

# Show current state
terraform show

# List resources
terraform state list

# View specific resource
terraform state show azurerm_resource_group.main
```

### Debugging

**Enable detailed logging:**
```bash
export TF_LOG=DEBUG
terraform apply
```

**Review plan:**
```bash
terraform plan -var-file="environments/dev.tfvars" -out=tfplan
terraform show tfplan
```

---

## Best Practices

### Code Quality

**Do's:**
- ✅ Run `terraform fmt` before committing
- ✅ Run `terraform validate` before committing
- ✅ Use descriptive variable names
- ✅ Add comments for complex logic
- ✅ Use consistent naming conventions
- ✅ Keep modules focused and simple
- ✅ Separate environments with `.tfvars` files

**Don'ts:**
- ❌ Hardcode values - use variables
- ❌ Create circular dependencies
- ❌ Commit `.tfstate` or `*.tfvars` with real values
- ❌ Use complex conditionals - keep it simple
- ❌ Mix unrelated resources in one module
- ❌ Skip `terraform plan` before applying

### Testing

- Test infrastructure changes in `dev` first
- Promote tested changes to `test`, then `prod`
- Never make changes directly in production
- Always review plan output carefully

### Deployment Workflow

1. Make Terraform changes
2. Run `terraform fmt`
3. Run `terraform validate`
4. Commit and push to feature branch
5. Create PR and review plan output
6. Merge to main
7. GitHub Actions deploys to `dev` automatically
8. Manually promote to `test` and `prod`

### Clean Up

To destroy an environment:

```bash
terraform destroy -var-file="environments/dev.tfvars"
```

⚠️ **Warning:** Permanently deletes all resources in that environment!

For production, resource locks prevent accidental deletion:
```bash
# Remove lock first
az lock delete --name DoNotDelete --resource-group rg-cvanalyzer-prod

# Then destroy
terraform destroy -var-file="environments/prod.tfvars"
```

---

## Outputs

After deployment, Terraform displays:

```hcl
Outputs:

acr_login_server = "acrcvanalyzerdev.azurecr.io"
api_url = "https://ca-cvanalyzer-api.{random}.swedencentral.azurecontainerapps.io"
frontend_url = "https://ca-cvanalyzer-frontend.{random}.swedencentral.azurecontainerapps.io"
sql_server_fqdn = "sql-cvanalyzer-dev.database.windows.net"
```

### Accessing Resources

**Container Apps:**
```bash
# View app details
az containerapp show \
  --name ca-cvanalyzer-api \
  --resource-group rg-cvanalyzer-dev

# View logs
az containerapp logs show \
  --name ca-cvanalyzer-api \
  --resource-group rg-cvanalyzer-dev \
  --follow
```

**SQL Database:**
```bash
# Connect via Azure CLI
az sql db show-connection-string \
  --server sql-cvanalyzer-dev \
  --name sqldb-cvanalyzer-dev \
  --client sqlcmd
```

**Container Registry:**
```bash
# List images
az acr repository list \
  --name acrcvanalyzerdev \
  --output table

# View tags
az acr repository show-tags \
  --name acrcvanalyzerdev \
  --repository cvanalyzer-api \
  --output table
```

---

## Resources

### Documentation

- [Azure Naming Conventions](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming)
- [Terraform Best Practices](https://www.terraform-best-practices.com/)
- [Azure Provider Documentation](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [Terraform CLI Documentation](https://www.terraform.io/docs/cli/index.html)

### Related Guides

- **Security:** `docs/SECURITY.md` - Security best practices and guardrails
- **DevOps:** `docs/DEVOPS.md` - CI/CD pipelines and deployment workflows
- **Architecture:** `docs/ARCHITECTURE.md` - System architecture overview

---

**For questions or issues, refer to the troubleshooting section or consult the team.**
