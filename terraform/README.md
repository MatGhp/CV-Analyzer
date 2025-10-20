# Terraform Infrastructure for CV Analyzer

This directory contains Terraform configuration files for deploying the CV Analyzer application to Azure.

## Prerequisites

- Terraform >= 1.0
- Azure CLI
- Azure subscription

## Structure

- `main.tf` - Main configuration file
- `variables.tf` - Variable definitions
- `outputs.tf` - Output definitions
- `providers.tf` - Provider configuration
- `modules/` - Reusable Terraform modules
  - `app-service/` - Azure App Service configuration
  - `sql-database/` - Azure SQL Database configuration
  - `key-vault/` - Azure Key Vault configuration

## Usage

1. Copy `terraform.tfvars.example` to `terraform.tfvars` and update with your values:
   ```bash
   cp terraform.tfvars.example terraform.tfvars
   ```

2. Initialize Terraform:
   ```bash
   terraform init
   ```

3. Plan the deployment:
   ```bash
   terraform plan
   ```

4. Apply the configuration:
   ```bash
   terraform apply
   ```

## Outputs

After successful deployment, you'll get:
- App Service URL
- SQL Server FQDN
- Key Vault URI

## Clean Up

To destroy all resources:
```bash
terraform destroy
```
