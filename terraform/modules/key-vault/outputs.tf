output "id" {
  description = "Key Vault resource ID"
  value       = azurerm_key_vault.main.id
}

output "name" {
  description = "Key Vault name"
  value       = azurerm_key_vault.main.name
}

output "uri" {
  description = "Key Vault URI"
  value       = azurerm_key_vault.main.vault_uri
}

# Note: secret_uris output removed - secrets are now managed in root main.tf
# This allows proper RBAC propagation delay before secret creation
