resource "azurerm_service_plan" "main" {
  name                = "${var.app_name}-asp-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = "B1"

  tags = {
    Environment = var.environment
    Application = var.app_name
  }
}

resource "azurerm_linux_web_app" "main" {
  name                = "${var.app_name}-app-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = azurerm_service_plan.main.id

  site_config {
    always_on = false

    application_stack {
      dotnet_version = "9.0"
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = var.environment
    "UseKeyVault"            = "true"
    "KeyVault__Uri"          = var.key_vault_uri
  }

  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = var.connection_string
  }

  identity {
    type = "SystemAssigned"
  }

  tags = {
    Environment = var.environment
    Application = var.app_name
  }
}
