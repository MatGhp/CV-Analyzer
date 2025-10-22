variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "app_name" {
  description = "Application name"
  type        = string
}

variable "connection_string" {
  description = "Database connection string"
  type        = string
  sensitive   = true
}

variable "key_vault_uri" {
  description = "Key Vault URI"
  type        = string
}
