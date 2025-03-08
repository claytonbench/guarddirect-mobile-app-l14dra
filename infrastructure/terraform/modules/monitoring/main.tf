# Azure monitoring resources for Security Patrol Application

locals {
  app_insights_name = "appi-security-patrol-${var.environment}"
  log_analytics_workspace_name = "law-security-patrol-${var.environment}"
  app_service_name = "${element(split("/", var.app_service_id), length(split("/", var.app_service_id)) - 1)}"
}

# Log Analytics Workspace for centralized log storage and analysis
resource "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  name                = local.log_analytics_workspace_name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = var.log_analytics_retention_days
  tags                = var.tags
}

# Application Insights for application telemetry and performance monitoring
resource "azurerm_application_insights" "application_insights" {
  name                = local.app_insights_name
  resource_group_name = var.resource_group_name
  location            = var.location
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.log_analytics_workspace.id
  retention_in_days   = var.app_insights_retention_days
  sampling_percentage = 100
  tags                = var.tags
}

# Diagnostic settings for App Service to send logs to Log Analytics
resource "azurerm_monitor_diagnostic_setting" "app_service_diagnostic_setting" {
  name                       = "diag-${local.app_service_name}"
  target_resource_id         = var.app_service_id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.log_analytics_workspace.id

  log {
    category = "AppServiceHTTPLogs"
    enabled  = true

    retention_policy {
      enabled = true
      days    = var.log_analytics_retention_days
    }
  }

  log {
    category = "AppServiceConsoleLogs"
    enabled  = true

    retention_policy {
      enabled = true
      days    = var.log_analytics_retention_days
    }
  }

  log {
    category = "AppServiceAppLogs"
    enabled  = true

    retention_policy {
      enabled = true
      days    = var.log_analytics_retention_days
    }
  }

  log {
    category = "AppServiceAuditLogs"
    enabled  = true

    retention_policy {
      enabled = true
      days    = var.log_analytics_retention_days
    }
  }

  log {
    category = "AppServiceIPSecAuditLogs"
    enabled  = true

    retention_policy {
      enabled = true
      days    = var.log_analytics_retention_days
    }
  }

  log {
    category = "AppServicePlatformLogs"
    enabled  = true

    retention_policy {
      enabled = true
      days    = var.log_analytics_retention_days
    }
  }

  metric {
    category = "AllMetrics"
    enabled  = true

    retention_policy {
      enabled = true
      days    = var.log_analytics_retention_days
    }
  }
}

# Alert rule for API response time exceeding threshold
resource "azurerm_monitor_metric_alert" "api_response_time_alert" {
  name                = "alert-api-response-time-${var.environment}"
  resource_group_name = var.resource_group_name
  scopes              = [azurerm_application_insights.application_insights.id]
  description         = "Alert when API response time exceeds ${var.api_response_time_threshold_ms}ms"
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT15M"

  criteria {
    metric_namespace = "microsoft.insights/components"
    metric_name      = "requests/duration"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = var.api_response_time_threshold_ms
  }

  action {
    action_group_id = azurerm_monitor_action_group.email_alert_group.id
  }

  tags = var.tags
}

# Alert rule for API failure rate exceeding threshold
resource "azurerm_monitor_metric_alert" "api_failure_rate_alert" {
  name                = "alert-api-failure-rate-${var.environment}"
  resource_group_name = var.resource_group_name
  scopes              = [azurerm_application_insights.application_insights.id]
  description         = "Alert when API failure rate exceeds ${var.api_failure_rate_threshold}%"
  severity            = 1
  frequency           = "PT5M"
  window_size         = "PT15M"

  criteria {
    metric_namespace = "microsoft.insights/components"
    metric_name      = "requests/failed"
    aggregation      = "Count"
    operator         = "GreaterThan"
    threshold        = var.api_failure_rate_threshold
  }

  action {
    action_group_id = azurerm_monitor_action_group.email_alert_group.id
  }

  tags = var.tags
}

# Alert rule for application availability dropping below threshold
resource "azurerm_monitor_metric_alert" "availability_alert" {
  name                = "alert-availability-${var.environment}"
  resource_group_name = var.resource_group_name
  scopes              = [azurerm_application_insights.application_insights.id]
  description         = "Alert when application availability drops below 99.5%"
  severity            = 1
  frequency           = "PT5M"
  window_size         = "PT15M"

  criteria {
    metric_namespace = "microsoft.insights/components"
    metric_name      = "availabilityResults/availabilityPercentage"
    aggregation      = "Average"
    operator         = "LessThan"
    threshold        = 99.5
  }

  action {
    action_group_id = azurerm_monitor_action_group.email_alert_group.id
  }

  tags = var.tags
}

# Alert rule for application exceptions exceeding threshold
resource "azurerm_monitor_metric_alert" "exception_alert" {
  name                = "alert-exceptions-${var.environment}"
  resource_group_name = var.resource_group_name
  scopes              = [azurerm_application_insights.application_insights.id]
  description         = "Alert when application exceptions exceed threshold"
  severity            = 2
  frequency           = "PT5M"
  window_size         = "PT15M"

  criteria {
    metric_namespace = "microsoft.insights/components"
    metric_name      = "exceptions/count"
    aggregation      = "Count"
    operator         = "GreaterThan"
    threshold        = 5
  }

  action {
    action_group_id = azurerm_monitor_action_group.email_alert_group.id
  }

  tags = var.tags
}

# Action group for sending email notifications for alerts
resource "azurerm_monitor_action_group" "email_alert_group" {
  name                = "ag-email-${var.environment}"
  resource_group_name = var.resource_group_name
  short_name          = "EmailAlert"

  email_receiver {
    name                    = "DevOpsTeam"
    email_address           = var.alert_email_address
    use_common_alert_schema = true
  }

  tags = var.tags
}

# Web test to monitor API health endpoint availability
resource "azurerm_application_insights_web_test" "app_insights_web_test" {
  name                    = "webtest-api-health-${var.environment}"
  resource_group_name     = var.resource_group_name
  location                = var.location
  application_insights_id = azurerm_application_insights.application_insights.id
  kind                    = "ping"
  frequency               = 300
  timeout                 = 30
  enabled                 = true
  geo_locations           = ["us-ca-sjc-azr", "us-tx-sn1-azr", "us-il-ch1-azr", "us-va-ash-azr", "us-fl-mia-edge"]

  configuration = <<XML
<WebTest Name="API Health Check" Enabled="True" Timeout="30" Frequency="300" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Items>
    <Request Method="GET" Version="1.1" Url="https://${local.app_service_name}.azurewebsites.net/health" ThinkTime="0" />
  </Items>
  <ValidationRules>
    <ValidationRule Classname="Microsoft.VisualStudio.TestTools.WebTesting.Rules.ValidationRuleFindText, Microsoft.VisualStudio.QualityTools.WebTestFramework, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" DisplayName="Find Text" Description="Verifies the existence of the specified text in the response." Level="High" ExecutionOrder="BeforeDependents">
      <RuleParameters>
        <RuleParameter Name="FindText" Value="Healthy" />
        <RuleParameter Name="IgnoreCase" Value="True" />
        <RuleParameter Name="UseRegularExpression" Value="False" />
        <RuleParameter Name="PassIfTextFound" Value="True" />
      </RuleParameters>
    </ValidationRule>
  </ValidationRules>
</WebTest>
XML

  tags = var.tags
}

# Alert rule for web test failures
resource "azurerm_monitor_metric_alert" "web_test_alert" {
  name                = "alert-webtest-${var.environment}"
  resource_group_name = var.resource_group_name
  scopes              = [
    azurerm_application_insights.application_insights.id,
    azurerm_application_insights_web_test.app_insights_web_test.id
  ]
  description         = "Alert when health endpoint is unavailable"
  severity            = 1
  frequency           = "PT5M"
  window_size         = "PT15M"

  criteria {
    web_test_name       = azurerm_application_insights_web_test.app_insights_web_test.name
    web_test_id         = azurerm_application_insights_web_test.app_insights_web_test.id
    component_id        = azurerm_application_insights.application_insights.id
    failed_location_count = 2
  }

  action {
    action_group_id = azurerm_monitor_action_group.email_alert_group.id
  }

  tags = var.tags
}

# Azure Dashboard for Application Insights metrics visualization
resource "azurerm_dashboard" "app_insights_dashboard" {
  name                = "dashboard-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  dashboard_properties = <<DASHBOARD
{
  "lenses": {
    "0": {
      "order": 0,
      "parts": {
        "0": {
          "position": {
            "x": 0,
            "y": 0,
            "colSpan": 6,
            "rowSpan": 4
          },
          "metadata": {
            "inputs": [
              {
                "name": "resourceTypeMode",
                "value": "components"
              },
              {
                "name": "ComponentId",
                "value": "${azurerm_application_insights.application_insights.id}"
              }
            ],
            "type": "Extension/AppInsightsExtension/PartType/AppMapGalPt"
          }
        },
        "1": {
          "position": {
            "x": 6,
            "y": 0,
            "colSpan": 6,
            "rowSpan": 4
          },
          "metadata": {
            "inputs": [
              {
                "name": "ComponentId",
                "value": "${azurerm_application_insights.application_insights.id}"
              }
            ],
            "type": "Extension/AppInsightsExtension/PartType/AvailabilityNavButtonGalleryPt"
          }
        },
        "2": {
          "position": {
            "x": 0,
            "y": 4,
            "colSpan": 6,
            "rowSpan": 4
          },
          "metadata": {
            "inputs": [
              {
                "name": "ComponentId",
                "value": "${azurerm_application_insights.application_insights.id}"
              }
            ],
            "type": "Extension/AppInsightsExtension/PartType/PerformanceNavButtonGalleryPt"
          }
        },
        "3": {
          "position": {
            "x": 6,
            "y": 4,
            "colSpan": 6,
            "rowSpan": 4
          },
          "metadata": {
            "inputs": [
              {
                "name": "ComponentId",
                "value": "${azurerm_application_insights.application_insights.id}"
              }
            ],
            "type": "Extension/AppInsightsExtension/PartType/FailuresNavButtonGalleryPt"
          }
        }
      }
    }
  }
}
DASHBOARD

  tags = var.tags
}

# Output values for use by other modules
output "app_insights_key" {
  value     = azurerm_application_insights.application_insights.instrumentation_key
  sensitive = true
  description = "The instrumentation key for Application Insights"
}

output "app_insights_connection_string" {
  value     = azurerm_application_insights.application_insights.connection_string
  sensitive = true
  description = "The connection string for Application Insights"
}

output "app_insights_id" {
  value     = azurerm_application_insights.application_insights.id
  description = "The resource ID of Application Insights"
}

output "log_analytics_workspace_id" {
  value     = azurerm_log_analytics_workspace.log_analytics_workspace.id
  description = "The resource ID of Log Analytics Workspace"
}