data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "main" {
  name                = "kv-cvanalyzer-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  # SECURITY: Purge protection should be enabled for production
  purge_protection_enabled   = var.environment == "prod" ? true : false
  soft_delete_retention_days = var.environment == "prod" ? 90 : 7

  # SECURITY: Network restrictions for production
  network_acls {
    bypass         = "AzureServices"
    default_action = var.environment == "prod" ? "Deny" : "Allow"
  }

  # Admin access for the current user/service principal running Terraform
  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get",
      "List",
      "Set",
      "Delete",
      "Purge"
    ]
  }

  tags = {
    Environment = var.environment
    Application = "cvanalyzer"
  }
}
