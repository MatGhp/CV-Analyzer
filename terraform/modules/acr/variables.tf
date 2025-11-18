# Azure Container Registry Variables

variable "name" {
  description = "Name of the Azure Container Registry"
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

variable "sku" {
  description = "SKU tier (Basic, Standard, Premium)"
  type        = string
  default     = "Basic"
}

variable "admin_enabled" {
  description = "Enable admin user for ACR (NOT RECOMMENDED - use managed identity instead)"
  type        = bool
  default     = false

  validation {
    condition     = var.admin_enabled == false
    error_message = "ACR admin credentials should not be enabled. Use managed identity for authentication."
  }
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
