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

variable "admin_username" {
  description = "SQL Server admin username"
  type        = string
  sensitive   = true
}

variable "admin_password" {
  description = "SQL Server admin password"
  type        = string
  sensitive   = true
}

variable "key_vault_id" {
  description = "ID of the Key Vault to store connection string"
  type        = string
}
