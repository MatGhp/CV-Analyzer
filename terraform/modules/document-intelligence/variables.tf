variable "name_prefix" {
  description = "Prefix for Document Intelligence resource name"
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

variable "sku_name" {
  description = "SKU for Document Intelligence"
  type        = string
  default     = "S0"

  validation {
    condition     = contains(["F0", "S0"], var.sku_name)
    error_message = "SKU must be F0 (free) or S0 (standard)"
  }
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
