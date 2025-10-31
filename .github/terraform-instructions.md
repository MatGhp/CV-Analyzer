# Terraform Best Practices for CV Analyzer

## Core Principles

**KISS (Keep It Simple, Stupid)** - Prioritize simplicity over complexity. If you can achieve the same result with fewer resources or simpler logic, do it.

**🔐 SECURITY FIRST** - Before modifying Terraform code, review `security-guardrails.md` for infrastructure security requirements and guardrails.

## Environment Management

### Multi-Environment Strategy
- Use **environment-specific `.tfvars` files** (dev.tfvars, test.tfvars, prod.tfvars)
- Do NOT use Terraform workspaces for environment separation
- Each environment should be isolated and independently deployable

### Deploying Environments
```bash
# Development
terraform plan -var-file="environments/dev.tfvars"
terraform apply -var-file="environments/dev.tfvars"

# Test
terraform plan -var-file="environments/test.tfvars"
terraform apply -var-file="environments/test.tfvars"

# Production
terraform plan -var-file="environments/prod.tfvars"
terraform apply -var-file="environments/prod.tfvars"
```

## Naming Conventions

### Resource Naming Pattern
Format: `{resource-type}-{app-name}-{environment}`

Examples:
- `rg-cvanalyzer-dev` (Resource Group)
- `kv-cvanalyzer-prod` (Key Vault - max 24 chars)
- `sql-cvanalyzer-test` (SQL Server)
- `app-cvanalyzer-dev` (App Service)

### Variable Names
- Use lowercase with underscores: `app_name`, `environment`, `sql_admin_password`
- Be descriptive but concise: `key_vault_sku` not `kv_s`

## Module Design

### Single Responsibility
Each module should manage ONE type of Azure resource or closely related resources:
- `app-service/` - App Service Plan + Web App only
- `key-vault/` - Key Vault + secrets only
- `sql-database/` - SQL Server + Database only

### Avoid Circular Dependencies
- **Never** pass outputs between modules that create cycles
- Use separate resources for cross-module access (e.g., `azurerm_key_vault_access_policy`)
- If module A needs info from module B, and B needs info from A, break the cycle with a third resource

### Module Structure
```
modules/{module-name}/
  ├── main.tf       # Resource definitions
  ├── variables.tf  # Input variables
  └── outputs.tf    # Output values
```

## State Management

### Remote Backend
Always use Azure Storage for Terraform state in team environments:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "tfstatecvanalyzer"
    container_name       = "tfstate"
    key                  = "dev.tfstate"  # Change per environment
  }
}
```

### State File Isolation
- `dev.tfstate` for development
- `test.tfstate` for test
- `prod.tfstate` for production

## Security Best Practices

### Secrets Management
- **Never** commit secrets to Git
- Use Azure Key Vault for all secrets
- Use `sensitive = true` for password variables
- Store connection strings in Key Vault, not Terraform outputs

### Variable Files
- Commit `terraform.tfvars.example` with dummy values
- Add `*.tfvars` to `.gitignore` (except `*.tfvars.example`)
- Team members create their own `terraform.tfvars` locally

## Code Organization

### File Structure
```
terraform/
  ├── main.tf              # Root module - resource group + module calls
  ├── variables.tf         # Input variables
  ├── outputs.tf           # Output values
  ├── providers.tf         # Provider configuration
  ├── versions.tf          # Terraform & provider versions
  ├── environments/
  │   ├── dev.tfvars
  │   ├── test.tfvars
  │   └── prod.tfvars
  └── modules/
      ├── app-service/
      ├── key-vault/
      └── sql-database/
```

### Keep main.tf Simple
- Only resource group and module calls
- No complex logic or conditionals
- Delegate complexity to modules

## Common Patterns

### Conditional Resources
Use `count` for optional resources:
```hcl
resource "azurerm_resource" "optional" {
  count = var.create_resource ? 1 : 0
  # ...
}
```

### Dynamic Blocks
Only use `dynamic` blocks when the number of nested blocks is truly variable. For static configurations, use explicit blocks.

### Data Sources
Use data sources to reference existing resources, not to create dependencies:
```hcl
data "azurerm_client_config" "current" {}
```

## Validation

### Pre-Deployment Checklist
1. Run `terraform fmt` to format code
2. Run `terraform validate` to check syntax
3. Run `terraform plan` and review changes carefully
4. For production, always review plan with team before applying

### Testing
- Test infrastructure changes in `dev` first
- Promote tested changes to `test`, then `prod`
- Never make changes directly in production

## Common Mistakes to Avoid

❌ **Don't** hardcode values - use variables
❌ **Don't** create circular dependencies between modules
❌ **Don't** commit `.tfstate` files or `*.tfvars` with real values
❌ **Don't** use complex conditionals - keep it simple
❌ **Don't** mix multiple resource types in one module without good reason
❌ **Don't** skip `terraform plan` before applying

✅ **Do** use descriptive variable names
✅ **Do** add comments for complex logic
✅ **Do** use remote state for team collaboration
✅ **Do** follow consistent naming conventions
✅ **Do** separate environments with .tfvars files
✅ **Do** keep modules focused and simple

## Quick Reference

### Initialize Terraform
```bash
cd terraform
terraform init
```

### Deploy Environment
```bash
terraform plan -var-file="environments/dev.tfvars" -out=tfplan
terraform apply tfplan
```

### Destroy Environment
```bash
terraform destroy -var-file="environments/dev.tfvars"
```

### Format Code
```bash
terraform fmt -recursive
```

### Validate Configuration
```bash
terraform validate
```

## Resources
- [Azure Naming Conventions](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming)
- [Terraform Best Practices](https://www.terraform-best-practices.com/)
- [Azure Provider Documentation](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
