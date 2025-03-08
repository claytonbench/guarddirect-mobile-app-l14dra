environment = "staging"

# Primary Azure region for resource deployment
location = "eastus2"

# Resource group name (will be suffixed with environment)
resource_group_name = "security-patrol-rg"

# App Service Plan configuration - Standard tier with 2 instances for staging
app_service_sku = {
  tier     = "Standard"
  size     = "S1"
  capacity = 2
}

# SQL Database configuration - Standard tier for staging
sql_database_sku = "S1"

# Storage account configuration - Standard with locally redundant storage
storage_account_tier = {
  tier        = "Standard"
  replication = "LRS"
}

# Key Vault base name (will be suffixed with environment and unique string)
key_vault_name = "security-patrol-kv"

# Traffic Manager profile base name
traffic_manager_name = "security-patrol-tm"

# Traffic Manager DNS name with staging suffix
traffic_manager_dns_name = "security-patrol-api-staging"

# Resource tags for the staging environment
tags = {
  Project     = "SecurityPatrol"
  Environment = "Staging"
  ManagedBy   = "Terraform"
  CostCenter  = "IT-PreProd"
}

# Email addresses for security alerts in staging
security_alert_emails = ["devops@example.com", "qa@example.com"]

# Disable geo-replication for staging to reduce costs
enable_geo_replication = false

# Enable advanced monitoring for pre-production validation
enable_advanced_monitoring = true

# Moderate backup retention for staging
backup_retention_days = 14

# Auto-scaling configuration for staging environment
auto_scale_min_capacity = 2
auto_scale_max_capacity = 4

# Application Insights data retention period
app_insights_retention_days = 90

# Log Analytics workspace data retention period
log_analytics_retention_days = 60

# Threshold for API response time alerts in milliseconds
api_response_time_threshold_ms = 1000

# Threshold percentage for API failure rate alerts
api_failure_rate_threshold = 5

# Email address for monitoring alerts
alert_email_address = "staging-alerts@example.com"

# Secondary region for potential failover testing
secondary_location = "westus2"

# Disable DDoS protection for staging to reduce costs
enable_ddos_protection = false

# Enable Web Application Firewall for security testing
enable_waf = true

# Disable private endpoints for simplified testing access
enable_private_endpoints = false