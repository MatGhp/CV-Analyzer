output "frontend_url" {
  description = "URL of the frontend Container App"
  value       = module.container_apps.frontend_url
}

output "api_url" {
  description = "URL of the API Container App"
  value       = module.container_apps.api_url
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of SQL Server"
  value       = module.sql_database.server_fqdn
}

output "acr_login_server" {
  description = "Login server URL for Azure Container Registry"
  value       = module.acr.login_server
}

output "ai_foundry_endpoint" {
  description = "Endpoint URL for Azure AI Foundry"
  value       = module.ai_foundry.endpoint
}

output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "frontend_name" {
  description = "Frontend Container App name"
  value       = module.container_apps.frontend_name
}

output "api_name" {
  description = "API Container App name"
  value       = module.container_apps.api_name
}
