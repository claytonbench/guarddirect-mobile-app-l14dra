# Output the primary App Service details
output "app_service_name" {
  value       = azurerm_app_service.primary_app_service.name
  description = "The name of the primary App Service instance"
}

output "app_service_url" {
  value       = "https://${azurerm_app_service.primary_app_service.default_site_hostname}"
  description = "The URL of the primary App Service instance"
}

output "app_service_id" {
  value       = azurerm_app_service.primary_app_service.id
  description = "The resource ID of the primary App Service instance"
}

# Output the staging slot details
output "staging_slot_name" {
  value       = azurerm_app_service_slot.staging_slot.name
  description = "The name of the staging deployment slot"
}

output "staging_slot_url" {
  value       = "https://${azurerm_app_service_slot.staging_slot.default_site_hostname}"
  description = "The URL of the staging deployment slot"
}

output "staging_slot_id" {
  value       = azurerm_app_service_slot.staging_slot.id
  description = "The resource ID of the staging deployment slot"
}