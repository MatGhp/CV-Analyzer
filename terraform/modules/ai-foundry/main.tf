resource "azurerm_cognitive_account" "ai_foundry" {
  name                          = var.ai_hub_name
  location                      = var.location
  resource_group_name           = var.resource_group_name
  kind                          = "OpenAI"
  sku_name                      = var.sku_name
  custom_subdomain_name         = var.ai_hub_name
  public_network_access_enabled = true

  # Network ACLs: Allow all for MVP (managed identity token exchange requires accessible endpoint)
  # Note: Container Apps Consumption plan uses dynamic outbound IPs
  # For production, consider Private Endpoints with VNet integration
  network_acls {
    default_action = "Allow"
    bypass         = ["AzureServices"]
  }

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    create_before_destroy = true
  }

  tags = var.tags
}

resource "azurerm_cognitive_deployment" "gpt4o" {
  name                 = var.model_deployment_name
  cognitive_account_id = azurerm_cognitive_account.ai_foundry.id

  model {
    format  = "OpenAI"
    name    = var.model_name
    version = var.model_version
  }

  sku {
    name     = "Standard"
    capacity = var.model_capacity
  }
}
