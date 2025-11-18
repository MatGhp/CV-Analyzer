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

output "secret_uris" {
  description = "Map of secret names to URIs (for Key Vault references)"
  value = {
    sql_connection_string            = azurerm_key_vault_secret.sql_connection_string.versionless_id
    app_insights_connection_string   = azurerm_key_vault_secret.app_insights_connection_string.versionless_id
    app_insights_instrumentation_key = azurerm_key_vault_secret.app_insights_instrumentation_key.versionless_id
  }
  sensitive = true
}
