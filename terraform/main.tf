# Common tags for all resources
locals {
  common_tags = {
    Environment = var.environment
    Application = "cvanalyzer"
  }

  # Health probes: Bootstrap Exception to Azure Best Practices
  # - Production: ALWAYS enabled (true) per Azure guidelines
  # - Dev/Test: Disabled (false) ONLY during initial Terraform deployment with placeholder images
  # - After CI/CD deploys real images, operators MUST set enable_health_probes=true in environment tfvars
  # - This two-stage approach solves the chicken-and-egg: Terraform creates containers with placeholder
  #   images that lack /health endpoints, causing ActivationFailed if probes are enabled
  enable_health_probes = var.enable_health_probes != null ? var.enable_health_probes : (var.environment == "prod" ? true : false)
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

# Current Azure client configuration (for Terraform service principal)
data "azurerm_client_config" "current" {}

# Key Vault Module (created without Container Apps access policies initially)
module "key_vault" {
  source              = "./modules/key-vault"
  name                = "kv-cvanalyzer-${var.environment}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  environment         = var.environment
  sku_name            = var.environment == "prod" ? "premium" : "standard"

  # Note: Secrets are created in root main.tf after RBAC propagation
  # Managed identity access policies added via RBAC after Container Apps created

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
  database_sku        = var.sql_database_sku
}

# Azure Container Registry Module
module "acr" {
  source              = "./modules/acr"
  name                = "acrcvanalyzer${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = var.acr_sku
  admin_enabled       = false # Use managed identity for Container Apps

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
  key_vault_uri                    = module.key_vault.uri
  sql_connection_string            = module.sql_database.connection_string # Fallback only
  ai_foundry_endpoint              = module.ai_foundry.endpoint
  model_deployment_name            = var.model_deployment_name
  storage_account_name             = module.storage.name
  storage_blob_endpoint            = module.storage.primary_blob_endpoint
  storage_queue_endpoint           = module.storage.primary_queue_endpoint
  queue_config                     = module.storage.queue_names
  document_intelligence_endpoint   = module.document_intelligence.endpoint
  app_insights_connection_string   = azurerm_application_insights.main.connection_string   # Fallback only
  app_insights_instrumentation_key = azurerm_application_insights.main.instrumentation_key # Fallback only
  log_analytics_workspace_id       = azurerm_log_analytics_workspace.main.id
  min_replicas                     = var.min_replicas
  max_replicas                     = var.max_replicas
  enable_health_probes             = local.enable_health_probes

  tags = local.common_tags
}

# ========================================
# Key Vault Secrets (created AFTER RBAC assignments propagate)
# ========================================

# Role assignment: Terraform Service Principal needs to manage Key Vault secrets
resource "azurerm_role_assignment" "terraform_keyvault" {
  scope                = module.key_vault.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

# Wait for RBAC permissions to propagate (Azure RBAC can take up to 2 minutes)
resource "time_sleep" "wait_for_rbac" {
  create_duration = "120s"

  depends_on = [azurerm_role_assignment.terraform_keyvault]
}

# Key Vault secrets - created after RBAC propagation
resource "azurerm_key_vault_secret" "sql_connection" {
  name         = "DatabaseConnectionString"
  value        = module.sql_database.connection_string
  key_vault_id = module.key_vault.id

  depends_on = [time_sleep.wait_for_rbac]
}

resource "azurerm_key_vault_secret" "app_insights_connection" {
  name         = "ApplicationInsightsConnectionString"
  value        = azurerm_application_insights.main.connection_string
  key_vault_id = module.key_vault.id

  depends_on = [time_sleep.wait_for_rbac]
}

resource "azurerm_key_vault_secret" "app_insights_instrumentation" {
  name         = "ApplicationInsightsInstrumentationKey"
  value        = azurerm_application_insights.main.instrumentation_key
  key_vault_id = module.key_vault.id

  depends_on = [time_sleep.wait_for_rbac]
}

# Role assignments for Container Apps
locals {
  frontend_role_assignments = {
    acr_pull  = { scope = module.acr.id, role = "AcrPull" }
    key_vault = { scope = module.key_vault.id, role = "Key Vault Secrets User" }
  }

  api_role_assignments = {
    acr_pull              = { scope = module.acr.id, role = "AcrPull" }
    key_vault             = { scope = module.key_vault.id, role = "Key Vault Secrets User" }
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
