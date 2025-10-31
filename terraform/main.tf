# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "rg-cvanalyzer-${var.environment}"
  location = var.location

  tags = {
    Environment = var.environment
    Application = "cvanalyzer"
  }
}

# Key Vault Module
module "key_vault" {
  source              = "./modules/key-vault"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  environment         = var.environment
}

# SQL Database Module
module "sql_database" {
  source              = "./modules/sql-database"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  environment         = var.environment
  admin_username      = var.sql_admin_username
  admin_password      = var.sql_admin_password
  key_vault_id        = module.key_vault.key_vault_id
}

# App Service Module
module "app_service" {
  source              = "./modules/app-service"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  environment         = var.environment
  key_vault_uri       = module.key_vault.key_vault_uri
}

# Grant App Service access to Key Vault
resource "azurerm_key_vault_access_policy" "app_service" {
  key_vault_id = module.key_vault.key_vault_id
  tenant_id    = module.app_service.tenant_id
  object_id    = module.app_service.principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}
