resource "azurerm_service_plan" "main" {
  name                = "asp-cvanalyzer-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = "B1"

  tags = {
    Environment = var.environment
    Application = "cvanalyzer"
  }
}

resource "azurerm_linux_web_app" "main" {
  name                = "app-cvanalyzer-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = azurerm_service_plan.main.id

  # SECURITY: Enforce HTTPS
  https_only                    = true
  client_certificate_enabled    = false
  public_network_access_enabled = true

  # SECURITY: Disable basic auth for FTP and WebDeploy
  ftp_publish_basic_authentication_enabled       = false
  webdeploy_publish_basic_authentication_enabled = false

  site_config {
    always_on           = false
    ftps_state          = "FtpsOnly" # SECURITY: Require FTPS
    http2_enabled       = true
    minimum_tls_version = "1.2" # SECURITY: Minimum TLS version

    # SECURITY: Add security headers
    app_command_line = ""

    application_stack {
      dotnet_version = "9.0"
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = title(var.environment) # Dev, Test, Prod
    "UseKeyVault"            = "true"
    "KeyVault__Uri"          = var.key_vault_uri

    # SECURITY: Additional security settings
    "ASPNETCORE_FORWARDEDHEADERS_ENABLED" = "true"
    "WEBSITE_HTTPLOGGING_RETENTION_DAYS"  = "7"
  }

  identity {
    type = "SystemAssigned"
  }

  tags = {
    Environment = var.environment
    Application = "cvanalyzer"
  }
}
