variable "resource_group_name" {
  type        = string
  description = "The name of the Azure Resource Group where API resources will be deployed"
}

variable "location" {
  type        = string
  description = "The Azure region where API resources will be deployed"
}

variable "environment" {
  type        = string
  description = "The deployment environment (dev, staging, prod) for resource naming and configuration"
  validation {
    condition     = contains(["dev", "staging", "prod", "dev-secondary", "staging-secondary", "prod-secondary"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod, dev-secondary, staging-secondary, prod-secondary."
  }
}

variable "app_service_sku" {
  type = object({
    tier     = string
    size     = string
    capacity = number
  })
  description = "The SKU configuration for the App Service Plan"
  default = {
    tier     = "Standard"
    size     = "S1"
    capacity = 2
  }
}

variable "key_vault_id" {
  type        = string
  description = "The resource ID of the Azure Key Vault for storing secrets"
}

variable "app_insights_key" {
  type        = string
  description = "The instrumentation key for Application Insights monitoring"
}

variable "sql_connection_string" {
  type        = string
  description = "The Key Vault secret URI for the SQL Database connection string"
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to apply to all API resources"
  default     = {}
}

variable "enable_ip_restrictions" {
  type        = bool
  description = "Whether to enable IP restrictions for the App Service"
  default     = false
}

variable "allowed_ip_addresses" {
  type        = list(string)
  description = "List of IP addresses to allow access to the App Service when IP restrictions are enabled"
  default     = []
}

variable "always_on" {
  type        = bool
  description = "Whether the App Service should be configured to always be running"
  default     = true
}

variable "http2_enabled" {
  type        = bool
  description = "Whether HTTP/2 should be enabled for the App Service"
  default     = true
}

variable "min_tls_version" {
  type        = string
  description = "The minimum TLS version required for the App Service"
  default     = "1.2"
  validation {
    condition     = contains(["1.0", "1.1", "1.2"], var.min_tls_version)
    error_message = "Minimum TLS version must be one of: 1.0, 1.1, 1.2."
  }
}

variable "cors_allowed_origins" {
  type        = list(string)
  description = "List of origins to allow CORS access to the App Service"
  default     = ["*"]
}

variable "token_expiry_minutes" {
  type        = number
  description = "The expiry time in minutes for authentication tokens"
  default     = 480 # 8 hours, matching shift duration from requirements
  validation {
    condition     = var.token_expiry_minutes > 0
    error_message = "Token expiry minutes must be greater than 0."
  }
}

variable "health_check_path" {
  type        = string
  description = "The path used for health checks on the App Service"
  default     = "/health"
}

variable "autoscale_cpu_threshold_high" {
  type        = number
  description = "The CPU percentage threshold to trigger scale out"
  default     = 70
  validation {
    condition     = var.autoscale_cpu_threshold_high > var.autoscale_cpu_threshold_low
    error_message = "High CPU threshold must be greater than low CPU threshold."
  }
}

variable "autoscale_cpu_threshold_low" {
  type        = number
  description = "The CPU percentage threshold to trigger scale in"
  default     = 30
  validation {
    condition     = var.autoscale_cpu_threshold_low >= 0 && var.autoscale_cpu_threshold_low < 100
    error_message = "Low CPU threshold must be between 0 and 99."
  }
}

variable "autoscale_max_capacity" {
  type        = number
  description = "The maximum number of instances for auto-scaling"
  default     = 10
  validation {
    condition     = var.autoscale_max_capacity >= var.app_service_sku.capacity
    error_message = "Maximum auto-scale capacity must be greater than or equal to the initial capacity."
  }
}