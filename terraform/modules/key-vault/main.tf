terraform {
  required_version = ">= 1.9.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.15.0"
    }
  }
}

# Key Vault for centralized secret management
resource "azurerm_key_vault" "main" {
  name                       = var.name
  location                   = var.location
  resource_group_name        = var.resource_group_name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = var.sku_name
  soft_delete_retention_days = 7
  purge_protection_enabled   = var.environment == "prod" ? true : false

  # Use RBAC for access control (recommended over access policies)
  enable_rbac_authorization = true

  # Network access - restrictive for production
  public_network_access_enabled = var.environment == "prod" ? false : true

  dynamic "network_acls" {
    for_each = var.environment == "prod" ? [1] : []
    content {
      default_action = "Deny"
      bypass         = "AzureServices"
      ip_rules       = var.allowed_ip_ranges
    }
  }

  tags = var.tags
}

# Current Azure client configuration
data "azurerm_client_config" "current" {}

# Secret: SQL Connection String
resource "azurerm_key_vault_secret" "sql_connection_string" {
  name         = "DatabaseConnectionString"
  value        = var.sql_connection_string
  key_vault_id = azurerm_key_vault.main.id

  # Wait for RBAC role assignment to propagate (assigned in root main.tf)
  depends_on = [azurerm_key_vault.main]

  tags = var.tags
}

# Secret: Application Insights Connection String
resource "azurerm_key_vault_secret" "app_insights_connection_string" {
  name         = "ApplicationInsightsConnectionString"
  value        = var.app_insights_connection_string
  key_vault_id = azurerm_key_vault.main.id

  # Wait for RBAC role assignment to propagate (assigned in root main.tf)
  depends_on = [azurerm_key_vault.main]

  tags = var.tags
}

# Secret: Application Insights Instrumentation Key
resource "azurerm_key_vault_secret" "app_insights_instrumentation_key" {
  name         = "ApplicationInsightsInstrumentationKey"
  value        = var.app_insights_instrumentation_key
  key_vault_id = azurerm_key_vault.main.id

  # Wait for RBAC role assignment to propagate (assigned in root main.tf)
  depends_on = [azurerm_key_vault.main]

  tags = var.tags
}

# Note: Access control is managed via RBAC (role assignments in main.tf)
# Terraform Service Principal needs "Key Vault Administrator" or "Key Vault Secrets Officer" role
# Container Apps managed identities need "Key Vault Secrets User" role
# These are assigned in the root main.tf file
