variable "resource_group_name" {
  type        = string
  description = "The name of the Azure Resource Group where monitoring resources will be deployed"
}

variable "location" {
  type        = string
  description = "The Azure region where monitoring resources will be deployed"
}

variable "environment" {
  type        = string
  description = "The deployment environment (dev, staging, prod) for resource naming and configuration"
  
  validation {
    condition     = contains(["dev", "staging", "prod", "dev-secondary", "staging-secondary", "prod-secondary"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod, dev-secondary, staging-secondary, prod-secondary."
  }
}

variable "app_service_id" {
  type        = string
  description = "The resource ID of the App Service to monitor"
}

variable "alert_email_address" {
  type        = string
  description = "The email address to receive monitoring alerts"
  default     = "devops@example.com"
}

variable "app_insights_retention_days" {
  type        = number
  description = "The number of days to retain data in Application Insights"
  default     = 90
  
  validation {
    condition     = var.app_insights_retention_days >= 30 && var.app_insights_retention_days <= 730
    error_message = "Application Insights retention days must be between 30 and 730."
  }
}

variable "log_analytics_retention_days" {
  type        = number
  description = "The number of days to retain data in Log Analytics Workspace"
  default     = 30
  
  validation {
    condition     = var.log_analytics_retention_days >= 30 && var.log_analytics_retention_days <= 730
    error_message = "Log Analytics retention days must be between 30 and 730."
  }
}

variable "api_response_time_threshold_ms" {
  type        = number
  description = "The threshold in milliseconds for API response time alerts"
  default     = 1000
  
  validation {
    condition     = var.api_response_time_threshold_ms > 0
    error_message = "API response time threshold must be greater than 0."
  }
}

variable "api_failure_rate_threshold" {
  type        = number
  description = "The threshold percentage for API failure rate alerts"
  default     = 5
  
  validation {
    condition     = var.api_failure_rate_threshold >= 0 && var.api_failure_rate_threshold <= 100
    error_message = "API failure rate threshold must be between 0 and 100."
  }
}

variable "tags" {
  type        = map(string)
  description = "Resource tags to apply to all monitoring resources"
  default     = {}
}

variable "enable_web_test" {
  type        = bool
  description = "Whether to enable web test for API health endpoint monitoring"
  default     = true
}

variable "web_test_geo_locations" {
  type        = list(string)
  description = "List of geographic locations for web test monitoring"
  default     = ["us-ca-sjc-azr", "us-tx-sn1-azr", "us-il-ch1-azr", "us-va-ash-azr", "us-fl-mia-edge"]
}

variable "availability_threshold" {
  type        = number
  description = "The threshold percentage for availability alerts"
  default     = 99.5
  
  validation {
    condition     = var.availability_threshold > 0 && var.availability_threshold <= 100
    error_message = "Availability threshold must be between 0 and 100."
  }
}

variable "exception_threshold" {
  type        = number
  description = "The threshold count for exception alerts"
  default     = 5
  
  validation {
    condition     = var.exception_threshold >= 0
    error_message = "Exception threshold must be greater than or equal to 0."
  }
}

variable "alert_severity_critical" {
  type        = number
  description = "The severity level for critical alerts (0-4, where 0 is most severe)"
  default     = 1
  
  validation {
    condition     = var.alert_severity_critical >= 0 && var.alert_severity_critical <= 4
    error_message = "Alert severity must be between 0 and 4."
  }
}

variable "alert_severity_warning" {
  type        = number
  description = "The severity level for warning alerts (0-4, where 0 is most severe)"
  default     = 2
  
  validation {
    condition     = var.alert_severity_warning >= 0 && var.alert_severity_warning <= 4
    error_message = "Alert severity must be between 0 and 4."
  }
}