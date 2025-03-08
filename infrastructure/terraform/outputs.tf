output "resource_group_name" {
  description = "The name of the Azure Resource Group containing all resources"
  value       = azurerm_resource_group.resource_group.name
  sensitive   = false
}

output "resource_group_location" {
  description = "The Azure region where the primary resources are deployed"
  value       = azurerm_resource_group.resource_group.location
  sensitive   = false
}

output "api_app_service_name" {
  description = "The name of the primary API App Service"
  value       = module.api.app_service_name
  sensitive   = false
}

output "api_app_service_url" {
  description = "The URL of the primary API App Service"
  value       = module.api.app_service_url
  sensitive   = false
}

output "api_staging_slot_url" {
  description = "The URL of the staging deployment slot for the API App Service"
  value       = module.api.staging_slot_url
  sensitive   = false
}

output "secondary_api_app_service_name" {
  description = "The name of the secondary region API App Service (if deployed)"
  value       = var.environment == "prod" ? module.api_secondary[0].app_service_name : "Not deployed"
  sensitive   = false
}

output "secondary_api_app_service_url" {
  description = "The URL of the secondary region API App Service (if deployed)"
  value       = var.environment == "prod" ? module.api_secondary[0].app_service_url : "Not deployed"
  sensitive   = false
}

output "traffic_manager_fqdn" {
  description = "The fully qualified domain name of the Traffic Manager profile"
  value       = azurerm_traffic_manager_profile.traffic_manager_profile.fqdn
  sensitive   = false
}

output "sql_server_name" {
  description = "The name of the Azure SQL Server"
  value       = module.database.server_name
  sensitive   = false
}

output "sql_database_name" {
  description = "The name of the Azure SQL Database"
  value       = module.database.database_name
  sensitive   = false
}

output "sql_server_fqdn" {
  description = "The fully qualified domain name of the Azure SQL Server"
  value       = module.database.server_fqdn
  sensitive   = false
}

output "storage_account_name" {
  description = "The name of the Azure Storage Account"
  value       = module.storage.storage_account_name
  sensitive   = false
}

output "blob_storage_endpoint" {
  description = "The blob endpoint URL for the Azure Storage Account"
  value       = module.storage.blob_endpoint
  sensitive   = false
}

output "key_vault_id" {
  description = "The resource ID of the Azure Key Vault"
  value       = azurerm_key_vault.key_vault.id
  sensitive   = false
}

output "key_vault_uri" {
  description = "The URI of the Azure Key Vault"
  value       = azurerm_key_vault.key_vault.vault_uri
  sensitive   = false
}

output "app_insights_id" {
  description = "The resource ID of Application Insights"
  value       = module.monitoring.app_insights_id
  sensitive   = false
}