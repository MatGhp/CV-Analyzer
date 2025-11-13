output "id" {
  description = "ID of the storage account"
  value       = azurerm_storage_account.main.id
}

output "name" {
  description = "Name of the storage account"
  value       = azurerm_storage_account.main.name
}

output "primary_blob_endpoint" {
  description = "Primary blob endpoint"
  value       = azurerm_storage_account.main.primary_blob_endpoint
}

output "primary_queue_endpoint" {
  description = "Primary queue endpoint"
  value       = azurerm_storage_account.main.primary_queue_endpoint
}

output "queue_names" {
  description = "Queue configuration"
  value       = local.queue_config
}

output "container_name" {
  description = "Blob container name"
  value       = azurerm_storage_container.resumes.name
}
