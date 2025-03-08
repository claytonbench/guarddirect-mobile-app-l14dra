# Azure Storage Configuration for Security Patrol Application

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

# Get current Azure client configuration
data "azurerm_client_config" "current" {}

# Local variables for resource naming and configuration
locals {
  storage_account_name = "secpatrol${var.environment}${random_string.storage_account_name.result}"
}

# Generate a unique suffix for the storage account name to ensure global uniqueness
resource "random_string" "storage_account_name" {
  length  = 8
  special = false
  upper   = false
  number  = true
}

# Azure Storage Account for storing application data including photos
resource "azurerm_storage_account" "storage_account" {
  name                     = local.storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = var.storage_account_tier.tier
  account_replication_type = var.storage_account_tier.replication
  account_kind             = "StorageV2"
  enable_https_traffic_only       = true
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = true
  
  blob_properties {
    versioning_enabled = true
    
    delete_retention_policy {
      days = 30
    }
    
    container_delete_retention_policy {
      days = 30
    }
    
    cors_rule {
      allowed_headers    = ["*"]
      allowed_methods    = ["GET", "POST", "PUT"]
      allowed_origins    = ["https://*.securitypatrol.com"]
      exposed_headers    = ["*"]
      max_age_in_seconds = 3600
    }
  }
  
  identity {
    type = "SystemAssigned"
  }
  
  network_rules {
    default_action = "Allow"
    bypass         = ["AzureServices"]
  }
  
  tags = var.tags
}

# Storage container for application photos captured by security personnel
resource "azurerm_storage_container" "photos_container" {
  name                  = "photos"
  storage_account_name  = azurerm_storage_account.storage_account.name
  container_access_type = "private"
}

# Storage container for application backups and exports
resource "azurerm_storage_container" "backups_container" {
  name                  = "backups"
  storage_account_name  = azurerm_storage_account.storage_account.name
  container_access_type = "private"
}

# Lifecycle management policy to move older photos to cool storage tier
resource "azurerm_storage_management_policy" "lifecycle_management_policy" {
  storage_account_id = azurerm_storage_account.storage_account.id
  
  rule {
    name    = "MoveToCoolTier"
    enabled = true
    
    filters {
      prefix_match = ["photos/"]
      blob_types   = ["blockBlob"]
    }
    
    actions {
      base_blob {
        tier_to_cool_after_days_since_modification_greater_than = 30
        delete_after_days_since_modification_greater_than      = 365
      }
    }
  }
}

# Stores the storage account access key in Key Vault for secure access
resource "azurerm_key_vault_secret" "storage_account_key_secret" {
  name           = "storage-account-key"
  key_vault_id   = var.key_vault_id
  value          = azurerm_storage_account.storage_account.primary_access_key
  content_type   = "text/plain"
  expiration_date = null
}

# Stores the storage account connection string in Key Vault for secure access
resource "azurerm_key_vault_secret" "storage_connection_string_secret" {
  name           = "storage-connection-string"
  key_vault_id   = var.key_vault_id
  value          = azurerm_storage_account.storage_account.primary_connection_string
  content_type   = "text/plain"
  expiration_date = null
}