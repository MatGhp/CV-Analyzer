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
    condition = (
      can(regex("[A-Z]", var.sql_admin_password)) &&
      can(regex("[a-z]", var.sql_admin_password)) &&
      can(regex("[0-9]", var.sql_admin_password)) &&
      can(regex("[^A-Za-z0-9]", var.sql_admin_password))
    )
    error_message = "Password requires: uppercase, lowercase, number, and special character"
  }
}

variable "acr_sku" {
  description = "SKU for Azure Container Registry (Basic, Standard, Premium)"
  type        = string
  default     = "Basic"

  validation {
    condition     = contains(["Basic", "Standard", "Premium"], var.acr_sku)
    error_message = "ACR SKU must be Basic, Standard, or Premium"
  }
}

variable "image_tag" {
  description = "Docker image tag to deploy"
  type        = string
  default     = "latest"
}

variable "subscription_id" {
  description = "Azure subscription ID (optional). Prefer Azure CLI context or environment variables."
  type        = string
  default     = null
}

variable "model_deployment_name" {
  description = "Name for the GPT-4o deployment"
  type        = string
  default     = "gpt-4o"
}

variable "model_capacity" {
  description = "Capacity units for the AI model (tokens per minute in thousands)"
  type        = number
  default     = 10

  validation {
    condition     = var.model_capacity >= 1 && var.model_capacity <= 1000
    error_message = "Model capacity must be between 1 and 1000"
  }
}

variable "min_replicas" {
  description = "Minimum number of replicas for Container Apps (0 enables scale-to-zero)"
  type        = number
  default     = 0

  validation {
    condition     = var.min_replicas >= 0 && var.min_replicas <= 30
    error_message = "Minimum replicas must be between 0 and 30"
  }
}

variable "max_replicas" {
  description = "Maximum number of replicas for Container Apps"
  type        = number
  default     = 5

  validation {
    condition     = var.max_replicas >= 1 && var.max_replicas <= 30
    error_message = "Maximum replicas must be between 1 and 30"
  }

  validation {
    condition     = var.max_replicas >= var.min_replicas
    error_message = "Maximum replicas must be greater than or equal to minimum replicas"
  }
}

variable "enable_health_probes" {
  description = "Enable health probes for Container Apps. IMPORTANT: Should be true for production per Azure best practices. Set to false only during initial bootstrap deployment to avoid ActivationFailed with placeholder images."
  type        = bool
  default     = null # Will use environment-specific default in locals
}

variable "sql_firewall_rules" {
  description = "Additional SQL Server firewall rules (name = {start_ip, end_ip})"
  type = map(object({
    start_ip_address = string
    end_ip_address   = string
  }))
  default = {}
}

variable "sql_database_sku" {
  description = "SQL Database SKU (Basic, S0-S12, P1-P15, etc.)"
  type        = string
  default     = "Basic"

  validation {
    condition     = can(regex("^(Basic|S[0-9]|S1[0-2]|P[1-9]|P1[0-5]|GP_.*|BC_.*)", var.sql_database_sku))
    error_message = "Database SKU must be a valid Azure SQL Database SKU."
  }
}
