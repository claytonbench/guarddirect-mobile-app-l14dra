#!/bin/bash
# provision-infrastructure.sh
#
# This script automates the provisioning of Azure infrastructure resources 
# for the Security Patrol Application using Terraform.
#
# The script handles:
# - Azure authentication
# - Environment selection (dev, staging, prod)
# - Terraform initialization, planning, and application
# - Output management for subsequent deployment steps
#
# Dependencies:
# - Azure CLI (az) - latest
# - Terraform - latest
# - jq - latest

# Script directory and logging setup
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
LOG_FILE="${SCRIPT_DIR}/provision-infrastructure.log"
VERBOSE=false

# Default values
ENVIRONMENT="dev"
SUBSCRIPTION_ID=""
TERRAFORM_DIR="${SCRIPT_DIR}/../terraform"
PLAN_ONLY=false
DESTROY=false
AUTO_APPROVE=false
OUTPUT_FILE="terraform-outputs.json"
ADDITIONAL_VARS=""

# Colors for console output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Trap for script interruption
trap 'log "ERROR" "Script execution interrupted."; cleanup; exit 1' INT TERM

# Function to write log messages
log() {
    local level="$1"
    local message="$2"
    local timestamp=$(date +"%Y-%m-%d %H:%M:%S")
    
    # Determine color based on level
    local color=""
    case "$level" in
        "INFO") color="${BLUE}" ;;
        "SUCCESS") color="${GREEN}" ;;
        "WARNING") color="${YELLOW}" ;;
        "ERROR") color="${RED}" ;;
        *) color="${NC}" ;;
    esac
    
    # Print to console with color
    echo -e "${color}[${timestamp}] [${level}] ${message}${NC}"
    
    # Append to log file
    echo "[${timestamp}] [${level}] ${message}" >> "${LOG_FILE}"
    
    # Additional verbose output if enabled
    if [ "$VERBOSE" = true ] && [ "$level" = "INFO" ]; then
        echo -e "${BLUE}[VERBOSE] $message${NC}"
    fi
}

# Function to check if dependencies are installed
check_dependencies() {
    log "INFO" "Checking dependencies..."
    local missing_deps=0
    
    # Check for Azure CLI
    if ! command -v az &> /dev/null; then
        log "ERROR" "Azure CLI is not installed. Please install it: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
        missing_deps=1
    fi
    
    # Check for Terraform
    if ! command -v terraform &> /dev/null; then
        log "ERROR" "Terraform is not installed. Please install it: https://www.terraform.io/downloads.html"
        missing_deps=1
    fi
    
    # Check for jq
    if ! command -v jq &> /dev/null; then
        log "ERROR" "jq is not installed. Please install it: https://stedolan.github.io/jq/download/"
        missing_deps=1
    fi
    
    if [ $missing_deps -eq 0 ]; then
        log "SUCCESS" "All dependencies are installed."
    fi
    
    return $missing_deps
}

# Function to check if the user is logged into Azure
check_az_login() {
    log "INFO" "Checking Azure CLI login status..."
    
    # Try to get account info to check if logged in
    if ! az account show &> /dev/null; then
        log "WARNING" "Not logged into Azure. Initiating login process..."
        if ! az login &> /dev/null; then
            log "ERROR" "Failed to login to Azure."
            return 1
        fi
        log "SUCCESS" "Successfully logged into Azure."
    else
        log "INFO" "Already logged into Azure."
    fi
    
    return 0
}

# Function to select Azure subscription
select_subscription() {
    local subscription_id="$1"
    
    if [ -z "$subscription_id" ]; then
        log "ERROR" "No subscription ID provided."
        return 1
    fi
    
    log "INFO" "Selecting Azure subscription: $subscription_id"
    
    if ! az account set --subscription "$subscription_id" &> /dev/null; then
        log "ERROR" "Failed to select subscription: $subscription_id"
        return 1
    fi
    
    log "SUCCESS" "Successfully selected subscription: $subscription_id"
    return 0
}

# Function to initialize Terraform
initialize_terraform() {
    local terraform_dir="$1"
    
    log "INFO" "Initializing Terraform in directory: $terraform_dir"
    
    # Navigate to Terraform directory
    pushd "$terraform_dir" > /dev/null || {
        log "ERROR" "Failed to navigate to Terraform directory: $terraform_dir"
        return 1
    }
    
    # Initialize Terraform
    if ! terraform init; then
        log "ERROR" "Failed to initialize Terraform."
        popd > /dev/null
        return 1
    fi
    
    log "SUCCESS" "Terraform initialized successfully."
    popd > /dev/null
    return 0
}

# Function to create a Terraform plan
create_terraform_plan() {
    local terraform_dir="$1"
    local environment="$2"
    local plan_file="$3"
    local additional_vars="$4"
    
    log "INFO" "Creating Terraform plan for environment: $environment"
    
    # Navigate to Terraform directory
    pushd "$terraform_dir" > /dev/null || {
        log "ERROR" "Failed to navigate to Terraform directory: $terraform_dir"
        return 1
    }
    
    # Build the plan command
    local plan_cmd="terraform plan -var-file=environments/${environment}.tfvars -out=${plan_file}"
    
    # Add additional variables if provided
    if [ -n "$additional_vars" ]; then
        plan_cmd="$plan_cmd $additional_vars"
    fi
    
    log "INFO" "Executing: $plan_cmd"
    
    # Execute terraform plan
    if ! eval "$plan_cmd"; then
        log "ERROR" "Failed to create Terraform plan."
        popd > /dev/null
        return 1
    fi
    
    log "SUCCESS" "Terraform plan created successfully: $plan_file"
    popd > /dev/null
    return 0
}

# Function to apply a Terraform plan
apply_terraform_plan() {
    local terraform_dir="$1"
    local plan_file="$2"
    local auto_approve="$3"
    
    log "INFO" "Applying Terraform plan: $plan_file"
    
    # Navigate to Terraform directory
    pushd "$terraform_dir" > /dev/null || {
        log "ERROR" "Failed to navigate to Terraform directory: $terraform_dir"
        return 1
    }
    
    # Build the apply command
    local apply_cmd="terraform apply"
    
    # Add auto-approve flag if specified
    if [ "$auto_approve" = true ]; then
        apply_cmd="$apply_cmd -auto-approve"
    fi
    
    # Add plan file
    apply_cmd="$apply_cmd $plan_file"
    
    log "INFO" "Executing: $apply_cmd"
    
    # Execute terraform apply
    if ! eval "$apply_cmd"; then
        log "ERROR" "Failed to apply Terraform plan."
        popd > /dev/null
        return 1
    fi
    
    log "SUCCESS" "Terraform plan applied successfully."
    popd > /dev/null
    return 0
}

# Function to destroy Terraform-managed infrastructure
destroy_terraform_infrastructure() {
    local terraform_dir="$1"
    local environment="$2"
    local auto_approve="$3"
    
    log "INFO" "Destroying Terraform-managed infrastructure for environment: $environment"
    
    # Navigate to Terraform directory
    pushd "$terraform_dir" > /dev/null || {
        log "ERROR" "Failed to navigate to Terraform directory: $terraform_dir"
        return 1
    }
    
    # Build the destroy command
    local destroy_cmd="terraform destroy -var-file=environments/${environment}.tfvars"
    
    # Add auto-approve flag if specified
    if [ "$auto_approve" = true ]; then
        destroy_cmd="$destroy_cmd -auto-approve"
    fi
    
    log "INFO" "Executing: $destroy_cmd"
    
    # Execute terraform destroy
    if ! eval "$destroy_cmd"; then
        log "ERROR" "Failed to destroy Terraform-managed infrastructure."
        popd > /dev/null
        return 1
    fi
    
    log "SUCCESS" "Terraform-managed infrastructure destroyed successfully."
    popd > /dev/null
    return 0
}

# Function to get a specific Terraform output
get_terraform_output() {
    local terraform_dir="$1"
    local output_name="$2"
    
    log "INFO" "Getting Terraform output: $output_name"
    
    # Navigate to Terraform directory
    pushd "$terraform_dir" > /dev/null || {
        log "ERROR" "Failed to navigate to Terraform directory: $terraform_dir"
        return ""
    }
    
    # Get the output
    local output_value
    output_value=$(terraform output -json "$output_name" | jq -r '.')
    
    popd > /dev/null
    
    echo "$output_value"
}

# Function to export all Terraform outputs to a file
export_terraform_outputs() {
    local terraform_dir="$1"
    local output_file="$2"
    
    log "INFO" "Exporting Terraform outputs to: $output_file"
    
    # Navigate to Terraform directory
    pushd "$terraform_dir" > /dev/null || {
        log "ERROR" "Failed to navigate to Terraform directory: $terraform_dir"
        return 1
    }
    
    # Export all outputs to JSON file
    if ! terraform output -json > "$output_file"; then
        log "ERROR" "Failed to export Terraform outputs."
        popd > /dev/null
        return 1
    fi
    
    log "SUCCESS" "Terraform outputs exported successfully to: $output_file"
    popd > /dev/null
    return 0
}

# Function to perform cleanup operations
cleanup() {
    log "INFO" "Performing cleanup operations..."
    # Remove temporary files
    rm -f "${TERRAFORM_DIR}/terraform-plan-${ENVIRONMENT}.tfplan" 2>/dev/null
    log "INFO" "Cleanup completed."
}

# Function to display script usage
show_usage() {
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  -e, --environment ENV      Target environment (dev, staging, prod) (default: dev)"
    echo "  -s, --subscription-id ID   Azure subscription ID"
    echo "  -t, --terraform-dir DIR    Path to Terraform directory (default: ../terraform)"
    echo "  -p, --plan-only            Only create Terraform plan without applying changes"
    echo "  -d, --destroy              Destroy the Terraform-managed infrastructure"
    echo "  -a, --auto-approve         Auto-approve Terraform plan application without confirmation"
    echo "  -o, --output-file FILE     File to export Terraform outputs to (default: terraform-outputs.json)"
    echo "  --vars 'VAR=VAL...'        Additional Terraform variables to pass to the plan command"
    echo "  -v, --verbose              Enable verbose logging"
    echo "  -h, --help                 Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 --environment dev --subscription-id xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
    echo "  $0 --destroy --environment staging --auto-approve"
}

# Parse command-line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            -e|--environment)
                ENVIRONMENT="$2"
                shift 2
                ;;
            -s|--subscription-id)
                SUBSCRIPTION_ID="$2"
                shift 2
                ;;
            -t|--terraform-dir)
                TERRAFORM_DIR="$2"
                shift 2
                ;;
            -p|--plan-only)
                PLAN_ONLY=true
                shift
                ;;
            -d|--destroy)
                DESTROY=true
                shift
                ;;
            -a|--auto-approve)
                AUTO_APPROVE=true
                shift
                ;;
            -o|--output-file)
                OUTPUT_FILE="$2"
                shift 2
                ;;
            --vars)
                ADDITIONAL_VARS="$2"
                shift 2
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            -h|--help)
                show_usage
                exit 0
                ;;
            *)
                log "ERROR" "Unknown option: $1"
                show_usage
                exit 1
                ;;
        esac
    done
    
    # Validate environment
    if [[ ! "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]]; then
        log "ERROR" "Invalid environment: $ENVIRONMENT. Must be one of: dev, staging, prod"
        exit 1
    fi
    
    # Validate subscription ID if provided
    if [ -n "$SUBSCRIPTION_ID" ]; then
        if ! [[ "$SUBSCRIPTION_ID" =~ ^[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}$ ]]; then
            log "ERROR" "Invalid subscription ID format: $SUBSCRIPTION_ID"
            exit 1
        fi
    fi
    
    # Validate terraform directory exists
    if [ ! -d "$TERRAFORM_DIR" ]; then
        log "ERROR" "Terraform directory does not exist: $TERRAFORM_DIR"
        exit 1
    fi
}

# Main execution flow
main() {
    # Initialize log file
    echo "# Terraform Provisioning Log - $(date +"%Y-%m-%d %H:%M:%S")" > "${LOG_FILE}"
    
    log "INFO" "Starting infrastructure provisioning for environment: $ENVIRONMENT"
    log "INFO" "Configuration:"
    log "INFO" "  Environment: $ENVIRONMENT"
    log "INFO" "  Terraform Directory: $TERRAFORM_DIR"
    log "INFO" "  Plan Only: $PLAN_ONLY"
    log "INFO" "  Destroy: $DESTROY"
    log "INFO" "  Auto Approve: $AUTO_APPROVE"
    log "INFO" "  Output File: $OUTPUT_FILE"
    
    # Check dependencies
    check_dependencies || {
        log "ERROR" "Missing dependencies. Please install required tools."
        exit 1
    }
    
    # Check Azure login
    check_az_login || {
        log "ERROR" "Failed to authenticate with Azure."
        exit 1
    }
    
    # Select subscription if provided
    if [ -n "$SUBSCRIPTION_ID" ]; then
        select_subscription "$SUBSCRIPTION_ID" || {
            log "ERROR" "Failed to select Azure subscription."
            exit 1
        }
    fi
    
    # Initialize Terraform
    initialize_terraform "$TERRAFORM_DIR" || {
        log "ERROR" "Failed to initialize Terraform."
        exit 1
    }
    
    # Handle destroy operation if requested
    if [ "$DESTROY" = true ]; then
        log "WARNING" "Destroying infrastructure for environment: $ENVIRONMENT"
        
        # Confirm destruction if not auto-approved
        if [ "$AUTO_APPROVE" != true ]; then
            echo -e "${RED}WARNING: You are about to destroy all resources in the $ENVIRONMENT environment.${NC}"
            echo -e "${RED}This action cannot be undone.${NC}"
            read -p "Are you sure you want to continue? (yes/no): " confirm
            if [[ "$confirm" != "yes" ]]; then
                log "INFO" "Destruction cancelled by user."
                exit 0
            fi
        fi
        
        destroy_terraform_infrastructure "$TERRAFORM_DIR" "$ENVIRONMENT" "$AUTO_APPROVE" || {
            log "ERROR" "Failed to destroy infrastructure."
            exit 1
        }
        
        log "SUCCESS" "Infrastructure destroyed successfully."
        exit 0
    fi
    
    # Create Terraform plan
    local plan_file="terraform-plan-${ENVIRONMENT}.tfplan"
    create_terraform_plan "$TERRAFORM_DIR" "$ENVIRONMENT" "$plan_file" "$ADDITIONAL_VARS" || {
        log "ERROR" "Failed to create Terraform plan."
        exit 1
    }
    
    # Exit if plan only
    if [ "$PLAN_ONLY" = true ]; then
        log "INFO" "Plan-only mode. Exiting without applying changes."
        exit 0
    fi
    
    # Apply Terraform plan
    apply_terraform_plan "$TERRAFORM_DIR" "$plan_file" "$AUTO_APPROVE" || {
        log "ERROR" "Failed to apply Terraform plan."
        exit 1
    }
    
    # Export Terraform outputs
    export_terraform_outputs "$TERRAFORM_DIR" "$OUTPUT_FILE" || {
        log "ERROR" "Failed to export Terraform outputs."
        exit 1
    }
    
    # Clean up
    cleanup
    
    log "SUCCESS" "Infrastructure provisioning completed successfully."
    log "INFO" "Terraform outputs exported to: $OUTPUT_FILE"
    
    # Display key outputs for quick reference
    log "INFO" "Key outputs:"
    local app_service_url=$(get_terraform_output "$TERRAFORM_DIR" "app_service_url")
    local sql_server_name=$(get_terraform_output "$TERRAFORM_DIR" "sql_server_name")
    local storage_account_name=$(get_terraform_output "$TERRAFORM_DIR" "storage_account_name")
    
    if [ -n "$app_service_url" ]; then
        log "INFO" "App Service URL: $app_service_url"
    fi
    
    if [ -n "$sql_server_name" ]; then
        log "INFO" "SQL Server: $sql_server_name"
    fi
    
    if [ -n "$storage_account_name" ]; then
        log "INFO" "Storage Account: $storage_account_name"
    fi
    
    exit 0
}

# Execute script with argument parsing
parse_args "$@"
main