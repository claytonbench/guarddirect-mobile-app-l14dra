variable "resource_group_name" {
  type        = string
  description = "The name of the Azure Resource Group where storage resources will be deployed"
}

variable "location" {
  type        = string
  description = "The Azure region where storage resources will be deployed"
}

variable "environment" {
  type        = string
  description = "The deployment environment (dev, staging, prod) for resource naming and configuration"
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

variable "storage_account_tier" {
  type = object({
    tier        = string
    replication = string
  })
  description = "The tier and replication strategy for Azure Storage Account"
  default = {
    tier        = "Standard"
    replication = "LRS"
  }
}

variable "key_vault_id" {
  type        = string
  description = "The Azure Key Vault ID where storage access keys will be stored"
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to apply to all storage resources"
  default     = {}
}