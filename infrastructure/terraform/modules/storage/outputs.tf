# Output for the Azure Storage Account name
output "storage_account_name" {
  description = "The name of the Azure Storage Account"
  value       = azurerm_storage_account.storage_account.name
}

# Output for the blob endpoint URL
output "blob_endpoint" {
  description = "The blob endpoint URL for the Azure Storage Account"
  value       = azurerm_storage_account.storage_account.primary_blob_endpoint
}

# Output for the photos container name
output "photos_container_name" {
  description = "The name of the container used for storing photos"
  value       = azurerm_storage_container.photos_container.name
}

# Output for the backups container name
output "backups_container_name" {
  description = "The name of the container used for storing backups"
  value       = azurerm_storage_container.backups_container.name
}

# Output for the storage account connection string
# Marked as sensitive to prevent exposure in logs
output "connection_string" {
  description = "The primary connection string for the storage account"
  value       = azurerm_storage_account.storage_account.primary_connection_string
  sensitive   = true
}

# Output for the storage account primary access key
# Marked as sensitive to prevent exposure in logs
output "primary_access_key" {
  description = "The primary access key for the storage account"
  value       = azurerm_storage_account.storage_account.primary_access_key
  sensitive   = true
}