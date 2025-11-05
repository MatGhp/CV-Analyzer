output "server_fqdn" {
  description = "Fully qualified domain name of SQL Server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "server_name" {
  description = "Name of the SQL Server"
  value       = azurerm_mssql_server.main.name
}

output "database_name" {
  description = "Name of the database"
  value       = azurerm_mssql_database.main.name
}

output "server_id" {
  description = "ID of the SQL Server"
  value       = azurerm_mssql_server.main.id
}

output "connection_string" {
  description = "SQL Server connection string"
  value       = "Server=${azurerm_mssql_server.main.fully_qualified_domain_name};Database=${azurerm_mssql_database.main.name};User Id=${var.admin_username};Password=${var.admin_password};TrustServerCertificate=True"
  sensitive   = true
}
