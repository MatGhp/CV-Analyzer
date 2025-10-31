# Terraform Infrastructure for CV Analyzer

This directory contains Terraform configuration for deploying CV Analyzer to Azure with support for multiple environments (dev, test, prod).

## Prerequisites

- Terraform >= 1.0
- Azure CLI configured with appropriate subscription
- Azure subscription (default: 9bf7d398-40c9-420e-8331-563f3e0dc68f)

## Project Structure

```
terraform/
├── main.tf              # Root module - creates resource group and calls other modules
├── variables.tf         # Input variables (environment, location, SQL credentials)
├── outputs.tf           # Output values (URLs, FQDNs)
├── providers.tf         # Azure provider configuration
├── environments/        # Environment-specific variable files
│   ├── dev.tfvars      # Development environment
│   ├── test.tfvars     # Test environment
│   └── prod.tfvars     # Production environment
└── modules/
    ├── app-service/    # Azure App Service Plan + Linux Web App
    ├── key-vault/      # Azure Key Vault for secrets
    └── sql-database/   # Azure SQL Server + Database
```

## Resource Naming Convention

Resources follow the pattern: `{resource-type}-cvanalyzer-{environment}`

Examples:
- `rg-cvanalyzer-dev` - Resource Group
- `kv-cvanalyzer-dev` - Key Vault
- `sql-cvanalyzer-dev` - SQL Server
- `app-cvanalyzer-dev` - App Service

## Quick Start

### 1. Set SQL Admin Password

Set the password via environment variable (never commit this!):

**PowerShell:**
```powershell
$env:TF_VAR_sql_admin_password = "YourSecurePassword123!"
```

**Bash:**
```bash
export TF_VAR_sql_admin_password="YourSecurePassword123!"
```

### 2. Initialize Terraform

```bash
cd terraform
terraform init
```

### 3. Deploy an Environment

**Development:**
```bash
terraform plan -var-file="environments/dev.tfvars"
terraform apply -var-file="environments/dev.tfvars"
```

**Test:**
```bash
terraform plan -var-file="environments/test.tfvars"
terraform apply -var-file="environments/test.tfvars"
```

**Production:**
```bash
terraform plan -var-file="environments/prod.tfvars"
terraform apply -var-file="environments/prod.tfvars"
```

## Deployed Resources

Each environment creates:

1. **Resource Group** - Container for all resources
2. **Key Vault** - Stores secrets (connection strings, API keys)
3. **SQL Server** - Azure SQL Server instance
4. **SQL Database** - Application database (S0 tier)
5. **SQL Firewall Rule** - Allows Azure services to connect
6. **App Service Plan** - Linux B1 hosting plan
7. **App Service** - .NET 9.0 web application
8. **Key Vault Secret** - SQL connection string
9. **Key Vault Access Policy** - Grants App Service read access to secrets

## Outputs

After deployment, Terraform displays:

```hcl
app_service_url = "https://app-cvanalyzer-dev.azurewebsites.net"
key_vault_uri   = "https://kv-cvanalyzer-dev.vault.azure.net/"
sql_server_fqdn = "sql-cvanalyzer-dev.database.windows.net"
```

## Environment Variables

The App Service is configured with:

- `ASPNETCORE_ENVIRONMENT` - Environment name (Dev, Test, Prod)
- `UseKeyVault` - Set to "true" to enable Key Vault integration
- `KeyVault__Uri` - URI of the Key Vault

## Security

- SQL credentials are marked as `sensitive` in Terraform
- Connection strings are stored in Key Vault
- App Service uses Managed Identity to access Key Vault
- Key Vault has soft-delete enabled (7 days retention)

## Clean Up

To destroy an environment:

```bash
terraform destroy -var-file="environments/dev.tfvars"
```

⚠️ **Warning:** This will permanently delete all resources in that environment!

## Best Practices

See [.github/terraform-instructions.md](../.github/terraform-instructions.md) for complete Terraform development guidelines.

## Troubleshooting

### Subscription Not Found
If you get subscription errors, ensure Azure CLI is configured:
```bash
az account show
az account set --subscription "9bf7d398-40c9-420e-8331-563f3e0dc68f"
```

### Key Vault Name Already Exists
Key Vault names are globally unique. If deployment fails due to name conflict, the Key Vault may be in soft-deleted state:
```bash
az keyvault list-deleted
az keyvault purge --name kv-cvanalyzer-dev
```

### Terraform State Lock
If state is locked, ensure no other Terraform operation is running. Force unlock if needed:
```bash
terraform force-unlock <lock-id>
```
