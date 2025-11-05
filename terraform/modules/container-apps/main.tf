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

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = "frontend"
      image  = var.frontend_image
      cpu    = 0.25
      memory = "0.5Gi"
    }
  }

  ingress {
    external_enabled = true
    target_port      = 80
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

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = "api"
      image  = var.api_image
      cpu    = 0.5
      memory = "1Gi"

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
        value = "false"
      }

      env {
        name  = "AIService__BaseUrl"
        value = "https://${azurerm_container_app.ai_service.ingress[0].fqdn}"
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = var.tags

  depends_on = [azurerm_container_app.ai_service]
}

# AI Service Container App (Python FastAPI)
resource "azurerm_container_app" "ai_service" {
  name                         = "${var.app_name_prefix}-ai-service"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = "ai-service"
      image  = var.ai_service_image
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "AI_FOUNDRY_ENDPOINT"
        value = var.ai_foundry_endpoint
      }

      env {
        name  = "MODEL_DEPLOYMENT_NAME"
        value = var.model_deployment_name
      }

      env {
        name  = "LOG_LEVEL"
        value = "INFO"
      }
    }
  }

  ingress {
    external_enabled = false
    target_port      = 8000
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = var.tags
}
