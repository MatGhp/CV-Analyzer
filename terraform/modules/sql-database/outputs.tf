output "sql_server_fqdn" {
  description = "Fully qualified domain name of SQL Server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "database_name" {
  description = "Name of the database"
  value       = azurerm_mssql_database.main.name
}

output "connection_string" {
  description = "SQL Server connection string"
  value       = azurerm_key_vault_secret.sql_connection_string.value
  sensitive   = true
}
