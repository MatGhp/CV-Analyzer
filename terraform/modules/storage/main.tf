# Storage Module for CV Analyzer
# Provides blob storage and queues for resume processing

locals {
  queue_config = {
    main_queue   = "resume-analysis"
    poison_queue = "resume-analysis-poison"
    container    = "resumes"
  }
}

# Random suffix for globally unique storage account name
resource "random_string" "storage_suffix" {
  length  = 8
  special = false
  upper   = false
  lower   = true
  numeric = true

  keepers = {
    name_prefix = var.name_prefix
    environment = var.environment
  }
}

# Locals for name generation with proper length management
locals {
  # Storage account names: 3-24 chars, lowercase alphanumeric only
  max_prefix_length = 24 - 8 # Reserve 8 chars for random suffix
  sanitized_prefix  = lower(replace("${var.name_prefix}${var.environment}", "/[^a-z0-9]/", ""))
  truncated_prefix  = substr(local.sanitized_prefix, 0, local.max_prefix_length)
  storage_name      = "${local.truncated_prefix}${random_string.storage_suffix.result}"
}

resource "azurerm_storage_account" "main" {
  name                     = local.storage_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = var.replication_type
  min_tls_version          = "TLS1_2"

  blob_properties {
    versioning_enabled = true

    delete_retention_policy {
      days = var.environment == "prod" ? 30 : 7
    }
  }

  lifecycle {
    prevent_destroy = false # Cannot use variables in lifecycle blocks - set to true manually for prod
  }

  tags = var.tags
}

# Blob container for resumes
resource "azurerm_storage_container" "resumes" {
  name                  = local.queue_config.container
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

# Queue for resume analysis
resource "azurerm_storage_queue" "resume_analysis" {
  name               = local.queue_config.main_queue
  storage_account_id = azurerm_storage_account.main.id
}

# Poison queue for failed messages
resource "azurerm_storage_queue" "resume_analysis_poison" {
  name               = local.queue_config.poison_queue
  storage_account_id = azurerm_storage_account.main.id
}

# Blob lifecycle management - auto-delete old resumes
resource "azurerm_storage_management_policy" "resume_retention" {
  storage_account_id = azurerm_storage_account.main.id

  rule {
    name    = "delete-old-resumes"
    enabled = var.enable_auto_delete

    filters {
      blob_types   = ["blockBlob"]
      prefix_match = ["${local.queue_config.container}/"]
    }

    actions {
      base_blob {
        delete_after_days_since_modification_greater_than = var.retention_days
      }
    }
  }
}
