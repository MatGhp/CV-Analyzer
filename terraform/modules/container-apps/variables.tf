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

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
