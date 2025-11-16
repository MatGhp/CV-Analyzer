resource "azurerm_mssql_server" "main" {
  name                         = "sql-cvanalyzer-${var.environment}"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.admin_username
  administrator_login_password = var.admin_password

  # SECURITY: Minimum TLS version
  minimum_tls_version = "1.2"

  # SECURITY: Disable public network access for production
  public_network_access_enabled = var.environment != "prod"

  tags = {
    Environment = var.environment
    Application = "cvanalyzer"
  }
}

resource "azurerm_mssql_database" "main" {
  name      = "cvanalyzer-db-${var.environment}"
  server_id = azurerm_mssql_server.main.id
  sku_name  = "Basic"

  # SECURITY: Enable threat detection for production
  threat_detection_policy {
    state                = var.environment == "prod" ? "Enabled" : "Disabled"
    retention_days       = var.environment == "prod" ? 30 : 7
    disabled_alerts      = []
    email_account_admins = var.environment == "prod" ? "Enabled" : "Disabled"
  }

  tags = {
    Environment = var.environment
    Application = "cvanalyzer"
  }
}

resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Dynamic firewall rules for specific IP addresses (dev/CI access)
resource "azurerm_mssql_firewall_rule" "custom" {
  for_each = var.firewall_rules

  name             = each.key
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = each.value.start_ip_address
  end_ip_address   = each.value.end_ip_address
}
