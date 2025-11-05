# Azure Container Apps - Simple Module
# Creates Container Apps Environment + 3 Apps (Frontend, API, AI Service)

# Container Apps Environment
resource "azurerm_container_app_environment" "env" {
  name                = var.environment_name
  resource_group_name = var.resource_group_name
  location            = var.location

  tags = var.tags
}

# Frontend Container App (Angular + nginx)
resource "azurerm_container_app" "frontend" {
  name                         = "${var.app_name_prefix}-frontend"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  dynamic "registry" {
    for_each = var.acr_login_server != "" ? [1] : []
    content {
      server   = var.acr_login_server
      identity = "system"
    }
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = "frontend"
      image  = var.frontend_image
      cpu    = "0.5"
      memory = "1.0Gi"

      env {
        name  = "NGINX_PORT"
        value = "80"
      }
    }
  }

  ingress {
    target_port              = 80
    external_enabled         = true
    allow_insecure_connections = false
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = var.tags
}

# Backend API Container App (.NET)
resource "azurerm_container_app" "api" {
  name                         = "${var.app_name_prefix}-api"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  dynamic "registry" {
    for_each = var.acr_login_server != "" ? [1] : []
    content {
      server   = var.acr_login_server
      identity = "system"
    }
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = "api"
      image  = var.api_image
      cpu    = "1.0"
      memory = "2.0Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment
      }

      env {
        name  = "ConnectionStrings__DefaultConnection"
        value = var.sql_connection_string
      }

      env {
        name  = "UseKeyVault"
        value = "true"
      }

      env {
        name  = "Agent__Endpoint"
        value = var.ai_foundry_endpoint
      }

      env {
        name  = "Agent__Deployment"
        value = var.model_deployment_name
      }

      env {
        name  = "Agent__Temperature"
        value = "0.7"
      }

      env {
        name  = "Agent__TopP"
        value = "0.95"
      }
    }
  }

  ingress {
    target_port      = 8080
    external_enabled = true
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = var.tags
}
