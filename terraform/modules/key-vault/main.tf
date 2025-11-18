terraform {
  required_version = ">= 1.9.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.15.0"
    }
    time = {
      source  = "hashicorp/time"
      version = "~> 0.12.1"
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

# Note: Secrets are created in root main.tf after RBAC propagation delay
# This avoids circular dependencies between Key Vault module and RBAC assignments
# Terraform Service Principal needs "Key Vault Administrator" or "Key Vault Secrets Officer" role
# Container Apps managed identities need "Key Vault Secrets User" role
# These are assigned in the root main.tf file
