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
}
