# Container Apps Variables

variable "environment_name" {
  description = "Name of the Container Apps Environment"
  type        = string
}

variable "app_name_prefix" {
  description = "Prefix for container app names"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "environment" {
  description = "Environment name (Development, Production)"
  type        = string
  default     = "Production"
}

variable "key_vault_uri" {
  description = "Key Vault URI for secret references"
  type        = string
}

variable "min_replicas" {
  description = "Minimum number of replicas (0 for scale-to-zero)"
  type        = number
  default     = 0
}

variable "max_replicas" {
  description = "Maximum number of replicas"
  type        = number
  default     = 5
}

variable "frontend_image" {
  description = "Frontend container image (e.g., acr.azurecr.io/frontend:latest)"
  type        = string
  default     = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
}

variable "api_image" {
  description = "API container image (e.g., acr.azurecr.io/api:latest)"
  type        = string
  default     = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
}

variable "ai_foundry_endpoint" {
  description = "Azure AI Foundry endpoint URL"
  type        = string
}

variable "model_deployment_name" {
  description = "Azure AI Foundry model deployment name"
  type        = string
  default     = "gpt-4o"
}

variable "sql_connection_string" {
  description = "SQL Server connection string"
  type        = string
  sensitive   = true
}

variable "acr_login_server" {
  description = "Azure Container Registry login server"
  type        = string
  default     = ""
}

variable "storage_account_name" {
  description = "Storage account name for managed identity access"
  type        = string
}

variable "storage_blob_endpoint" {
  description = "Storage blob endpoint"
  type        = string
}

variable "storage_queue_endpoint" {
  description = "Storage queue endpoint"
  type        = string
}

variable "queue_config" {
  description = "Queue configuration from storage module"
  type = object({
    main_queue   = string
    poison_queue = string
    container    = string
  })
}

variable "document_intelligence_endpoint" {
  description = "Document Intelligence endpoint URL"
  type        = string
}

variable "app_insights_connection_string" {
  description = "Application Insights connection string"
  type        = string
  sensitive   = true
}

variable "app_insights_instrumentation_key" {
  description = "Application Insights instrumentation key"
  type        = string
  sensitive   = true
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics Workspace ID for Container Apps Environment"
  type        = string
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
