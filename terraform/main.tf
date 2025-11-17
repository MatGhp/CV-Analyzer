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

# Log Analytics Workspace for Container Apps monitoring
resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-cvanalyzer-${var.environment}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = local.common_tags
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = "appi-cvanalyzer-${var.environment}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"

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
  firewall_rules      = var.sql_firewall_rules
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

# Storage Module (Blob + Queue)
module "storage" {
  source              = "./modules/storage"
  name_prefix         = "cvanalyzer"
  environment         = var.environment
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  retention_days      = 30
  enable_auto_delete  = var.environment != "prod"

  tags = local.common_tags
}

# Document Intelligence Module
module "document_intelligence" {
  source              = "./modules/document-intelligence"
  name_prefix         = "cvanalyzer-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku_name            = var.environment == "prod" ? "S0" : "F0"

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

  # ACR configuration
  acr_login_server = module.acr.login_server

  # Configuration
  sql_connection_string            = module.sql_database.connection_string
  ai_foundry_endpoint              = module.ai_foundry.endpoint
  model_deployment_name            = var.model_deployment_name
  storage_account_name             = module.storage.name
  storage_blob_endpoint            = module.storage.primary_blob_endpoint
  storage_queue_endpoint           = module.storage.primary_queue_endpoint
  queue_config                     = module.storage.queue_names
  document_intelligence_endpoint   = module.document_intelligence.endpoint
  app_insights_connection_string   = azurerm_application_insights.main.connection_string
  app_insights_instrumentation_key = azurerm_application_insights.main.instrumentation_key
  log_analytics_workspace_id       = azurerm_log_analytics_workspace.main.id
  min_replicas                     = var.min_replicas
  max_replicas                     = var.max_replicas

  tags = local.common_tags
}

# Role assignments for Container Apps
locals {
  frontend_role_assignments = {
    acr_pull = { scope = module.acr.id, role = "AcrPull" }
  }

  api_role_assignments = {
    acr_pull              = { scope = module.acr.id, role = "AcrPull" }
    cognitive_services    = { scope = module.ai_foundry.id, role = "Cognitive Services User" }
    storage_blob          = { scope = module.storage.id, role = "Storage Blob Data Contributor" }
    storage_queue         = { scope = module.storage.id, role = "Storage Queue Data Contributor" }
    document_intelligence = { scope = module.document_intelligence.id, role = "Cognitive Services User" }
  }
}

resource "azurerm_role_assignment" "frontend_roles" {
  for_each             = local.frontend_role_assignments
  scope                = each.value.scope
  role_definition_name = each.value.role
  principal_id         = module.container_apps.frontend_identity_principal_id
}

resource "azurerm_role_assignment" "api_roles" {
  for_each             = local.api_role_assignments
  scope                = each.value.scope
  role_definition_name = each.value.role
  principal_id         = module.container_apps.api_identity_principal_id
}
