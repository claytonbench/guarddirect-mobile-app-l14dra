variable "environment" {
  description = "The deployment environment (dev, staging, prod) for resource naming and configuration"
  type        = string
  default     = "dev"
  
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

variable "location" {
  description = "The primary Azure region where resources will be deployed"
  type        = string
  default     = "eastus"
}

variable "resource_group_name" {
  description = "The base name for the Azure Resource Group"
  type        = string
  default     = "security-patrol-rg"
}

variable "app_service_sku" {
  description = "The SKU configuration for the App Service Plan"
  type = object({
    tier     = string
    size     = string
    capacity = number
  })
  default = {
    tier     = "Standard"
    size     = "S1"
    capacity = 2
  }
}

variable "sql_database_sku" {
  description = "The SKU for Azure SQL Database"
  type        = string
  default     = "S1"
}

variable "storage_account_tier" {
  description = "The tier and replication strategy for Azure Storage Account"
  type = object({
    tier        = string
    replication = string
  })
  default = {
    tier        = "Standard"
    replication = "LRS"
  }
}

variable "key_vault_name" {
  description = "The base name for Azure Key Vault"
  type        = string
  default     = "security-patrol-kv"
}

variable "traffic_manager_name" {
  description = "The base name for Azure Traffic Manager profile"
  type        = string
  default     = "security-patrol-tm"
}

variable "traffic_manager_dns_name" {
  description = "The DNS name for Azure Traffic Manager"
  type        = string
  default     = "security-patrol-api"
}

variable "tags" {
  description = "Resource tags to apply to all resources"
  type        = map(string)
  default = {
    Project   = "SecurityPatrol"
    ManagedBy = "Terraform"
  }
}

variable "security_alert_emails" {
  description = "Email addresses for security alerts (string or list of strings)"
  type        = any
  default     = "security@example.com"
}

variable "enable_geo_replication" {
  description = "Whether to enable geo-replication for high availability"
  type        = bool
  default     = false
}

variable "enable_advanced_monitoring" {
  description = "Whether to enable advanced monitoring features"
  type        = bool
  default     = false
}

variable "backup_retention_days" {
  description = "The number of days to retain backups"
  type        = number
  default     = 7
  
  validation {
    condition     = var.backup_retention_days >= 7 && var.backup_retention_days <= 35
    error_message = "Backup retention days must be between 7 and 35."
  }
}

variable "auto_scale_min_capacity" {
  description = "The minimum number of instances for auto-scaling"
  type        = number
  default     = 1
  
  validation {
    condition     = var.auto_scale_min_capacity >= 1
    error_message = "Minimum auto-scale capacity must be at least 1."
  }
}

variable "auto_scale_max_capacity" {
  description = "The maximum number of instances for auto-scaling"
  type        = number
  default     = 5
  
  validation {
    condition     = var.auto_scale_max_capacity >= var.auto_scale_min_capacity
    error_message = "Maximum auto-scale capacity must be greater than or equal to minimum capacity."
  }
}

variable "backend_subscription_id" {
  description = "The Azure subscription ID for Terraform state storage"
  type        = string
  sensitive   = true
}

variable "backend_tenant_id" {
  description = "The Azure tenant ID for Terraform state storage authentication"
  type        = string
  sensitive   = true
}