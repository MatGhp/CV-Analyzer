resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location

  tags = {
    Environment = var.environment
    Application = var.app_name
  }
}

module "key_vault" {
  source              = "./modules/key-vault"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  environment         = var.environment
  app_name            = var.app_name
}

module "sql_database" {
  source              = "./modules/sql-database"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  environment         = var.environment
  app_name            = var.app_name
  admin_username      = var.sql_admin_username
  admin_password      = var.sql_admin_password
  key_vault_id        = module.key_vault.key_vault_id
}

module "app_service" {
  source               = "./modules/app-service"
  resource_group_name  = azurerm_resource_group.main.name
  location             = azurerm_resource_group.main.location
  environment          = var.environment
  app_name             = var.app_name
  connection_string    = module.sql_database.connection_string
  key_vault_uri        = module.key_vault.key_vault_uri
}
