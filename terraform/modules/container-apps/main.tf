# Azure Container Apps - Simple Module
# Creates Container Apps Environment + 2 Apps (Frontend, API)
#
# Health Probe Bootstrap Pattern:
# 1. Initial Terraform deployment uses placeholder images (mcr.microsoft.com/azuredocs/containerapps-helloworld)
#    that do NOT implement /health endpoints. Health probes must be disabled (enable_health_probes=false)
#    during this phase to avoid ActivationFailed errors.
# 2. After Terraform creates infrastructure, CI/CD (GitHub Actions) deploys real container images
#    that DO implement /health endpoints.
# 3. Operators should then enable health probes (enable_health_probes=true) in environment tfvars
#    and re-run Terraform to activate health monitoring per Azure best practices.
# 4. Production environments default to health probes enabled; dev/test default to disabled for bootstrap.

# Container Apps Environment
resource "azurerm_container_app_environment" "env" {
  name                       = var.environment_name
  resource_group_name        = var.resource_group_name
  location                   = var.location
  log_analytics_workspace_id = var.log_analytics_workspace_id

  tags = var.tags
}

# Frontend Container App (Angular + nginx)
resource "azurerm_container_app" "frontend" {
  name                         = "${var.app_name_prefix}-frontend"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  # Explicit dependency: Frontend needs API FQDN for nginx configuration
  depends_on = [azurerm_container_app.api]

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

  # Lifecycle: Ignore only image and revision changes after initial creation
  # CI/CD pipeline (app-deploy.yml) manages container image updates
  # Allows Terraform to update other template properties (env vars, resources, etc.)
  # Note: Changes to env vars (e.g., API_FQDN) will trigger new revisions by Terraform.
  # This is intentional - infrastructure changes (API recreation) should propagate to frontend.
  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
      template[0].revision_suffix
    ]
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name = "frontend"
      # Use Microsoft's hello-world as placeholder
      # Real image deployed by GitHub Actions app-deploy.yml
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = "0.5"
      memory = "1.0Gi"

      env {
        name  = "NGINX_PORT"
        value = "80"
      }

      env {
        name  = "API_FQDN"
        value = azurerm_container_app.api.ingress[0].fqdn
      }

      # Liveness probe - detects and restarts failed containers
      # Checks /health endpoint to ensure nginx is responding
      # Note: failure_threshold and success_threshold are not supported by Azure Container Apps Terraform provider
      # Azure uses default values: failure_threshold=3, success_threshold=1
      # Only enabled when enable_health_probes=true to avoid ActivationFailed with placeholder images
      dynamic "liveness_probe" {
        for_each = var.enable_health_probes ? [1] : []
        content {
          transport        = "HTTP"
          port             = 80
          path             = "/health"
          interval_seconds = 10
          timeout          = 3
        }
      }

      # Readiness probe - ensures only healthy containers receive traffic
      # Critical for preventing 502/503 errors during deployment
      # Note: failure_threshold and success_threshold are not supported by Azure Container Apps Terraform provider
      # Azure uses default values: failure_threshold=3, success_threshold=1
      # Only enabled when enable_health_probes=true to avoid ActivationFailed with placeholder images
      dynamic "readiness_probe" {
        for_each = var.enable_health_probes ? [1] : []
        content {
          transport        = "HTTP"
          port             = 80
          path             = "/health"
          interval_seconds = 5
          timeout          = 3
          initial_delay    = 3
        }
      }
    }
  }

  ingress {
    target_port                = 80
    external_enabled           = true
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

  # Lifecycle: Ignore only image and revision changes after initial creation
  # CI/CD pipeline (app-deploy.yml) manages container image updates
  # Allows Terraform to update other template properties (env vars, resources, etc.)
  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
      template[0].revision_suffix
    ]
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name = "api"
      # Placeholder image - real app deployed by GitHub Actions
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
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
        name  = "KeyVault__Uri"
        value = var.key_vault_uri
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

      env {
        name  = "AzureStorage__UseManagedIdentity"
        value = "true"
      }

      env {
        name  = "AzureStorage__AccountName"
        value = var.storage_account_name
      }

      env {
        name  = "AzureStorage__BlobEndpoint"
        value = var.storage_blob_endpoint
      }

      env {
        name  = "AzureStorage__QueueEndpoint"
        value = var.storage_queue_endpoint
      }

      env {
        name  = "AzureStorage__QueueName"
        value = var.queue_config.main_queue
      }

      env {
        name  = "AzureStorage__PoisonQueueName"
        value = var.queue_config.poison_queue
      }

      env {
        name  = "AzureStorage__ContainerName"
        value = var.queue_config.container
      }

      env {
        name  = "DocumentIntelligence__Endpoint"
        value = var.document_intelligence_endpoint
      }

      env {
        name  = "DocumentIntelligence__UseManagedIdentity"
        value = "true"
      }

      env {
        name  = "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"
        value = "false"
      }

      env {
        name  = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        value = var.app_insights_connection_string
      }

      env {
        name  = "ApplicationInsights__InstrumentationKey"
        value = var.app_insights_instrumentation_key
      }

      # Liveness probe - detects and restarts failed containers
      # Checks /health endpoint to ensure API is responding
      # Note: failure_threshold and success_threshold are not supported by Azure Container Apps Terraform provider
      # Azure uses default values: failure_threshold=3, success_threshold=1
      # Only enabled when enable_health_probes=true to avoid ActivationFailed with placeholder images
      dynamic "liveness_probe" {
        for_each = var.enable_health_probes ? [1] : []
        content {
          transport        = "HTTP"
          port             = 8080
          path             = "/health"
          interval_seconds = 10
          timeout          = 3
        }
      }

      # Readiness probe - ensures only healthy containers receive traffic
      # Critical for preventing 502/503 errors during deployment
      # Note: failure_threshold and success_threshold are not supported by Azure Container Apps Terraform provider
      # Azure uses default values: failure_threshold=3, success_threshold=1
      # Only enabled when enable_health_probes=true to avoid ActivationFailed with placeholder images
      dynamic "readiness_probe" {
        for_each = var.enable_health_probes ? [1] : []
        content {
          transport        = "HTTP"
          port             = 8080
          path             = "/health"
          interval_seconds = 5
          timeout          = 3
          initial_delay    = 3
        }
      }
    }
  }

  ingress {
    target_port                = 8080
    external_enabled           = true
    allow_insecure_connections = false # Enforce HTTPS
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = var.tags
}
