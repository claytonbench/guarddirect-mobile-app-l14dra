terraform {
  required_version = ">= 1.0.0"
  
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

  # NOTE: In actual usage, variables cannot be used directly in the backend configuration
  # block because the backend is initialized before variables are loaded.
  # The following configuration shows the intended structure, but in practice you would need to:
  # - Use partial configuration and provide values at init time:
  #   terraform init \
  #     -backend-config="key=security-patrol-${environment}.tfstate" \
  #     -backend-config="subscription_id=${subscription_id}" \
  #     -backend-config="tenant_id=${tenant_id}"
  # - Or use different backend config files for each environment
  backend "azurerm" {
    resource_group_name  = "terraform-state-rg"
    storage_account_name = "securitypatrolstate"
    container_name       = "terraform-state"
    key                  = "security-patrol-${var.environment}.tfstate"
    subscription_id      = "${var.backend_subscription_id}"
    tenant_id            = "${var.backend_tenant_id}"
    use_azuread_auth     = true
    use_msi              = false
  }
}