#!/bin/bash

# deploy-backend.sh
# Automated deployment script for Security Patrol backend services to Azure

set -e  # Exit on error

# Script directory for relative paths
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
LOG_FILE="${SCRIPT_DIR}/deploy-backend.log"
VERBOSE=false

# Set up trap for cleanup on exit
trap cleanup EXIT

# Function definitions
function log() {
    local level="$1"
    local message="$2"
    local timestamp=$(date +"%Y-%m-%d %H:%M:%S")
    
    # Add color based on log level
    if [[ "$level" == "ERROR" ]]; then
        echo -e "\e[31m${timestamp} ${level}: ${message}\e[0m"  # Red for errors
    elif [[ "$level" == "WARN" ]]; then
        echo -e "\e[33m${timestamp} ${level}: ${message}\e[0m"  # Yellow for warnings
    elif [[ "$level" == "INFO" ]]; then
        echo -e "\e[32m${timestamp} ${level}: ${message}\e[0m"  # Green for info
    elif [[ "$level" == "DEBUG" ]]; then
        if [[ "$VERBOSE" == "true" ]]; then
            echo -e "\e[36m${timestamp} ${level}: ${message}\e[0m"  # Cyan for debug
        fi
    else
        echo -e "${timestamp} ${level}: ${message}"
    fi
    
    # Also log to file
    echo "${timestamp} ${level}: ${message}" >> "$LOG_FILE"
}

function check_dependencies() {
    log "INFO" "Checking required dependencies..."
    
    # Check Azure CLI
    if ! command -v az &> /dev/null; then
        log "ERROR" "Azure CLI (az) not found. Please install it: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
        return 1
    fi
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        log "ERROR" "Docker not found. Please install it: https://docs.docker.com/get-docker/"
        return 1
    fi
    
    # Check Terraform
    if ! command -v terraform &> /dev/null; then
        log "ERROR" "Terraform not found. Please install it: https://www.terraform.io/downloads.html"
        return 1
    fi
    
    # Check jq
    if ! command -v jq &> /dev/null; then
        log "ERROR" "jq not found. Please install it: https://stedolan.github.io/jq/download/"
        return 1
    fi
    
    log "INFO" "All dependencies are installed."
    return 0
}

function check_az_login() {
    log "INFO" "Checking Azure CLI login status..."
    
    # Check if logged in
    if ! az account show &> /dev/null; then
        log "INFO" "Not logged in to Azure. Initiating login process..."
        az login --use-device-code
        
        if [ $? -ne 0 ]; then
            log "ERROR" "Failed to log in to Azure."
            return 1
        fi
    fi
    
    log "INFO" "Successfully logged in to Azure."
    return 0
}

function select_subscription() {
    local subscription_id="$1"
    
    log "INFO" "Setting Azure subscription to: $subscription_id"
    az account set --subscription "$subscription_id"
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Failed to set Azure subscription."
        return 1
    fi
    
    log "INFO" "Successfully set Azure subscription."
    return 0
}

function get_terraform_output() {
    local output_name="$1"
    local terraform_dir="$2"
    
    log "DEBUG" "Getting Terraform output: $output_name from directory: $terraform_dir"
    
    pushd "$terraform_dir" > /dev/null
    local output_value
    output_value=$(terraform output -json "$output_name" | jq -r '.')
    local terraform_exit_code=$?
    popd > /dev/null
    
    if [ $terraform_exit_code -ne 0 ] || [[ -z "$output_value" || "$output_value" == "null" ]]; then
        log "ERROR" "Failed to get Terraform output: $output_name"
        return ""
    fi
    
    echo "$output_value"
}

function build_docker_image() {
    local image_name="$1"
    local image_tag="$2"
    local dockerfile_path="$3"
    local build_context="$4"
    
    log "INFO" "Building Docker image: $image_name:$image_tag"
    
    pushd "$build_context" > /dev/null
    docker build -f "$dockerfile_path" -t "$image_name:$image_tag" --build-arg ASPNETCORE_ENVIRONMENT="$ENVIRONMENT" .
    local build_status=$?
    popd > /dev/null
    
    if [ $build_status -ne 0 ]; then
        log "ERROR" "Failed to build Docker image: $image_name:$image_tag"
        return 1
    fi
    
    log "INFO" "Successfully built Docker image: $image_name:$image_tag"
    return 0
}

function push_docker_image_to_acr() {
    local image_name="$1"
    local image_tag="$2"
    local acr_name="$3"
    
    log "INFO" "Logging in to Azure Container Registry: $acr_name"
    az acr login --name "$acr_name"
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Failed to log in to Azure Container Registry: $acr_name"
        return 1
    fi
    
    local acr_login_server
    acr_login_server=$(az acr show --name "$acr_name" --query loginServer -o tsv)
    
    log "INFO" "Tagging Docker image for ACR: $acr_login_server/$image_name:$image_tag"
    docker tag "$image_name:$image_tag" "$acr_login_server/$image_name:$image_tag"
    
    log "INFO" "Pushing Docker image to ACR: $acr_login_server/$image_name:$image_tag"
    docker push "$acr_login_server/$image_name:$image_tag"
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Failed to push Docker image to ACR"
        return 1
    fi
    
    log "INFO" "Successfully pushed Docker image to ACR"
    return 0
}

function update_app_service_settings() {
    local resource_group="$1"
    local app_service_name="$2"
    local settings_json="$3"
    local slot_param=""
    
    # Check if a slot was specified (4th parameter)
    if [ ! -z "$4" ]; then
        slot_param="--slot $4"
    fi
    
    log "INFO" "Updating App Service settings for: $app_service_name"
    az webapp config appsettings set --resource-group "$resource_group" --name "$app_service_name" $slot_param --settings "$settings_json"
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Failed to update App Service settings"
        return 1
    fi
    
    log "INFO" "Successfully updated App Service settings"
    return 0
}

function update_app_service_connection_strings() {
    local resource_group="$1"
    local app_service_name="$2"
    local connection_strings_json="$3"
    local slot_param=""
    
    # Check if a slot was specified (4th parameter)
    if [ ! -z "$4" ]; then
        slot_param="--slot $4"
    fi
    
    log "INFO" "Updating App Service connection strings for: $app_service_name"
    az webapp config connection-string set --resource-group "$resource_group" --name "$app_service_name" $slot_param --connection-string-type "SQLAzure" --settings "$connection_strings_json"
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Failed to update App Service connection strings"
        return 1
    fi
    
    log "INFO" "Successfully updated App Service connection strings"
    return 0
}

function deploy_container_to_app_service() {
    local resource_group="$1"
    local app_service_name="$2"
    local container_image_uri="$3"
    local registry_url="$4"
    local registry_username="$5"
    local registry_password="$6"
    
    log "INFO" "Deploying container to App Service: $app_service_name"
    
    # Configure container settings
    az webapp config container set --resource-group "$resource_group" --name "$app_service_name" \
      --docker-custom-image-name "$container_image_uri" \
      --docker-registry-server-url "$registry_url" \
      --docker-registry-server-user "$registry_username" \
      --docker-registry-server-password "$registry_password"
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Failed to configure container settings for App Service"
        return 1
    fi
    
    # Restart the app service to apply changes
    log "INFO" "Restarting App Service to apply changes"
    az webapp restart --resource-group "$resource_group" --name "$app_service_name"
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Failed to restart App Service"
        return 1
    fi
    
    log "INFO" "Successfully deployed container to App Service: $app_service_name"
    return 0
}

function deploy_to_slot() {
    local resource_group="$1"
    local app_service_name="$2"
    local slot_name="$3"
    local container_image_uri="$4"
    local registry_url="$5"
    local registry_username="$6"
    local registry_password="$7"
    
    log "INFO" "Deploying container to App Service slot: $app_service_name-$slot_name"
    
    # Configure container settings for the slot
    az webapp config container set --resource-group "$resource_group" --name "$app_service_name" --slot "$slot_name" \
      --docker-custom-image-name "$container_image_uri" \
      --docker-registry-server-url "$registry_url" \
      --docker-registry-server-user "$registry_username" \
      --docker-registry-server-password "$registry_password"
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Failed to configure container settings for App Service slot"
        return 1
    fi
    
    # Restart the app service slot to apply changes
    log "INFO" "Restarting App Service slot to apply changes"
    az webapp restart --resource-group "$resource_group" --name "$app_service_name" --slot "$slot_name"
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Failed to restart App Service slot"
        return 1
    fi
    
    log "INFO" "Successfully deployed container to App Service slot: $app_service_name-$slot_name"
    return 0
}

function swap_deployment_slot() {
    local resource_group="$1"
    local app_service_name="$2"
    local slot_name="$3"
    
    log "INFO" "Swapping App Service deployment slot: $slot_name to production"
    az webapp deployment slot swap --resource-group "$resource_group" --name "$app_service_name" --slot "$slot_name" --target-slot production
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Failed to swap deployment slot"
        return 1
    fi
    
    log "INFO" "Successfully swapped deployment slot to production"
    return 0
}

function test_deployment() {
    local api_url="$1"
    
    log "INFO" "Testing deployment at: $api_url"
    
    # Wait for App Service to start
    log "INFO" "Waiting for App Service to start..."
    sleep 30
    
    # Check if the API health endpoint is responding
    local max_retries=5
    local retry_count=0
    local health_url="${api_url}/health"
    
    while [ $retry_count -lt $max_retries ]; do
        log "INFO" "Checking health endpoint: $health_url (Attempt $((retry_count+1))/$max_retries)"
        
        response=$(curl -s -o /dev/null -w "%{http_code}" "$health_url")
        
        if [ "$response" == "200" ]; then
            log "INFO" "Health check successful. API is responding with HTTP 200."
            return 0
        else
            log "WARN" "Health check failed. API returned HTTP $response. Retrying in 10 seconds..."
            retry_count=$((retry_count+1))
            sleep 10
        fi
    done
    
    log "ERROR" "Health check failed after $max_retries attempts."
    return 1
}

function cleanup() {
    log "INFO" "Performing cleanup operations..."
    # Remove temporary files
    log "INFO" "Cleanup completed."
}

# Main script execution starts here

# Parse script parameters
ENVIRONMENT="dev"
SUBSCRIPTION_ID=""
RESOURCE_GROUP=""
TERRAFORM_DIR="../terraform"
BUILD_ONLY=false
SKIP_BUILD=false
IMAGE_TAG="latest"
DEPLOY_TO_SLOT=false
SWAP_AFTER_DEPLOYMENT=false
VERBOSE=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    key="$1"
    case $key in
        --environment|-e)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --subscription|-s)
            SUBSCRIPTION_ID="$2"
            shift 2
            ;;
        --resource-group|-g)
            RESOURCE_GROUP="$2"
            shift 2
            ;;
        --terraform-dir|-t)
            TERRAFORM_DIR="$2"
            shift 2
            ;;
        --build-only|-b)
            BUILD_ONLY=true
            shift
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --image-tag|-i)
            IMAGE_TAG="$2"
            shift 2
            ;;
        --deploy-to-slot)
            DEPLOY_TO_SLOT=true
            shift
            ;;
        --swap-after-deployment)
            SWAP_AFTER_DEPLOYMENT=true
            shift
            ;;
        --verbose|-v)
            VERBOSE=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  --environment, -e        Target environment: dev, test, staging, prod (default: dev)"
            echo "  --subscription, -s       Azure subscription ID"
            echo "  --resource-group, -g     Azure resource group (overrides Terraform output if specified)"
            echo "  --terraform-dir, -t      Path to Terraform directory (default: ../terraform)"
            echo "  --build-only, -b         Only build the Docker image without deploying"
            echo "  --skip-build             Skip building the Docker image and use existing image"
            echo "  --image-tag, -i          Tag for the Docker image (default: latest)"
            echo "  --deploy-to-slot         Deploy to staging slot instead of production slot"
            echo "  --swap-after-deployment  Swap staging slot to production after successful deployment"
            echo "  --verbose, -v            Enable verbose logging"
            echo "  --help, -h               Show this help message"
            exit 0
            ;;
        *)
            log "ERROR" "Unknown option: $key"
            exit 1
            ;;
    esac
done

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(dev|test|staging|prod)$ ]]; then
    log "ERROR" "Invalid environment: $ENVIRONMENT. Must be one of: dev, test, staging, prod"
    exit 1
fi

# Initialize log file
echo "" > "$LOG_FILE"
log "INFO" "Starting deployment for environment: $ENVIRONMENT"

# Check dependencies
if ! check_dependencies; then
    log "ERROR" "Missing required dependencies. Please install them and try again."
    exit 1
fi

# Verify Azure login
if ! check_az_login; then
    log "ERROR" "Failed to log in to Azure. Please try logging in manually with 'az login'."
    exit 1
fi

# Select Azure subscription if provided
if [[ -n "$SUBSCRIPTION_ID" ]]; then
    if ! select_subscription "$SUBSCRIPTION_ID"; then
        log "ERROR" "Failed to set Azure subscription: $SUBSCRIPTION_ID"
        exit 1
    fi
fi

# Get resource information from Terraform
log "INFO" "Reading Terraform outputs from: $TERRAFORM_DIR"

# Convert relative path to absolute path if needed
if [[ ! "$TERRAFORM_DIR" = /* ]]; then
    TERRAFORM_DIR="$SCRIPT_DIR/$TERRAFORM_DIR"
fi

if [ ! -d "$TERRAFORM_DIR" ]; then
    log "ERROR" "Terraform directory not found: $TERRAFORM_DIR"
    exit 1
fi

if [ -z "$RESOURCE_GROUP" ]; then
    RESOURCE_GROUP=$(get_terraform_output "resource_group_name" "$TERRAFORM_DIR")
    if [ -z "$RESOURCE_GROUP" ]; then
        log "ERROR" "Failed to get resource group name from Terraform output"
        exit 1
    fi
fi

APP_SERVICE_NAME=$(get_terraform_output "app_service_name" "$TERRAFORM_DIR")
if [ -z "$APP_SERVICE_NAME" ]; then
    log "ERROR" "Failed to get App Service name from Terraform output"
    exit 1
fi

DATABASE_CONNECTION_STRING=$(get_terraform_output "database_connection_string" "$TERRAFORM_DIR")
STORAGE_ACCOUNT_NAME=$(get_terraform_output "storage_account_name" "$TERRAFORM_DIR")
APP_INSIGHTS_CONNECTION_STRING=$(get_terraform_output "app_insights_connection_string" "$TERRAFORM_DIR")

# Set image and registry information
IMAGE_NAME="security-patrol-api"
ACR_NAME="securitypatrolacr$ENVIRONMENT"
ACR_LOGIN_SERVER="$ACR_NAME.azurecr.io"
FULL_IMAGE_NAME="$ACR_LOGIN_SERVER/$IMAGE_NAME:$IMAGE_TAG"

# Build Docker image if not skipped
if [ "$SKIP_BUILD" != "true" ]; then
    DOCKERFILE_PATH="../../src/backend/Dockerfile"
    BUILD_CONTEXT="../../src/backend"
    
    if ! build_docker_image "$IMAGE_NAME" "$IMAGE_TAG" "$DOCKERFILE_PATH" "$BUILD_CONTEXT"; then
        log "ERROR" "Docker image build failed"
        exit 1
    fi
    
    # Push to ACR if not build-only
    if [ "$BUILD_ONLY" != "true" ]; then
        if ! push_docker_image_to_acr "$IMAGE_NAME" "$IMAGE_TAG" "$ACR_NAME"; then
            log "ERROR" "Docker image push to ACR failed"
            exit 1
        fi
    fi
fi

# Exit if build-only is specified
if [ "$BUILD_ONLY" == "true" ]; then
    log "INFO" "Build-only specified, skipping deployment"
    exit 0
fi

# Prepare App Service settings
APP_SETTINGS='{
    "ASPNETCORE_ENVIRONMENT": "'$ENVIRONMENT'",
    "APPINSIGHTS_INSTRUMENTATIONKEY": "'$APP_INSIGHTS_CONNECTION_STRING'",
    "WEBSITE_TIME_ZONE": "UTC"
}'

CONNECTION_STRINGS='{
    "DefaultConnection": {
        "value": "'$DATABASE_CONNECTION_STRING'",
        "type": "SQLAzure"
    }
}'

# Get ACR credentials
ACR_USERNAME=$(az acr credential show --name "$ACR_NAME" --query "username" -o tsv)
ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --query "passwords[0].value" -o tsv)

# Deploy to App Service
if [ "$DEPLOY_TO_SLOT" == "true" ]; then
    SLOT_NAME="staging"
    
    if ! deploy_to_slot "$RESOURCE_GROUP" "$APP_SERVICE_NAME" "$SLOT_NAME" "$FULL_IMAGE_NAME" "$ACR_LOGIN_SERVER" "$ACR_USERNAME" "$ACR_PASSWORD"; then
        log "ERROR" "Deployment to slot failed"
        exit 1
    fi
    
    # Update slot settings
    if ! update_app_service_settings "$RESOURCE_GROUP" "$APP_SERVICE_NAME" "$APP_SETTINGS" "$SLOT_NAME"; then
        log "ERROR" "Failed to update slot settings"
        exit 1
    fi
    
    if ! update_app_service_connection_strings "$RESOURCE_GROUP" "$APP_SERVICE_NAME" "$CONNECTION_STRINGS" "$SLOT_NAME"; then
        log "ERROR" "Failed to update slot connection strings"
        exit 1
    fi
    
    # Test the deployment
    SLOT_URL="https://$APP_SERVICE_NAME-$SLOT_NAME.azurewebsites.net"
    if ! test_deployment "$SLOT_URL"; then
        log "ERROR" "Deployment testing failed"
        exit 1
    fi
    
    # Swap slots if requested
    if [ "$SWAP_AFTER_DEPLOYMENT" == "true" ]; then
        if ! swap_deployment_slot "$RESOURCE_GROUP" "$APP_SERVICE_NAME" "$SLOT_NAME"; then
            log "ERROR" "Slot swap failed"
            exit 1
        fi
    fi
else
    # Deploy directly to production
    if ! deploy_container_to_app_service "$RESOURCE_GROUP" "$APP_SERVICE_NAME" "$FULL_IMAGE_NAME" "$ACR_LOGIN_SERVER" "$ACR_USERNAME" "$ACR_PASSWORD"; then
        log "ERROR" "Deployment to App Service failed"
        exit 1
    fi
    
    # Update App Service settings
    if ! update_app_service_settings "$RESOURCE_GROUP" "$APP_SERVICE_NAME" "$APP_SETTINGS"; then
        log "ERROR" "Failed to update App Service settings"
        exit 1
    fi
    
    if ! update_app_service_connection_strings "$RESOURCE_GROUP" "$APP_SERVICE_NAME" "$CONNECTION_STRINGS"; then
        log "ERROR" "Failed to update App Service connection strings"
        exit 1
    fi
    
    # Test the deployment
    APP_URL="https://$APP_SERVICE_NAME.azurewebsites.net"
    if ! test_deployment "$APP_URL"; then
        log "ERROR" "Deployment testing failed"
        exit 1
    fi
fi

log "INFO" "Deployment completed successfully!"
exit 0