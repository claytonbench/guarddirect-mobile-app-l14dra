environment = "prod"

# Primary Azure region for production resource deployment
location = "eastus"

# Base name for the Azure Resource Group
resource_group_name = "security-patrol-rg"

# Standard SKU configuration for App Service Plan with two instances for production environment
app_service_sku = {
  tier     = "Standard"
  size     = "S1"
  capacity = 2
}

# Standard tier SKU for Azure SQL Database in production environment
sql_database_sku = "S1"

# Storage account tier with geo-redundant storage for production environment
storage_account_tier = {
  tier        = "Standard"
  replication = "GRS"
}

# Base name for Azure Key Vault
key_vault_name = "security-patrol-kv"

# Base name for Azure Traffic Manager profile
traffic_manager_name = "security-patrol-tm"

# DNS name for Azure Traffic Manager in production environment
traffic_manager_dns_name = "security-patrol-api"

# Resource tags for the production environment
tags = {
  Project     = "SecurityPatrol"
  Environment = "Production"
  ManagedBy   = "Terraform"
  CostCenter  = "IT-Prod"
}

# Email addresses for security alerts in production environment
security_alert_emails = ["security@example.com", "operations@example.com", "devops@example.com"]

# Enable geo-replication for production environment to ensure high availability
enable_geo_replication = true

# Enable advanced monitoring features for production environment
enable_advanced_monitoring = true

# Maximum backup retention period for production environment
backup_retention_days = 35

# Minimum number of instances for auto-scaling in production
auto_scale_min_capacity = 2

# Maximum number of instances for auto-scaling in production environment
auto_scale_max_capacity = 10

# Number of days to retain data in Application Insights for production
app_insights_retention_days = 90

# Number of days to retain data in Log Analytics workspace for production
log_analytics_retention_days = 90

# Strict threshold in milliseconds for API response time alerts in production
api_response_time_threshold_ms = 500

# Strict threshold percentage for API failure rate alerts in production
api_failure_rate_threshold = 1

# Email address for receiving monitoring alerts in production
alert_email_address = "prod-alerts@example.com"

# Secondary Azure region for geo-replication and disaster recovery in production
secondary_location = "westus"

# Enable DDoS protection for production environment
enable_ddos_protection = true

# Enable Web Application Firewall for production environment
enable_waf = true

# Enable private endpoints for production environment to enhance security
enable_private_endpoints = true

# Enable IP restrictions for App Service in production environment
enable_ip_restrictions = true

# List of allowed IP address ranges for App Service access in production
allowed_ip_addresses = ["10.0.0.0/8", "172.16.0.0/12"]

# CPU percentage threshold to trigger scale out in production
autoscale_cpu_threshold_high = 70

# CPU percentage threshold to trigger scale in in production
autoscale_cpu_threshold_low = 30

# Threshold percentage for availability alerts in production
availability_threshold = 99.9

# Threshold count for exception alerts in production
exception_threshold = 2

# Severity level for critical alerts in production (0 is most severe)
alert_severity_critical = 0

# Severity level for warning alerts in production (1 is second most severe)
alert_severity_warning = 1

# Geographic locations for web test monitoring in production with global coverage
web_test_geo_locations = [
  "us-ca-sjc-azr",   # US West (San Jose)
  "us-tx-sn1-azr",   # US South Central (San Antonio)
  "us-il-ch1-azr",   # US North Central (Chicago)
  "us-va-ash-azr",   # US East (Ashburn)
  "us-fl-mia-edge",  # US East (Miami)
  "emea-gb-db3-azr", # Europe (Dublin)
  "apac-sg-sin-azr"  # Asia Pacific (Singapore)
]