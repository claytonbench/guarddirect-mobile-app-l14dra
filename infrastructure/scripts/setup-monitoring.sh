#!/bin/bash
#
# setup-monitoring.sh - Set up Azure monitoring resources for Security Patrol Application
#
# This script automates the setup and configuration of Azure monitoring resources including
# Application Insights, Log Analytics Workspace, alert rules, and dashboards to enable
# comprehensive monitoring of application performance, availability, and health.
#
# Usage: ./setup-monitoring.sh [options]
#   Options:
#     --deployment-method=<ARM|Bicep|CLI>   Method to use for deploying resources
#     --environment=<dev|staging|prod>      Target environment
#     --resource-group=<name>               Azure resource group name
#     --location=<location>                 Azure region
#     --subscription-id=<guid>              Azure subscription ID
#     --app-service=<name>                  App Service name to monitor
#     --sql-server=<name>                   SQL Server name to monitor
#     --sql-database=<name>                 SQL Database name to monitor
#     --storage-account=<name>              Storage Account name to monitor
#     --alert-email=<email>                 Email address for alerts
#     --verbose                             Enable verbose logging
#     --force                               Force recreation of resources
#     --help                                Show this help message
#

# Set strict mode
set -e

# Get script directory for relative path resolution
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Default values
DEPLOYMENT_METHOD="ARM"
ENVIRONMENT="dev"
RESOURCE_GROUP_NAME=""
LOCATION="eastus"
SUBSCRIPTION_ID=""
APP_SERVICE_NAME=""
SQL_SERVER_NAME=""
SQL_DATABASE_NAME="SecurityPatrolDb"
STORAGE_ACCOUNT_NAME=""
ALERT_EMAIL_ADDRESS="devops@example.com"
APP_INSIGHTS_RETENTION_DAYS=90
LOG_ANALYTICS_RETENTION_DAYS=30
API_RESPONSE_TIME_THRESHOLD=1000
API_FAILURE_RATE_THRESHOLD=5
OUTPUT_FILE="./monitoring-config.json"
VERBOSE=false
FORCE=false

# Log file
LOG_FILE="${SCRIPT_DIR}/setup-monitoring.log"

# Function to display usage information
usage() {
    echo "Usage: ./setup-monitoring.sh [options]"
    echo "  Options:"
    echo "    --deployment-method=<ARM|Bicep|CLI>   Method to use for deploying resources"
    echo "    --environment=<dev|staging|prod>      Target environment"
    echo "    --resource-group=<name>               Azure resource group name"
    echo "    --location=<location>                 Azure region"
    echo "    --subscription-id=<guid>              Azure subscription ID"
    echo "    --app-service=<name>                  App Service name to monitor"
    echo "    --sql-server=<name>                   SQL Server name to monitor"
    echo "    --sql-database=<name>                 SQL Database name to monitor"
    echo "    --storage-account=<name>              Storage Account name to monitor"
    echo "    --alert-email=<email>                 Email address for alerts"
    echo "    --app-insights-retention=<days>       Application Insights retention days"
    echo "    --log-analytics-retention=<days>      Log Analytics retention days"
    echo "    --api-response-threshold=<ms>         API response time threshold in ms"
    echo "    --api-failure-threshold=<%>           API failure rate threshold percentage"
    echo "    --output-file=<path>                  Path to save config as JSON"
    echo "    --verbose                             Enable verbose logging"
    echo "    --force                               Force recreation of resources"
    echo "    --help                                Show this help message"
    exit 1
}

# Function to log messages with timestamp and severity
log() {
    local level="$1"
    local message="$2"
    local timestamp=$(date +"%Y-%m-%d %H:%M:%S")
    
    # Set colors for console output
    local color_reset='\033[0m'
    local color_red='\033[0;31m'
    local color_green='\033[0;32m'
    local color_yellow='\033[0;33m'
    local color_blue='\033[0;34m'
    
    # Choose color based on log level
    local color=$color_reset
    case "$level" in
        "ERROR") color=$color_red ;;
        "WARNING") color=$color_yellow ;;
        "INFO") color=$color_green ;;
        "DEBUG") color=$color_blue ;;
    esac
    
    # Print to console with color
    echo -e "${color}${timestamp} [${level}] ${message}${color_reset}"
    
    # Write to log file
    echo "${timestamp} [${level}] ${message}" >> "$LOG_FILE"
}

# Function to check if required dependencies are installed
check_dependencies() {
    log "INFO" "Checking dependencies..."
    
    # Check for az CLI
    if ! command -v az &> /dev/null; then
        log "ERROR" "Azure CLI (az) is not installed. Please install it first."
        return 1
    fi
    
    # Check for jq (used for JSON processing)
    if ! command -v jq &> /dev/null; then
        log "ERROR" "jq is not installed. Please install it first."
        return 1
    fi
    
    log "INFO" "All dependencies are satisfied."
    return 0
}

# Function to check if user is logged into Azure
check_az_login() {
    log "INFO" "Checking Azure CLI login status..."
    
    # Try to get account information
    if ! az account show &> /dev/null; then
        log "WARNING" "Not logged into Azure. Initiating login..."
        az login
        if [ $? -ne 0 ]; then
            log "ERROR" "Failed to log in to Azure."
            return 1
        fi
    fi
    
    log "INFO" "Successfully logged into Azure."
    return 0
}

# Function to select the specified Azure subscription
select_subscription() {
    local subscription_id="$1"
    log "INFO" "Setting Azure subscription to $subscription_id..."
    
    if az account set --subscription "$subscription_id"; then
        log "INFO" "Successfully set subscription to $subscription_id."
        return 0
    else
        log "ERROR" "Failed to set subscription to $subscription_id."
        return 1
    fi
}

# Function to create a resource group if it doesn't exist
create_resource_group() {
    local name="$1"
    local location="$2"
    local tags="$3"
    
    log "INFO" "Checking if resource group $name exists..."
    
    # Check if resource group exists
    if az group show --name "$name" &> /dev/null; then
        log "INFO" "Resource group $name already exists."
    else
        log "INFO" "Creating resource group $name in $location..."
        if ! az group create --name "$name" --location "$location" --tags $tags; then
            log "ERROR" "Failed to create resource group $name."
            return 1
        fi
        log "INFO" "Resource group $name created successfully."
    fi
    
    return 0
}

# Function to deploy resources using ARM template
deploy_arm_template() {
    local resource_group="$1"
    local template_file="$2"
    local parameters_file="$3"
    
    log "INFO" "Deploying ARM template $template_file to resource group $resource_group..."
    
    # Validate the template first
    log "INFO" "Validating ARM template..."
    if ! az deployment group validate --resource-group "$resource_group" --template-file "$template_file" --parameters "@$parameters_file"; then
        log "ERROR" "ARM template validation failed."
        return 1
    fi
    
    # Deploy the template
    log "INFO" "Deploying ARM template..."
    if ! az deployment group create --resource-group "$resource_group" --template-file "$template_file" --parameters "@$parameters_file"; then
        log "ERROR" "ARM template deployment failed."
        return 1
    fi
    
    log "INFO" "ARM template deployment completed successfully."
    return 0
}

# Function to deploy resources using Bicep template
deploy_bicep_template() {
    local resource_group="$1"
    local template_file="$2"
    local parameters_file="$3"
    
    log "INFO" "Deploying Bicep template $template_file to resource group $resource_group..."
    
    # Validate the template first
    log "INFO" "Validating Bicep template..."
    if ! az deployment group validate --resource-group "$resource_group" --template-file "$template_file" --parameters "@$parameters_file"; then
        log "ERROR" "Bicep template validation failed."
        return 1
    fi
    
    # Deploy the template
    log "INFO" "Deploying Bicep template..."
    if ! az deployment group create --resource-group "$resource_group" --template-file "$template_file" --parameters "@$parameters_file"; then
        log "ERROR" "Bicep template deployment failed."
        return 1
    fi
    
    log "INFO" "Bicep template deployment completed successfully."
    return 0
}

# Function to create or update a Log Analytics workspace
create_log_analytics_workspace() {
    local resource_group="$1"
    local name="$2"
    local location="$3"
    local retention_days="$4"
    local tags="$5"
    
    log "INFO" "Creating/updating Log Analytics workspace $name..."
    
    # Check if Log Analytics workspace exists
    if az monitor log-analytics workspace show --resource-group "$resource_group" --workspace-name "$name" &> /dev/null; then
        if [ "$FORCE" = true ]; then
            log "INFO" "Log Analytics workspace $name exists. Updating..."
        else
            log "INFO" "Log Analytics workspace $name already exists. Skipping creation."
            # Get workspace ID for return
            local workspace_id=$(az monitor log-analytics workspace show --resource-group "$resource_group" --workspace-name "$name" --query id -o tsv)
            echo "$workspace_id"
            return 0
        fi
    else
        log "INFO" "Creating new Log Analytics workspace $name..."
    fi
    
    # Create or update the workspace
    local workspace_id=$(az monitor log-analytics workspace create \
        --resource-group "$resource_group" \
        --workspace-name "$name" \
        --location "$location" \
        --sku PerGB2018 \
        --retention-time "$retention_days" \
        --tags $tags \
        --query id -o tsv)
    
    if [ -z "$workspace_id" ]; then
        log "ERROR" "Failed to create/update Log Analytics workspace $name."
        return ""
    fi
    
    log "INFO" "Log Analytics workspace $name created/updated successfully."
    echo "$workspace_id"
}

# Function to create or update an Application Insights resource
create_application_insights() {
    local resource_group="$1"
    local name="$2"
    local location="$3"
    local workspace_id="$4"
    local retention_days="$5"
    local tags="$6"
    
    log "INFO" "Creating/updating Application Insights $name..."
    
    # Check if Application Insights exists
    if az monitor app-insights component show --resource-group "$resource_group" --app "$name" &> /dev/null; then
        if [ "$FORCE" = true ]; then
            log "INFO" "Application Insights $name exists. Updating..."
        else
            log "INFO" "Application Insights $name already exists. Skipping creation."
            # Get instrumentation key for return
            local instrumentation_key=$(az monitor app-insights component show \
                --resource-group "$resource_group" \
                --app "$name" \
                --query instrumentationKey -o tsv)
            echo "$instrumentation_key"
            return 0
        fi
    else
        log "INFO" "Creating new Application Insights $name..."
    fi
    
    # Create or update Application Insights
    local instrumentation_key=$(az monitor app-insights component create \
        --resource-group "$resource_group" \
        --app "$name" \
        --location "$location" \
        --application-type web \
        --kind web \
        --workspace "$workspace_id" \
        --retention-time "$retention_days" \
        --tags $tags \
        --query instrumentationKey -o tsv)
    
    if [ -z "$instrumentation_key" ]; then
        log "ERROR" "Failed to create/update Application Insights $name."
        return ""
    fi
    
    log "INFO" "Application Insights $name created/updated successfully."
    echo "$instrumentation_key"
}

# Function to create or update an Action Group
create_action_group() {
    local resource_group="$1"
    local name="$2"
    local short_name="$3"
    local email_address="$4"
    local tags="$5"
    
    log "INFO" "Creating/updating Action Group $name..."
    
    # Check if Action Group exists
    if az monitor action-group show --resource-group "$resource_group" --name "$name" &> /dev/null; then
        if [ "$FORCE" = true ]; then
            log "INFO" "Action Group $name exists. Updating..."
        else
            log "INFO" "Action Group $name already exists. Skipping creation."
            # Get action group ID for return
            local action_group_id=$(az monitor action-group show \
                --resource-group "$resource_group" \
                --name "$name" \
                --query id -o tsv)
            echo "$action_group_id"
            return 0
        fi
    else
        log "INFO" "Creating new Action Group $name..."
    fi
    
    # Create or update Action Group
    local action_group_id=$(az monitor action-group create \
        --resource-group "$resource_group" \
        --name "$name" \
        --short-name "$short_name" \
        --email-receiver name="EmailReceiver" email-address="$email_address" \
        --tags $tags \
        --query id -o tsv)
    
    if [ -z "$action_group_id" ]; then
        log "ERROR" "Failed to create/update Action Group $name."
        return ""
    fi
    
    log "INFO" "Action Group $name created/updated successfully."
    echo "$action_group_id"
}

# Function to create or update a metric alert rule
create_metric_alert() {
    local resource_group="$1"
    local name="$2"
    local description="$3"
    local severity="$4"
    local resource_id="$5"
    local metric_name="$6"
    local operator="$7"
    local threshold="$8"
    local aggregation="$9"
    local action_group_id="${10}"
    local tags="${11}"
    
    log "INFO" "Creating/updating metric alert $name..."
    
    # Check if alert exists
    if az monitor metrics alert show --resource-group "$resource_group" --name "$name" &> /dev/null; then
        if [ "$FORCE" = true ]; then
            log "INFO" "Metric alert $name exists. Updating..."
        else
            log "INFO" "Metric alert $name already exists. Skipping creation."
            return 0
        fi
    else
        log "INFO" "Creating new metric alert $name..."
    fi
    
    # Create or update metric alert
    if ! az monitor metrics alert create \
        --resource-group "$resource_group" \
        --name "$name" \
        --description "$description" \
        --severity "$severity" \
        --scopes "$resource_id" \
        --condition "avg $metric_name > $threshold" \
        --evaluation-frequency 5m \
        --window-size 15m \
        --action "$action_group_id" \
        --tags $tags; then
        
        log "ERROR" "Failed to create/update metric alert $name."
        return 1
    fi
    
    log "INFO" "Metric alert $name created/updated successfully."
    return 0
}

# Function to create or update an availability test (web test)
create_availability_test() {
    local resource_group="$1"
    local name="$2"
    local app_insights_id="$3"
    local app_insights_name="$4"
    local url="$5"
    local frequency="$6"
    local tags="$7"
    
    log "INFO" "Creating/updating availability test $name..."
    
    # Check if availability test exists
    if az monitor app-insights web-test show --resource-group "$resource_group" --name "$name" &> /dev/null; then
        if [ "$FORCE" = true ]; then
            log "INFO" "Availability test $name exists. Updating..."
        else
            log "INFO" "Availability test $name already exists. Skipping creation."
            return 0
        fi
    else
        log "INFO" "Creating new availability test $name..."
    fi
    
    # Create the web test configuration XML
    local temp_xml_file=$(mktemp)
    cat > "$temp_xml_file" << EOF
<WebTest Name="API Health Check" Enabled="True" Timeout="30" Frequency="$frequency" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Items>
    <Request Method="GET" Version="1.1" Url="$url" ThinkTime="0" />
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
EOF
    
    # Create or update availability test using REST API (az CLI doesn't fully support this)
    local subscription_id=$(az account show --query id -o tsv)
    local api_version="2020-02-02"
    local url="https://management.azure.com/subscriptions/$subscription_id/resourceGroups/$resource_group/providers/Microsoft.Insights/webtests/$name?api-version=$api_version"
    
    # Get the access token for REST API call
    local access_token=$(az account get-access-token --query accessToken -o tsv)
    
    # Create the request body
    local request_body=$(cat <<EOF
{
  "location": "$(az group show --name $resource_group --query location -o tsv)",
  "tags": {
    "hidden-link:$app_insights_id": "Resource"
  },
  "properties": {
    "SyntheticMonitorId": "$name",
    "Name": "API Health Check",
    "Description": "Checks the health endpoint of the application API",
    "Enabled": true,
    "Frequency": $frequency,
    "Timeout": 30,
    "Kind": "ping",
    "RetryEnabled": true,
    "Locations": [
      { "Id": "us-ca-sjc-azr" },
      { "Id": "us-tx-sn1-azr" },
      { "Id": "us-il-ch1-azr" },
      { "Id": "us-va-ash-azr" },
      { "Id": "us-fl-mia-edge" }
    ],
    "Configuration": {
      "WebTest": "$(cat $temp_xml_file | sed 's/"/\\"/g' | tr -d '\n\r')"
    }
  }
}
EOF
)
    
    # Make the REST API call
    local response=$(curl -s -X PUT -H "Authorization: Bearer $access_token" -H "Content-Type: application/json" -d "$request_body" "$url")
    
    # Clean up temporary file
    rm -f "$temp_xml_file"
    
    # Check if successful
    if echo "$response" | jq -e '.id' >/dev/null; then
        log "INFO" "Availability test $name created/updated successfully."
        
        # Create associated alert rule
        local alert_name="alert-$name-availability"
        if ! az monitor metrics alert create \
            --resource-group "$resource_group" \
            --name "$alert_name" \
            --description "Alert when API health check fails" \
            --severity 1 \
            --scopes "$app_insights_id" \
            --condition "avg availabilityResults/availabilityPercentage < 90" \
            --evaluation-frequency 5m \
            --window-size 15m \
            --action "$action_group_id" \
            --tags $tags; then
            
            log "ERROR" "Failed to create alert for availability test $name."
            return 1
        fi
        
        return 0
    else
        log "ERROR" "Failed to create/update availability test $name. Response: $response"
        return 1
    fi
}

# Function to create or update diagnostic settings for a resource
create_diagnostic_setting() {
    local resource_id="$1"
    local name="$2"
    local workspace_id="$3"
    local log_categories="$4"
    local enable_metrics="$5"
    
    log "INFO" "Creating/updating diagnostic setting $name for resource..."
    
    # Check if diagnostic setting exists
    if az monitor diagnostic-settings show --resource "$resource_id" --name "$name" &> /dev/null; then
        if [ "$FORCE" = true ]; then
            log "INFO" "Diagnostic setting $name exists. Updating..."
        else
            log "INFO" "Diagnostic setting $name already exists. Skipping creation."
            return 0
        fi
    else
        log "INFO" "Creating new diagnostic setting $name..."
    fi
    
    # Prepare log settings parameter
    local logs_param=""
    if [ -n "$log_categories" ]; then
        for category in $(echo $log_categories | tr ',' ' '); do
            logs_param="$logs_param --logs category=$category enabled=true"
        done
    fi
    
    # Prepare metrics parameter
    local metrics_param=""
    if [ "$enable_metrics" = true ]; then
        metrics_param="--metrics category=AllMetrics enabled=true"
    fi
    
    # Create or update diagnostic setting
    if ! az monitor diagnostic-settings create \
        --resource "$resource_id" \
        --name "$name" \
        --workspace "$workspace_id" \
        $logs_param \
        $metrics_param; then
        
        log "ERROR" "Failed to create/update diagnostic setting $name."
        return 1
    fi
    
    log "INFO" "Diagnostic setting $name created/updated successfully."
    return 0
}

# Function to create or update a monitoring dashboard
create_dashboard() {
    local resource_group="$1"
    local name="$2"
    local location="$3"
    local app_insights_id="$4"
    local dashboard_type="$5"
    local tags="$6"
    
    log "INFO" "Creating/updating dashboard $name of type $dashboard_type..."
    
    # Check if dashboard exists
    if az portal dashboard show --resource-group "$resource_group" --name "$name" &> /dev/null; then
        if [ "$FORCE" = true ]; then
            log "INFO" "Dashboard $name exists. Updating..."
        else
            log "INFO" "Dashboard $name already exists. Skipping creation."
            return 0
        fi
    else
        log "INFO" "Creating new dashboard $name..."
    fi
    
    # Create template JSON file based on dashboard type
    local temp_json_file=$(mktemp)
    
    # Different JSON structures for different dashboard types
    if [ "$dashboard_type" = "Executive" ]; then
        # Executive dashboard with high-level metrics
        cat > "$temp_json_file" << EOF
{
  "properties": {
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
                  "name": "ComponentId",
                  "value": "$app_insights_id"
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
                  "value": "$app_insights_id"
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
                  "value": "$app_insights_id"
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
                  "value": "$app_insights_id"
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/FailuresNavButtonGalleryPt"
            }
          }
        }
      }
    }
  }
}
EOF
    elif [ "$dashboard_type" = "Technical" ]; then
        # Technical dashboard with detailed metrics
        cat > "$temp_json_file" << EOF
{
  "properties": {
    "lenses": {
      "0": {
        "order": 0,
        "parts": {
          "0": {
            "position": {
              "x": 0,
              "y": 0,
              "colSpan": 4,
              "rowSpan": 3
            },
            "metadata": {
              "inputs": [
                {
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                },
                {
                  "name": "TimeContext",
                  "value": {
                    "durationMs": 86400000,
                    "endTime": null,
                    "createdTime": "2023-01-01T00:00:00.000Z",
                    "isInitialTime": true,
                    "grain": 1,
                    "useDashboardTimeRange": false
                  }
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/AvailabilityDurationPanelBlade"
            }
          },
          "1": {
            "position": {
              "x": 4,
              "y": 0,
              "colSpan": 4,
              "rowSpan": 3
            },
            "metadata": {
              "inputs": [
                {
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                },
                {
                  "name": "TimeContext",
                  "value": {
                    "durationMs": 86400000,
                    "endTime": null,
                    "createdTime": "2023-01-01T00:00:00.000Z",
                    "isInitialTime": true,
                    "grain": 1,
                    "useDashboardTimeRange": false
                  }
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/PerformancePanelBlade"
            }
          },
          "2": {
            "position": {
              "x": 8,
              "y": 0,
              "colSpan": 4,
              "rowSpan": 3
            },
            "metadata": {
              "inputs": [
                {
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                },
                {
                  "name": "TimeContext",
                  "value": {
                    "durationMs": 86400000,
                    "endTime": null,
                    "createdTime": "2023-01-01T00:00:00.000Z",
                    "isInitialTime": true,
                    "grain": 1,
                    "useDashboardTimeRange": false
                  }
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/FailuresPanelBlade"
            }
          },
          "3": {
            "position": {
              "x": 0,
              "y": 3,
              "colSpan": 4,
              "rowSpan": 3
            },
            "metadata": {
              "inputs": [
                {
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                },
                {
                  "name": "TimeContext",
                  "value": {
                    "durationMs": 86400000,
                    "endTime": null,
                    "createdTime": "2023-01-01T00:00:00.000Z",
                    "isInitialTime": true,
                    "grain": 1,
                    "useDashboardTimeRange": false
                  }
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/UsagePanelBlade"
            }
          },
          "4": {
            "position": {
              "x": 4,
              "y": 3,
              "colSpan": 4,
              "rowSpan": 3
            },
            "metadata": {
              "inputs": [
                {
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                },
                {
                  "name": "TimeContext",
                  "value": {
                    "durationMs": 86400000,
                    "endTime": null,
                    "createdTime": "2023-01-01T00:00:00.000Z",
                    "isInitialTime": true,
                    "grain": 1,
                    "useDashboardTimeRange": false
                  }
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/MetricsExplorerBlade"
            }
          },
          "5": {
            "position": {
              "x": 8,
              "y": 3,
              "colSpan": 4,
              "rowSpan": 3
            },
            "metadata": {
              "inputs": [
                {
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                },
                {
                  "name": "TimeContext",
                  "value": {
                    "durationMs": 86400000,
                    "endTime": null,
                    "createdTime": "2023-01-01T00:00:00.000Z",
                    "isInitialTime": true,
                    "grain": 1,
                    "useDashboardTimeRange": false
                  }
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/ExceptionsBlade"
            }
          }
        }
      }
    }
  }
}
EOF
    elif [ "$dashboard_type" = "Operations" ]; then
        # Operations dashboard for day-to-day monitoring
        cat > "$temp_json_file" << EOF
{
  "properties": {
    "lenses": {
      "0": {
        "order": 0,
        "parts": {
          "0": {
            "position": {
              "x": 0,
              "y": 0,
              "colSpan": 6,
              "rowSpan": 3
            },
            "metadata": {
              "inputs": [
                {
                  "name": "resourceTypeMode",
                  "value": "components"
                },
                {
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/AspNetOverviewPinPart"
            }
          },
          "1": {
            "position": {
              "x": 6,
              "y": 0,
              "colSpan": 6,
              "rowSpan": 3
            },
            "metadata": {
              "inputs": [
                {
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                },
                {
                  "name": "TimeContext",
                  "value": {
                    "durationMs": 86400000,
                    "endTime": null,
                    "createdTime": "2023-01-01T00:00:00.000Z",
                    "isInitialTime": true,
                    "grain": 1,
                    "useDashboardTimeRange": false
                  }
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/LiveMetricsBlade"
            }
          },
          "2": {
            "position": {
              "x": 0,
              "y": 3,
              "colSpan": 6,
              "rowSpan": 3
            },
            "metadata": {
              "inputs": [
                {
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                },
                {
                  "name": "TimeContext",
                  "value": {
                    "durationMs": 86400000,
                    "endTime": null,
                    "createdTime": "2023-01-01T00:00:00.000Z",
                    "isInitialTime": true,
                    "grain": 1,
                    "useDashboardTimeRange": false
                  }
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/RequestsMapBladePinPart"
            }
          },
          "3": {
            "position": {
              "x": 6,
              "y": 3,
              "colSpan": 6,
              "rowSpan": 3
            },
            "metadata": {
              "inputs": [
                {
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                },
                {
                  "name": "TimeContext",
                  "value": {
                    "durationMs": 86400000,
                    "endTime": null,
                    "createdTime": "2023-01-01T00:00:00.000Z",
                    "isInitialTime": true,
                    "grain": 1,
                    "useDashboardTimeRange": false
                  }
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/AlertsListBlade"
            }
          }
        }
      }
    }
  }
}
EOF
    else
        # Default dashboard with basic metrics
        cat > "$temp_json_file" << EOF
{
  "properties": {
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
                  "name": "ComponentId",
                  "value": "$app_insights_id"
                }
              ],
              "type": "Extension/AppInsightsExtension/PartType/AppMapGalPt"
            }
          }
        }
      }
    }
  }
}
EOF
    fi
    
    # Create or update dashboard
    if ! az portal dashboard create \
        --resource-group "$resource_group" \
        --name "$name" \
        --location "$location" \
        --tags $tags \
        --input-path "$temp_json_file"; then
        
        log "ERROR" "Failed to create/update dashboard $name."
        rm -f "$temp_json_file"
        return 1
    fi
    
    # Clean up temporary file
    rm -f "$temp_json_file"
    
    log "INFO" "Dashboard $name created/updated successfully."
    return 0
}

# Function to export monitoring configuration to a JSON file
export_monitoring_config() {
    local app_insights_id="$1"
    local app_insights_key="$2"
    local workspace_id="$3"
    local output_file="$4"
    
    log "INFO" "Exporting monitoring configuration to $output_file..."
    
    # Create configuration object
    local config=$(cat <<EOF
{
  "appInsights": {
    "instrumentationKey": "$app_insights_key",
    "resourceId": "$app_insights_id"
  },
  "logAnalytics": {
    "workspaceId": "$workspace_id"
  },
  "dashboards": {
    "executiveDashboardUrl": "https://portal.azure.com/#dashboard/private/$RESOURCE_GROUP_NAME-dashboard-executive",
    "technicalDashboardUrl": "https://portal.azure.com/#dashboard/private/$RESOURCE_GROUP_NAME-dashboard-technical",
    "operationsDashboardUrl": "https://portal.azure.com/#dashboard/private/$RESOURCE_GROUP_NAME-dashboard-operations"
  },
  "alerts": {
    "responseTimeThreshold": $API_RESPONSE_TIME_THRESHOLD,
    "failureRateThreshold": $API_FAILURE_RATE_THRESHOLD,
    "emailRecipient": "$ALERT_EMAIL_ADDRESS"
  },
  "timestamp": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "environment": "$ENVIRONMENT"
}
EOF
)
    
    # Write to file
    echo "$config" > "$output_file"
    
    if [ $? -eq 0 ]; then
        log "INFO" "Configuration exported successfully to $output_file."
        return 0
    else
        log "ERROR" "Failed to export configuration to $output_file."
        return 1
    fi
}

# Function to perform cleanup operations
cleanup() {
    log "INFO" "Performing cleanup operations..."
    # Add cleanup code here if needed
    log "INFO" "Cleanup completed."
}

# Trap to ensure cleanup runs on exit
trap cleanup EXIT

# Parse command line arguments
while [ "$#" -gt 0 ]; do
    case "$1" in
        --deployment-method=*)
            DEPLOYMENT_METHOD="${1#*=}"
            ;;
        --environment=*)
            ENVIRONMENT="${1#*=}"
            ;;
        --resource-group=*)
            RESOURCE_GROUP_NAME="${1#*=}"
            ;;
        --location=*)
            LOCATION="${1#*=}"
            ;;
        --subscription-id=*)
            SUBSCRIPTION_ID="${1#*=}"
            ;;
        --app-service=*)
            APP_SERVICE_NAME="${1#*=}"
            ;;
        --sql-server=*)
            SQL_SERVER_NAME="${1#*=}"
            ;;
        --sql-database=*)
            SQL_DATABASE_NAME="${1#*=}"
            ;;
        --storage-account=*)
            STORAGE_ACCOUNT_NAME="${1#*=}"
            ;;
        --alert-email=*)
            ALERT_EMAIL_ADDRESS="${1#*=}"
            ;;
        --app-insights-retention=*)
            APP_INSIGHTS_RETENTION_DAYS="${1#*=}"
            ;;
        --log-analytics-retention=*)
            LOG_ANALYTICS_RETENTION_DAYS="${1#*=}"
            ;;
        --api-response-threshold=*)
            API_RESPONSE_TIME_THRESHOLD="${1#*=}"
            ;;
        --api-failure-threshold=*)
            API_FAILURE_RATE_THRESHOLD="${1#*=}"
            ;;
        --output-file=*)
            OUTPUT_FILE="${1#*=}"
            ;;
        --verbose)
            VERBOSE=true
            ;;
        --force)
            FORCE=true
            ;;
        --help)
            usage
            ;;
        *)
            echo "Unknown parameter passed: $1"
            usage
            ;;
    esac
    shift
done

# Validate required parameters
if [ -z "$RESOURCE_GROUP_NAME" ]; then
    log "ERROR" "Resource group name is required. Use --resource-group=<name>"
    usage
fi

if [ -z "$APP_SERVICE_NAME" ]; then
    log "ERROR" "App Service name is required. Use --app-service=<name>"
    usage
fi

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]]; then
    log "ERROR" "Environment must be one of: dev, staging, prod"
    usage
fi

# Validate deployment method
if [[ ! "$DEPLOYMENT_METHOD" =~ ^(ARM|Bicep|CLI)$ ]]; then
    log "ERROR" "Deployment method must be one of: ARM, Bicep, CLI"
    usage
fi

# Set default resource names if not provided
if [ -z "$STORAGE_ACCOUNT_NAME" ]; then
    STORAGE_ACCOUNT_NAME="stsecuritypatrol${ENVIRONMENT}"
    log "INFO" "Using default storage account name: $STORAGE_ACCOUNT_NAME"
fi

if [ -z "$SQL_SERVER_NAME" ]; then
    SQL_SERVER_NAME="sql-security-patrol-${ENVIRONMENT}"
    log "INFO" "Using default SQL server name: $SQL_SERVER_NAME"
fi

# Initialize log file
> "$LOG_FILE"
log "INFO" "Starting monitoring setup for environment: $ENVIRONMENT"
log "INFO" "Deployment method: $DEPLOYMENT_METHOD"
log "INFO" "Resource group: $RESOURCE_GROUP_NAME"
log "INFO" "Location: $LOCATION"

# Check dependencies
if ! check_dependencies; then
    log "ERROR" "Missing dependencies. Please install required tools."
    exit 1
fi

# Verify Azure login
if ! check_az_login; then
    log "ERROR" "Azure login failed. Please try again."
    exit 1
fi

# Select subscription if provided
if [ -n "$SUBSCRIPTION_ID" ]; then
    if ! select_subscription "$SUBSCRIPTION_ID"; then
        log "ERROR" "Failed to set subscription. Please check the subscription ID."
        exit 1
    fi
fi

# Create resource group if it doesn't exist
tags="Application=SecurityPatrol Environment=$ENVIRONMENT ManagedBy=AzureCLI"
if ! create_resource_group "$RESOURCE_GROUP_NAME" "$LOCATION" "$tags"; then
    log "ERROR" "Failed to create or validate resource group."
    exit 1
fi

# Setup monitoring based on deployment method
if [ "$DEPLOYMENT_METHOD" = "ARM" ]; then
    log "INFO" "Deploying monitoring resources using ARM template..."
    
    # Set parameters file based on environment
    parameters_file="${SCRIPT_DIR}/../azure/arm-templates/parameters/${ENVIRONMENT}.json"
    if [ ! -f "$parameters_file" ]; then
        log "ERROR" "Parameters file not found: $parameters_file"
        exit 1
    fi
    
    # Deploy ARM template
    template_file="${SCRIPT_DIR}/../azure/arm-templates/monitoring.json"
    if ! deploy_arm_template "$RESOURCE_GROUP_NAME" "$template_file" "$parameters_file"; then
        log "ERROR" "ARM template deployment failed."
        exit 1
    fi
    
    # Get deployed resources for output
    app_insights_name="ai-security-patrol-${ENVIRONMENT}"
    app_insights_key=$(az monitor app-insights component show \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --app "$app_insights_name" \
        --query instrumentationKey -o tsv)
    
    app_insights_id=$(az monitor app-insights component show \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --app "$app_insights_name" \
        --query id -o tsv)
    
    workspace_name="law-security-patrol-${ENVIRONMENT}"
    workspace_id=$(az monitor log-analytics workspace show \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --workspace-name "$workspace_name" \
        --query id -o tsv)
    
    # Export configuration
    export_monitoring_config "$app_insights_id" "$app_insights_key" "$workspace_id" "$OUTPUT_FILE"
    
elif [ "$DEPLOYMENT_METHOD" = "Bicep" ]; then
    log "INFO" "Deploying monitoring resources using Bicep template..."
    
    # Set parameters file based on environment
    parameters_file="${SCRIPT_DIR}/../azure/bicep/parameters/${ENVIRONMENT}.json"
    if [ ! -f "$parameters_file" ]; then
        log "ERROR" "Parameters file not found: $parameters_file"
        exit 1
    fi
    
    # Deploy Bicep template
    template_file="${SCRIPT_DIR}/../azure/bicep/monitoring.bicep"
    if ! deploy_bicep_template "$RESOURCE_GROUP_NAME" "$template_file" "$parameters_file"; then
        log "ERROR" "Bicep template deployment failed."
        exit 1
    fi
    
    # Get deployed resources for output
    app_insights_name="ai-security-patrol-${ENVIRONMENT}"
    app_insights_key=$(az monitor app-insights component show \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --app "$app_insights_name" \
        --query instrumentationKey -o tsv)
    
    app_insights_id=$(az monitor app-insights component show \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --app "$app_insights_name" \
        --query id -o tsv)
    
    workspace_name="law-security-patrol-${ENVIRONMENT}"
    workspace_id=$(az monitor log-analytics workspace show \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --workspace-name "$workspace_name" \
        --query id -o tsv)
    
    # Export configuration
    export_monitoring_config "$app_insights_id" "$app_insights_key" "$workspace_id" "$OUTPUT_FILE"
    
else  # CLI deployment method
    log "INFO" "Deploying monitoring resources using Azure CLI commands..."
    
    # Resource names
    log_analytics_name="law-security-patrol-${ENVIRONMENT}"
    app_insights_name="ai-security-patrol-${ENVIRONMENT}"
    action_group_name="ag-security-patrol-${ENVIRONMENT}"
    
    # Create Log Analytics workspace
    workspace_id=$(create_log_analytics_workspace "$RESOURCE_GROUP_NAME" "$log_analytics_name" "$LOCATION" "$LOG_ANALYTICS_RETENTION_DAYS" "$tags")
    if [ -z "$workspace_id" ]; then
        log "ERROR" "Failed to create Log Analytics workspace."
        exit 1
    fi
    
    # Create Application Insights
    app_insights_key=$(create_application_insights "$RESOURCE_GROUP_NAME" "$app_insights_name" "$LOCATION" "$workspace_id" "$APP_INSIGHTS_RETENTION_DAYS" "$tags")
    if [ -z "$app_insights_key" ]; then
        log "ERROR" "Failed to create Application Insights resource."
        exit 1
    fi
    
    # Get Application Insights ID
    app_insights_id=$(az monitor app-insights component show \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --app "$app_insights_name" \
        --query id -o tsv)
    
    # Create Action Group for alerts
    action_group_id=$(create_action_group "$RESOURCE_GROUP_NAME" "$action_group_name" "SecPatrol" "$ALERT_EMAIL_ADDRESS" "$tags")
    if [ -z "$action_group_id" ]; then
        log "ERROR" "Failed to create Action Group."
        exit 1
    fi
    
    # Get App Service resource ID
    app_service_id=$(az webapp show \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --name "$APP_SERVICE_NAME" \
        --query id -o tsv)
    
    if [ -z "$app_service_id" ]; then
        log "ERROR" "Failed to get App Service resource ID. Make sure the App Service exists."
        exit 1
    fi
    
    # Create API response time alert
    create_metric_alert \
        "$RESOURCE_GROUP_NAME" \
        "alert-api-response-time-${ENVIRONMENT}" \
        "Alert when API response time exceeds ${API_RESPONSE_TIME_THRESHOLD}ms" \
        "2" \
        "$app_service_id" \
        "HttpResponseTime" \
        "GreaterThan" \
        "$API_RESPONSE_TIME_THRESHOLD" \
        "Average" \
        "$action_group_id" \
        "$tags"
    
    # Create API failure rate alert
    create_metric_alert \
        "$RESOURCE_GROUP_NAME" \
        "alert-api-failure-rate-${ENVIRONMENT}" \
        "Alert when API failure rate exceeds ${API_FAILURE_RATE_THRESHOLD}%" \
        "1" \
        "$app_service_id" \
        "Http5xx" \
        "GreaterThan" \
        "$API_FAILURE_RATE_THRESHOLD" \
        "Total" \
        "$action_group_id" \
        "$tags"
    
    # Create CPU high alert
    create_metric_alert \
        "$RESOURCE_GROUP_NAME" \
        "alert-cpu-high-${ENVIRONMENT}" \
        "Alert when CPU usage exceeds 80%" \
        "2" \
        "$app_service_id" \
        "CpuPercentage" \
        "GreaterThan" \
        "80" \
        "Average" \
        "$action_group_id" \
        "$tags"
    
    # Create memory high alert
    create_metric_alert \
        "$RESOURCE_GROUP_NAME" \
        "alert-memory-high-${ENVIRONMENT}" \
        "Alert when memory usage exceeds 80%" \
        "2" \
        "$app_service_id" \
        "MemoryPercentage" \
        "GreaterThan" \
        "80" \
        "Average" \
        "$action_group_id" \
        "$tags"
    
    # Get App Service hostname for availability test
    app_service_hostname=$(az webapp show \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --name "$APP_SERVICE_NAME" \
        --query defaultHostName -o tsv)
    
    # Create availability test for API health endpoint
    create_availability_test \
        "$RESOURCE_GROUP_NAME" \
        "webtest-api-health-${ENVIRONMENT}" \
        "$app_insights_id" \
        "$app_insights_name" \
        "https://${app_service_hostname}/health" \
        "300" \
        "$tags"
    
    # Create diagnostic setting for App Service
    create_diagnostic_setting \
        "$app_service_id" \
        "diag-appservice-${ENVIRONMENT}" \
        "$workspace_id" \
        "AppServiceHTTPLogs,AppServiceConsoleLogs,AppServiceAppLogs,AppServiceAuditLogs" \
        true
    
    # If SQL Server and Database are provided, create diagnostic setting for database
    if [ -n "$SQL_SERVER_NAME" ] && [ -n "$SQL_DATABASE_NAME" ]; then
        # Get SQL Database resource ID
        sql_db_id=$(az sql db show \
            --resource-group "$RESOURCE_GROUP_NAME" \
            --server "$SQL_SERVER_NAME" \
            --name "$SQL_DATABASE_NAME" \
            --query id -o tsv)
        
        if [ -n "$sql_db_id" ]; then
            create_diagnostic_setting \
                "$sql_db_id" \
                "diag-sqldb-${ENVIRONMENT}" \
                "$workspace_id" \
                "SQLInsights,AutomaticTuning,QueryStoreRuntimeStatistics,QueryStoreWaitStatistics,Errors,DatabaseWaitStatistics,Timeouts,Blocks,Deadlocks" \
                true
        else
            log "WARNING" "Could not find SQL Database. Skipping diagnostic setting creation."
        fi
    fi
    
    # If Storage Account is provided, create diagnostic setting for it
    if [ -n "$STORAGE_ACCOUNT_NAME" ]; then
        # Get Storage Account resource ID
        storage_id=$(az storage account show \
            --resource-group "$RESOURCE_GROUP_NAME" \
            --name "$STORAGE_ACCOUNT_NAME" \
            --query id -o tsv)
        
        if [ -n "$storage_id" ]; then
            create_diagnostic_setting \
                "$storage_id" \
                "diag-storage-${ENVIRONMENT}" \
                "$workspace_id" \
                "" \
                true
            
            # Also create diagnostic setting for blob service
            create_diagnostic_setting \
                "${storage_id}/blobServices/default" \
                "diag-storage-blob-${ENVIRONMENT}" \
                "$workspace_id" \
                "StorageRead,StorageWrite,StorageDelete" \
                true
        else
            log "WARNING" "Could not find Storage Account. Skipping diagnostic setting creation."
        fi
    fi
    
    # Create dashboards
    create_dashboard \
        "$RESOURCE_GROUP_NAME" \
        "dashboard-executive-${ENVIRONMENT}" \
        "$LOCATION" \
        "$app_insights_id" \
        "Executive" \
        "$tags"
    
    create_dashboard \
        "$RESOURCE_GROUP_NAME" \
        "dashboard-technical-${ENVIRONMENT}" \
        "$LOCATION" \
        "$app_insights_id" \
        "Technical" \
        "$tags"
    
    create_dashboard \
        "$RESOURCE_GROUP_NAME" \
        "dashboard-operations-${ENVIRONMENT}" \
        "$LOCATION" \
        "$app_insights_id" \
        "Operations" \
        "$tags"
    
    # Export monitoring configuration
    export_monitoring_config "$app_insights_id" "$app_insights_key" "$workspace_id" "$OUTPUT_FILE"
fi

log "INFO" "Monitoring setup completed successfully!"
log "INFO" "Configuration exported to: $OUTPUT_FILE"

exit 0