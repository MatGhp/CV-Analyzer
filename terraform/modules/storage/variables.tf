variable "name_prefix" {
  description = "Prefix for storage account name (will be sanitized and truncated)"
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
  description = "Environment name (dev, test, prod)"
  type        = string
}

variable "replication_type" {
  description = "Storage account replication type"
  type        = string
  default     = "LRS"

  validation {
    condition     = contains(["LRS", "GRS", "RAGRS", "ZRS"], var.replication_type)
    error_message = "Replication type must be LRS, GRS, RAGRS, or ZRS"
  }
}

variable "retention_days" {
  description = "Number of days to retain resumes before auto-deletion"
  type        = number
  default     = 30

  validation {
    condition     = var.retention_days >= 1 && var.retention_days <= 365
    error_message = "Retention days must be between 1 and 365"
  }
}

variable "enable_auto_delete" {
  description = "Enable automatic deletion of old resumes"
  type        = bool
  default     = true
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
  default     = {}
}
