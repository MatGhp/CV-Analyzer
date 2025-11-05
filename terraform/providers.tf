terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Remote state backend in Azure Storage
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "tfstatecvanalyzer"
    container_name       = "tfstate"
    key                  = "cvanalyzer-dev.tfstate"
  }
}

provider "azurerm" {
  subscription_id = "9bf7d398-40c9-420e-8331-563f3e0dc68f"

  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}
