# Retrieve current client configuration
data "azurerm_client_config" "current" {}

# Local variables for resource naming and configuration
locals {
  app_service_plan_name = "plan-security-patrol-${var.environment}"
  app_service_name = "app-security-patrol-${var.environment}"
  autoscale_setting_name = "as-security-patrol-${var.environment}"
  storage_connection_string = "DefaultEndpointsProtocol=https;AccountName=secpatrol${var.environment};AccountKey=${random_password.storage_key.result};EndpointSuffix=core.windows.net"
}

# Random resources for secure secrets
resource "random_password" "token_secret" {
  length           = 32
  special          = true
  override_special = "!@#$%&*()-_=+[]{}<>:?"
}

resource "random_password" "sms_api_key" {
  length  = 24
  special = false
}

resource "random_password" "storage_key" {
  length  = 64
  special = false
}

# App Service Plan
resource "azurerm_app_service_plan" "app_service_plan" {
  name                = local.app_service_plan_name
  resource_group_name = var.resource_group_name
  location            = var.location
  kind                = "Linux"
  reserved            = true

  sku {
    tier     = var.app_service_sku.tier
    size     = var.app_service_sku.size
    capacity = var.app_service_sku.capacity
  }

  tags = var.tags
}

# Primary App Service
resource "azurerm_app_service" "primary_app_service" {
  name                = local.app_service_name
  resource_group_name = var.resource_group_name
  location            = var.location
  app_service_plan_id = azurerm_app_service_plan.app_service_plan.id
  https_only          = true
  client_affinity_enabled = false

  site_config {
    always_on        = true
    linux_fx_version = "DOTNETCORE|8.0"
    min_tls_version  = "1.2"
    http2_enabled    = true
    
    cors {
      allowed_origins = ["*"]
      support_credentials = true
    }

    health_check_path = "/health"

    ip_restriction {
      ip_address = "0.0.0.0/0"
      name       = "Allow All"
      priority   = 100
      action     = "Allow"
    }
  }

  app_settings = {
    APPINSIGHTS_INSTRUMENTATIONKEY = var.app_insights_key
    ASPNETCORE_ENVIRONMENT = var.environment == "prod" ? "Production" : var.environment == "staging" ? "Staging" : "Development"
    WEBSITE_RUN_FROM_PACKAGE = "1"
    WEBSITE_TIME_ZONE = "UTC"
    
    # Key Vault references
    ConnectionStrings__DefaultConnection = "@Microsoft.KeyVault(SecretUri=${var.sql_connection_string})"
    Authentication__TokenSecret = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.token_secret.id})"
    Authentication__TokenExpiryMinutes = "480"
    Storage__ConnectionString = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.storage_connection_string.id})"
    Sms__ApiKey = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.sms_api_key.id})"
  }

  identity {
    type = "SystemAssigned"
  }

  tags = var.tags
}

# Staging deployment slot for blue-green deployments
resource "azurerm_app_service_slot" "staging_slot" {
  name                = "staging"
  app_service_name    = azurerm_app_service.primary_app_service.name
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_app_service_plan.app_service_plan.id
  https_only          = true
  client_affinity_enabled = false

  site_config {
    always_on        = true
    linux_fx_version = "DOTNETCORE|8.0"
    min_tls_version  = "1.2"
    http2_enabled    = true
    
    cors {
      allowed_origins = ["*"]
      support_credentials = true
    }

    health_check_path = "/health"

    ip_restriction {
      ip_address = "0.0.0.0/0"
      name       = "Allow All"
      priority   = 100
      action     = "Allow"
    }
  }

  app_settings = {
    APPINSIGHTS_INSTRUMENTATIONKEY = var.app_insights_key
    ASPNETCORE_ENVIRONMENT = "Staging"
    WEBSITE_RUN_FROM_PACKAGE = "1"
    WEBSITE_TIME_ZONE = "UTC"
    
    # Key Vault references
    ConnectionStrings__DefaultConnection = "@Microsoft.KeyVault(SecretUri=${var.sql_connection_string})"
    Authentication__TokenSecret = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.token_secret.id})"
    Authentication__TokenExpiryMinutes = "480"
    Storage__ConnectionString = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.storage_connection_string.id})"
    Sms__ApiKey = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.sms_api_key.id})"
  }

  identity {
    type = "SystemAssigned"
  }

  tags = var.tags
}

# Autoscale settings
resource "azurerm_monitor_autoscale_setting" "autoscale_setting" {
  name                = local.autoscale_setting_name
  resource_group_name = var.resource_group_name
  location            = var.location
  target_resource_id  = azurerm_app_service_plan.app_service_plan.id

  profile {
    name = "Default"

    capacity {
      default = var.app_service_sku.capacity
      minimum = var.app_service_sku.capacity
      maximum = 10
    }

    rule {
      metric_trigger {
        metric_name        = "CpuPercentage"
        metric_resource_id = azurerm_app_service_plan.app_service_plan.id
        time_grain         = "PT1M"
        statistic          = "Average"
        time_window        = "PT5M"
        time_aggregation   = "Average"
        operator           = "GreaterThan"
        threshold          = 70
      }

      scale_action {
        direction = "Increase"
        type      = "ChangeCount"
        value     = 1
        cooldown  = "PT10M"
      }
    }

    rule {
      metric_trigger {
        metric_name        = "CpuPercentage"
        metric_resource_id = azurerm_app_service_plan.app_service_plan.id
        time_grain         = "PT1M"
        statistic          = "Average"
        time_window        = "PT5M"
        time_aggregation   = "Average"
        operator           = "LessThan"
        threshold          = 30
      }

      scale_action {
        direction = "Decrease"
        type      = "ChangeCount"
        value     = 1
        cooldown  = "PT10M"
      }
    }
  }

  notification {
    email {
      send_to_subscription_administrator = true
      custom_emails                      = ["devops@example.com"]
    }
  }

  tags = var.tags
}

# Key Vault secrets
resource "azurerm_key_vault_secret" "token_secret" {
  name         = "TokenSecret-${var.environment}"
  value        = random_password.token_secret.result
  key_vault_id = var.key_vault_id
  content_type = "text/plain"
  tags         = var.tags
}

resource "azurerm_key_vault_secret" "storage_connection_string" {
  name         = "StorageConnectionString-${var.environment}"
  value        = local.storage_connection_string
  key_vault_id = var.key_vault_id
  content_type = "text/plain"
  tags         = var.tags
}

resource "azurerm_key_vault_secret" "sms_api_key" {
  name         = "SmsApiKey-${var.environment}"
  value        = random_password.sms_api_key.result
  key_vault_id = var.key_vault_id
  content_type = "text/plain"
  tags         = var.tags
}

# Key Vault access policies for App Service managed identities
resource "azurerm_key_vault_access_policy" "app_service_key_vault_access_policy" {
  key_vault_id = var.key_vault_id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_app_service.primary_app_service.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List",
  ]
}

resource "azurerm_key_vault_access_policy" "staging_slot_key_vault_access_policy" {
  key_vault_id = var.key_vault_id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_app_service_slot.staging_slot.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List",
  ]
}

# Output values
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