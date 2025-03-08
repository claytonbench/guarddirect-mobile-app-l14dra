# Main Terraform configuration for Security Patrol Application infrastructure

# Provider configuration
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

# Azure provider configuration
provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = false
      recover_soft_deleted_key_vaults = true
    }
    resource_group {
      prevent_deletion_if_contains_resources = true
    }
  }
}

# Get current Azure client configuration for accessing tenant and subscription info
data "azurerm_client_config" "current" {}

# Get information about the current Azure subscription
data "azurerm_subscription" "subscription" {}

# Local variables for resource naming and configuration
locals {
  resource_group_name = "${var.resource_group_name}-${var.environment}"
  key_vault_name = "${var.key_vault_name}-${var.environment}"
  traffic_manager_name = "${var.traffic_manager_name}-${var.environment}"
  secondary_location = var.location == "eastus" ? "westus" : "eastus"
}

# Resource Group to contain all resources
resource "azurerm_resource_group" "resource_group" {
  name     = local.resource_group_name
  location = var.location
  tags     = var.tags
}

# Key Vault for securely storing secrets, keys, and certificates
resource "azurerm_key_vault" "key_vault" {
  name                = local.key_vault_name
  resource_group_name = azurerm_resource_group.resource_group.name
  location            = azurerm_resource_group.resource_group.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"
  
  soft_delete_retention_days = 90
  purge_protection_enabled   = true
  enabled_for_disk_encryption = true
  enabled_for_deployment = true
  enabled_for_template_deployment = true
  
  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id
    
    key_permissions = [
      "Get", "List", "Create", "Delete", "Update", "Recover", "Backup", "Restore"
    ]
    
    secret_permissions = [
      "Get", "List", "Set", "Delete", "Recover", "Backup", "Restore"
    ]
    
    certificate_permissions = [
      "Get", "List", "Create", "Delete", "Update", "Recover", "Backup", "Restore"
    ]
  }
  
  network_acls {
    default_action = "Allow"
    bypass         = "AzureServices"
  }
  
  tags = var.tags
}

# Traffic Manager for routing traffic and providing high availability
resource "azurerm_traffic_manager_profile" "traffic_manager_profile" {
  name                = local.traffic_manager_name
  resource_group_name = azurerm_resource_group.resource_group.name
  traffic_routing_method = "Performance"
  
  dns_config {
    relative_name = var.traffic_manager_dns_name
    ttl           = 60
  }
  
  monitor_config {
    protocol      = "HTTPS"
    port          = 443
    path          = "/health"
    interval_in_seconds = 30
    timeout_in_seconds = 10
    tolerated_number_of_failures = 3
  }
  
  tags = var.tags
}

# Primary endpoint for Traffic Manager pointing to the primary region App Service
resource "azurerm_traffic_manager_endpoint" "primary_traffic_manager_endpoint" {
  name                = "primary"
  resource_group_name = azurerm_resource_group.resource_group.name
  profile_name        = azurerm_traffic_manager_profile.traffic_manager_profile.name
  type                = "azureEndpoints"
  target_resource_id  = module.api.app_service_id
  priority            = 1
  endpoint_status     = "Enabled"
  geo_mappings        = []
}

# Secondary endpoint for Traffic Manager pointing to the secondary region App Service
resource "azurerm_traffic_manager_endpoint" "secondary_traffic_manager_endpoint" {
  count               = var.environment == "prod" ? 1 : 0
  name                = "secondary"
  resource_group_name = azurerm_resource_group.resource_group.name
  profile_name        = azurerm_traffic_manager_profile.traffic_manager_profile.name
  type                = "azureEndpoints"
  target_resource_id  = module.api_secondary[0].app_service_id
  priority            = 2
  endpoint_status     = "Enabled"
  geo_mappings        = []
}

# Module for deploying the primary region API App Service
module "api" {
  source              = "./modules/api"
  resource_group_name = azurerm_resource_group.resource_group.name
  location            = var.location
  environment         = var.environment
  app_service_sku     = var.app_service_sku
  key_vault_id        = azurerm_key_vault.key_vault.id
  sql_connection_string = module.database.connection_string
  app_insights_key    = module.monitoring.app_insights_key
  tags                = var.tags
}

# Module for deploying the secondary region API App Service for disaster recovery
module "api_secondary" {
  count               = var.environment == "prod" ? 1 : 0
  source              = "./modules/api"
  resource_group_name = azurerm_resource_group.resource_group.name
  location            = local.secondary_location
  environment         = "${var.environment}-secondary"
  app_service_sku     = var.app_service_sku
  key_vault_id        = azurerm_key_vault.key_vault.id
  sql_connection_string = module.database.connection_string
  app_insights_key    = module.monitoring.app_insights_key
  tags                = var.tags
}

# Module for deploying the Azure SQL Database
module "database" {
  source              = "./modules/database"
  resource_group_name = azurerm_resource_group.resource_group.name
  location            = var.location
  environment         = var.environment
  sql_database_sku    = var.sql_database_sku
  key_vault_id        = azurerm_key_vault.key_vault.id
  secondary_location  = local.secondary_location
  tags                = var.tags
}

# Module for deploying the Azure Storage Account
module "storage" {
  source              = "./modules/storage"
  resource_group_name = azurerm_resource_group.resource_group.name
  location            = var.location
  environment         = var.environment
  storage_account_tier = var.storage_account_tier
  key_vault_id        = azurerm_key_vault.key_vault.id
  tags                = var.tags
}

# Module for deploying Application Insights and monitoring resources
module "monitoring" {
  source              = "./modules/monitoring"
  resource_group_name = azurerm_resource_group.resource_group.name
  location            = var.location
  environment         = var.environment
  app_service_id      = module.api.app_service_id
  tags                = var.tags
}