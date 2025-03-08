# Development environment identifier
environment = "dev"

# Primary Azure region for development resource deployment
location = "eastus"

# Base name for the Azure Resource Group
resource_group_name = "security-patrol-rg"

# Minimal SKU configuration for App Service Plan with single instance for development environment
app_service_sku = {
  tier     = "Standard"
  size     = "S1"
  capacity = 1
}

# Basic tier SKU for Azure SQL Database in development environment
sql_database_sku = "S0"

# Storage account tier with locally redundant storage for development environment
storage_account_tier = {
  tier        = "Standard"
  replication = "LRS"
}

# Base name for Azure Key Vault
key_vault_name = "security-patrol-kv"

# Base name for Azure Traffic Manager profile
traffic_manager_name = "security-patrol-tm"

# DNS name for Azure Traffic Manager with development environment suffix
traffic_manager_dns_name = "security-patrol-api-dev"

# Resource tags for the development environment
tags = {
  Project     = "SecurityPatrol"
  Environment = "Development"
  ManagedBy   = "Terraform"
  CostCenter  = "IT-Dev"
}

# Email address for security alerts in development environment
security_alert_emails = "dev-team@example.com"

# Disable geo-replication for development environment to reduce costs
enable_geo_replication = false

# Disable advanced monitoring features for development environment to reduce costs
enable_advanced_monitoring = false

# Minimum backup retention period for development environment
backup_retention_days = 7

# Minimum number of instances for auto-scaling in development
auto_scale_min_capacity = 1

# Maximum number of instances for auto-scaling in development environment
auto_scale_max_capacity = 2

# Number of days to retain data in Application Insights for development
app_insights_retention_days = 30

# Number of days to retain data in Log Analytics workspace for development
log_analytics_retention_days = 30

# Relaxed threshold in milliseconds for API response time alerts in development
api_response_time_threshold_ms = 3000

# Relaxed threshold percentage for API failure rate alerts in development
api_failure_rate_threshold = 10

# Email address for receiving monitoring alerts in development
alert_email_address = "dev-alerts@example.com"