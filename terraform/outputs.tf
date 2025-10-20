output "app_service_url" {
  description = "URL of the deployed App Service"
  value       = module.app_service.app_service_url
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of SQL Server"
  value       = module.sql_database.sql_server_fqdn
}

output "key_vault_uri" {
  description = "URI of the Key Vault"
  value       = module.key_vault.key_vault_uri
}
