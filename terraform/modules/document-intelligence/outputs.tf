output "id" {
  description = "ID of Document Intelligence resource"
  value       = azurerm_cognitive_account.main.id
}

output "endpoint" {
  description = "Endpoint URL of Document Intelligence"
  value       = azurerm_cognitive_account.main.endpoint
}

output "primary_access_key" {
  description = "Primary access key for Document Intelligence"
  value       = azurerm_cognitive_account.main.primary_access_key
  sensitive   = true
}

output "identity_principal_id" {
  description = "Principal ID of managed identity"
  value       = azurerm_cognitive_account.main.identity[0].principal_id
}
