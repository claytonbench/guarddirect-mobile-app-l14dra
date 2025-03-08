terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

# Get current client configuration
data "azurerm_client_config" "current" {}

# Local variables for naming and configuration
locals {
  # Resource naming
  sql_server_name = "sqlserver-securitypatrol-${var.environment}"
  sql_server_secondary_name = "sqlserver-securitypatrol-${var.environment}-secondary"
  sql_database_name = "sqldb-securitypatrol-${var.environment}"
  
  # Key Vault secret names
  sql_admin_password_secret_name = "sql-admin-password-${var.environment}"
  sql_connection_string_secret_name = "sql-connection-string-${var.environment}"
  
  # Connection string format
  database_connection_string = "Server=tcp:${azurerm_mssql_server.sql_server.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.sql_database.name};Persist Security Info=False;User ID=sqladmin;Password=${random_password.sql_admin_password.result};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  
  # App Service outbound IPs - this would be populated with actual IPs in real implementation
  app_service_outbound_ips = ["0.0.0.0"]
}

# Generate random password for SQL admin
resource "random_password" "sql_admin_password" {
  length           = 16
  special          = true
  min_upper        = 1
  min_lower        = 1
  min_numeric      = 1
  min_special      = 1
}

# Primary SQL Server
resource "azurerm_mssql_server" "sql_server" {
  name                         = local.sql_server_name
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = "sqladmin"
  administrator_login_password = random_password.sql_admin_password.result
  minimum_tls_version          = "1.2"
  public_network_access_enabled = true
  
  azuread_administrator {
    login_username = "AzureAD Admin"
    object_id      = data.azurerm_client_config.current.object_id
    tenant_id      = data.azurerm_client_config.current.tenant_id
  }
  
  identity {
    type = "SystemAssigned"
  }
  
  tags = var.tags
}

# SQL Database
resource "azurerm_mssql_database" "sql_database" {
  name                        = local.sql_database_name
  server_id                   = azurerm_mssql_server.sql_server.id
  sku_name                    = var.sql_database_sku
  max_size_gb                 = 50
  auto_pause_delay_in_minutes = var.environment == "prod" ? -1 : 60  # Disable auto-pause in prod
  min_capacity                = var.environment == "prod" ? 1 : 0.5  # Higher min capacity in prod
  read_scale                  = var.environment == "prod" ? true : false  # Enable read scaling in prod
  zone_redundant              = var.environment == "prod" ? true : false  # Enable zone redundancy in prod
  geo_backup_enabled          = true  # Enable geo-backup for disaster recovery
  
  tags = var.tags
}

# Firewall rule to allow Azure services to access SQL
resource "azurerm_mssql_firewall_rule" "sql_firewall_rule_azure_services" {
  name      = "AllowAzureServices"
  server_id = azurerm_mssql_server.sql_server.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Firewall rule to allow App Service to access SQL
resource "azurerm_mssql_firewall_rule" "sql_firewall_rule_app_service" {
  name      = "AllowAppService"
  server_id = azurerm_mssql_server.sql_server.id
  start_ip_address = local.app_service_outbound_ips[0]
  end_ip_address   = local.app_service_outbound_ips[length(local.app_service_outbound_ips) - 1]
}

# Security alert policy (for production only)
resource "azurerm_mssql_server_security_alert_policy" "sql_security_alert_policy" {
  count               = var.environment == "prod" ? 1 : 0
  resource_group_name = var.resource_group_name
  server_name         = azurerm_mssql_server.sql_server.name
  state               = "Enabled"
  email_account_admins = true
  email_addresses     = var.security_alert_emails
  retention_days      = 30
  disabled_alerts     = []
}

# Vulnerability assessment (for production only)
resource "azurerm_mssql_server_vulnerability_assessment" "sql_vulnerability_assessment" {
  count                        = var.environment == "prod" ? 1 : 0
  server_security_alert_policy_id = azurerm_mssql_server_security_alert_policy.sql_security_alert_policy[0].id
  storage_container_path       = var.audit_storage_account_container_url
  storage_account_access_key   = var.audit_storage_account_access_key
  
  recurring_scans {
    enabled                    = true
    email_subscription_admins  = true
    emails                     = var.security_alert_emails
  }
}

# Auditing policy (for production only)
resource "azurerm_mssql_server_extended_auditing_policy" "sql_auditing_policy" {
  count                = var.environment == "prod" ? 1 : 0
  server_id            = azurerm_mssql_server.sql_server.id
  storage_endpoint     = var.audit_storage_account_endpoint
  storage_account_access_key = var.audit_storage_account_access_key
  retention_in_days    = 90
  log_monitoring_enabled = true
}

# Secondary SQL Server for geo-replication (production only)
resource "azurerm_mssql_server" "sql_server_secondary" {
  count                        = var.environment == "prod" ? 1 : 0
  name                         = local.sql_server_secondary_name
  resource_group_name          = var.resource_group_name
  location                     = var.secondary_location
  version                      = "12.0"
  administrator_login          = "sqladmin"
  administrator_login_password = random_password.sql_admin_password.result
  minimum_tls_version          = "1.2"
  public_network_access_enabled = true
  
  azuread_administrator {
    login_username = "AzureAD Admin"
    object_id      = data.azurerm_client_config.current.object_id
    tenant_id      = data.azurerm_client_config.current.tenant_id
  }
  
  identity {
    type = "SystemAssigned"
  }
  
  tags = var.tags
}

# Firewall rule for secondary SQL Server (production only)
resource "azurerm_mssql_firewall_rule" "sql_firewall_rule_azure_services_secondary" {
  count     = var.environment == "prod" ? 1 : 0
  name      = "AllowAzureServices"
  server_id = azurerm_mssql_server.sql_server_secondary[0].id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Geo-replicated database (production only)
resource "azurerm_mssql_database" "sql_database_geo_replica" {
  count               = var.environment == "prod" ? 1 : 0
  name                = local.sql_database_name
  server_id           = azurerm_mssql_server.sql_server_secondary[0].id
  create_mode         = "Secondary"
  creation_source_database_id = azurerm_mssql_database.sql_database.id
  read_scale          = true
  tags                = var.tags
}

# Store SQL admin password in Key Vault
resource "azurerm_key_vault_secret" "sql_admin_password_secret" {
  name         = local.sql_admin_password_secret_name
  key_vault_id = var.key_vault_id
  value        = random_password.sql_admin_password.result
  content_type = "text/plain"
  
  # Set an expiration date (1 year from creation)
  expiration_date = timeadd(timestamp(), "8760h")
}

# Store SQL connection string in Key Vault
resource "azurerm_key_vault_secret" "sql_connection_string_secret" {
  name         = local.sql_connection_string_secret_name
  key_vault_id = var.key_vault_id
  value        = local.database_connection_string
  content_type = "text/plain"
  
  # Set an expiration date (1 year from creation)
  expiration_date = timeadd(timestamp(), "8760h")
}