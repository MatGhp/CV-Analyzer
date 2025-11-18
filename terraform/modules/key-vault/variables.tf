variable "name" {
  description = "Name of the Key Vault"
  type        = string
  validation {
    condition     = length(var.name) >= 3 && length(var.name) <= 24 && can(regex("^[a-zA-Z][a-zA-Z0-9-]*$", var.name))
    error_message = "Key Vault name must be 3-24 characters, start with letter, and contain only alphanumeric and hyphens"
  }
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "resource_group_name" {
  description = "Resource group name"
  type        = string
}

variable "environment" {
  description = "Environment (dev, test, prod)"
  type        = string
  validation {
    condition     = contains(["dev", "test", "prod"], var.environment)
    error_message = "Environment must be dev, test, or prod"
  }
}

variable "sku_name" {
  description = "SKU name (standard or premium)"
  type        = string
  default     = "standard"
  validation {
    condition     = contains(["standard", "premium"], var.sku_name)
    error_message = "SKU must be standard or premium"
  }
}

variable "sql_connection_string" {
  description = "SQL Server connection string to store as secret"
  type        = string
  sensitive   = true
}

variable "app_insights_connection_string" {
  description = "Application Insights connection string to store as secret"
  type        = string
  sensitive   = true
}

variable "app_insights_instrumentation_key" {
  description = "Application Insights instrumentation key to store as secret"
  type        = string
  sensitive   = true
}

variable "api_managed_identity_principal_id" {
  description = "Principal ID of API Container App managed identity"
  type        = string
  default     = null
}

variable "frontend_managed_identity_principal_id" {
  description = "Principal ID of Frontend Container App managed identity"
  type        = string
  default     = null
}

variable "allowed_ip_ranges" {
  description = "IP ranges allowed to access Key Vault (production only)"
  type        = list(string)
  default     = []
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
