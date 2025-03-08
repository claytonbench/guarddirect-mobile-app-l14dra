# Outputs for the monitoring module
# These values are exposed for use by other modules or the root module

# Application Insights outputs
output "app_insights_key" {
  description = "The instrumentation key for Application Insights"
  value       = azurerm_application_insights.application_insights.instrumentation_key
  sensitive   = true
}

output "app_insights_connection_string" {
  description = "The connection string for Application Insights"
  value       = azurerm_application_insights.application_insights.connection_string
  sensitive   = true
}

output "app_insights_id" {
  description = "The resource ID of Application Insights"
  value       = azurerm_application_insights.application_insights.id
}

# Log Analytics outputs
output "log_analytics_workspace_id" {
  description = "The resource ID of Log Analytics Workspace"
  value       = azurerm_log_analytics_workspace.log_analytics_workspace.id
}

# Alert Action Group outputs
output "action_group_id" {
  description = "The resource ID of the alert action group"
  value       = azurerm_monitor_action_group.email_alert_group.id
}

# Dashboard outputs
output "dashboard_url" {
  description = "The URL of the monitoring dashboard"
  value       = "https://portal.azure.com/#@/dashboard/arm${azurerm_dashboard.app_insights_dashboard.id}"
}