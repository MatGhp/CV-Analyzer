# Common tags for all resources
locals {
  common_tags = {
    Environment = var.environment
    Application = "cvanalyzer"
  }
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "rg-cvanalyzer-${var.environment}"
  location = var.location

  tags = local.common_tags
}

# SQL Database Module
module "sql_database" {
  source              = "./modules/sql-database"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  environment         = var.environment
  admin_username      = var.sql_admin_username
  admin_password      = var.sql_admin_password
}

# Azure Container Registry Module
module "acr" {
  source              = "./modules/acr"
  name                = "acrcvanalyzer${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = var.acr_sku
  admin_enabled       = true

  tags = local.common_tags
}

# Azure AI Foundry Module
module "ai_foundry" {
  source                = "./modules/ai-foundry"
  ai_hub_name           = "ai-cvanalyzer-${var.environment}"
  resource_group_name   = azurerm_resource_group.main.name
  location              = azurerm_resource_group.main.location
  model_deployment_name = var.model_deployment_name
  model_capacity        = var.model_capacity

  tags = local.common_tags
}

# Azure Container Apps Module
module "container_apps" {
  source              = "./modules/container-apps"
  environment_name    = "cae-cvanalyzer-${var.environment}"
  app_name_prefix     = "ca-cvanalyzer"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  environment         = var.environment

  # Docker images - using placeholder until we build and push actual images
  # frontend_image   = "${module.acr.login_server}/cvanalyzer-frontend:${var.image_tag}"
  # api_image        = "${module.acr.login_server}/cvanalyzer-api:${var.image_tag}"
  # ai_service_image = "${module.acr.login_server}/cvanalyzer-ai:${var.image_tag}"

  # Configuration
  sql_connection_string = module.sql_database.connection_string
  ai_foundry_endpoint   = module.ai_foundry.endpoint
  model_deployment_name = var.model_deployment_name
  min_replicas          = var.min_replicas
  max_replicas          = var.max_replicas

  tags = local.common_tags
}

# Grant Container Apps ACR pull access
resource "azurerm_role_assignment" "acr_pull_frontend" {
  scope                = module.acr.id
  role_definition_name = "AcrPull"
  principal_id         = module.container_apps.frontend_identity_principal_id
}

resource "azurerm_role_assignment" "acr_pull_api" {
  scope                = module.acr.id
  role_definition_name = "AcrPull"
  principal_id         = module.container_apps.api_identity_principal_id
}

resource "azurerm_role_assignment" "acr_pull_ai" {
  scope                = module.acr.id
  role_definition_name = "AcrPull"
  principal_id         = module.container_apps.ai_service_identity_principal_id
}

# Grant AI Service access to AI Foundry
resource "azurerm_role_assignment" "ai_service_foundry_access" {
  scope                = module.ai_foundry.id
  role_definition_name = "Cognitive Services User"
  principal_id         = module.container_apps.ai_service_identity_principal_id
}
