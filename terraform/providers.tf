terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
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
