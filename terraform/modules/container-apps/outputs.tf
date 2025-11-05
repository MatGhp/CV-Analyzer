# Container Apps Outputs

output "environment_id" {
  description = "Container Apps Environment ID"
  value       = azurerm_container_app_environment.env.id
}

output "environment_name" {
  description = "Container Apps Environment name"
  value       = azurerm_container_app_environment.env.name
}

output "frontend_url" {
  description = "Frontend application URL"
  value       = "https://${azurerm_container_app.frontend.ingress[0].fqdn}"
}

output "api_url" {
  description = "API application URL"
  value       = "https://${azurerm_container_app.api.ingress[0].fqdn}"
}

output "frontend_identity_principal_id" {
  description = "Frontend managed identity principal ID"
  value       = azurerm_container_app.frontend.identity[0].principal_id
}

output "api_identity_principal_id" {
  description = "API managed identity principal ID"
  value       = azurerm_container_app.api.identity[0].principal_id
}

output "ai_service_identity_principal_id" {
  description = "AI Service managed identity principal ID"
  value       = azurerm_container_app.ai_service.identity[0].principal_id
}
