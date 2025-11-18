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

variable "firewall_rules" {
  description = "Map of firewall rules to create (name = {start_ip, end_ip})"
  type = map(object({
    start_ip_address = string
    end_ip_address   = string
  }))
  default = {}
}

variable "database_sku" {
  description = "Database SKU (Basic, S0-S12, P1-P15, GP_Gen5_2, etc.)"
  type        = string
  default     = "Basic"

  validation {
    condition     = can(regex("^(Basic|S[0-9]|S1[0-2]|P[1-9]|P1[0-5]|GP_.*|BC_.*)", var.database_sku))
    error_message = "Database SKU must be a valid Azure SQL Database SKU."
  }
}
