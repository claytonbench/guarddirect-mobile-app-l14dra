variable "resource_group_name" {
  type        = string
  description = "The name of the Azure Resource Group where database resources will be deployed"
}

variable "location" {
  type        = string
  description = "The primary Azure region where database resources will be deployed"
}

variable "secondary_location" {
  type        = string
  description = "The secondary Azure region for geo-replication of database resources"
}

variable "environment" {
  type        = string
  description = "The deployment environment (dev, staging, prod) for resource naming and configuration"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

variable "sql_database_sku" {
  type        = string
  description = "The SKU for Azure SQL Database (e.g., S1, S2, P1)"
  default     = "S1"
}

variable "key_vault_id" {
  type        = string
  description = "The Azure Key Vault ID for storing database secrets"
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to apply to all database resources"
  default     = {}
}

variable "audit_storage_account_endpoint" {
  type        = string
  description = "The storage account endpoint for SQL auditing"
}

variable "audit_storage_account_access_key" {
  type        = string
  description = "The storage account access key for SQL auditing"
  sensitive   = true
}

variable "audit_storage_account_container_url" {
  type        = string
  description = "The storage container URL for SQL vulnerability assessment"
}

variable "security_alert_emails" {
  type        = list(string)
  description = "Email addresses for database security alerts"
  default     = []
}