output "id" {
  description = "The ID of the AI Foundry (Cognitive Services) resource"
  value       = azurerm_cognitive_account.ai_foundry.id
}

output "name" {
  description = "The name of the AI Foundry hub"
  value       = azurerm_cognitive_account.ai_foundry.name
}

output "endpoint" {
  description = "The endpoint URL for the AI Foundry service"
  value       = azurerm_cognitive_account.ai_foundry.endpoint
}

output "primary_access_key" {
  description = "Primary access key for AI Foundry (sensitive)"
  value       = azurerm_cognitive_account.ai_foundry.primary_access_key
  sensitive   = true
}

output "deployment_name" {
  description = "The name of the GPT-4o deployment"
  value       = azurerm_cognitive_deployment.gpt4o.name
}

output "identity_principal_id" {
  description = "The principal ID of the AI Foundry managed identity"
  value       = azurerm_cognitive_account.ai_foundry.identity[0].principal_id
}
