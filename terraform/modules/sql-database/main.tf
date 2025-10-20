resource "azurerm_mssql_server" "main" {
  name                         = "${var.app_name}-sql-${var.environment}"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.admin_username
  administrator_login_password = var.admin_password

  tags = {
    Environment = var.environment
    Application = var.app_name
  }
}

resource "azurerm_mssql_database" "main" {
  name      = "${var.app_name}-db-${var.environment}"
  server_id = azurerm_mssql_server.main.id
  sku_name  = "S0"

  tags = {
    Environment = var.environment
    Application = var.app_name
  }
}

resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_key_vault_secret" "sql_connection_string" {
  name         = "DatabaseConnectionString"
  value        = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.admin_username};Password=${var.admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = var.key_vault_id
}
