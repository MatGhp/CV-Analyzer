variable "location" {
  description = "Azure region for resources"
  type        = string
}

variable "environment" {
  description = "Environment name (dev, test, prod)"
  type        = string

  validation {
    condition     = contains(["dev", "test", "prod"], var.environment)
    error_message = "Environment must be dev, test, or prod"
  }
}

variable "sql_admin_username" {
  description = "SQL Server admin username"
  type        = string
  sensitive   = true
}

variable "sql_admin_password" {
  description = "SQL Server admin password"
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.sql_admin_password) >= 12
    error_message = "SQL admin password must be at least 12 characters long"
  }

  validation {
    condition     = can(regex("[A-Z]", var.sql_admin_password)) && can(regex("[a-z]", var.sql_admin_password)) && can(regex("[0-9]", var.sql_admin_password)) && can(regex("[^A-Za-z0-9]", var.sql_admin_password))
    error_message = "SQL admin password must contain uppercase, lowercase, numbers, and special characters"
  }
}