#!/bin/bash
# backup-database.sh - Automated backup script for Security Patrol SQL Databases
# 
# This script automates the backup process for Azure SQL Databases used by
# the Security Patrol Application, with support for disaster recovery procedures.
#
# Dependencies:
# - Azure CLI (az)
# - jq (for JSON parsing)

set -e  # Exit immediately if a command exits with a non-zero status

# Script constants and defaults
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
LOG_FILE="./backup-database.log"
TIMESTAMP=$(date "+%Y%m%d%H%M%S")
ENVIRONMENT="dev"
# The following variables will be populated from terraform outputs or command-line arguments
RESOURCE_GROUP_NAME=""
SQL_SERVER_NAME=""
SQL_DATABASE_NAME=""
STORAGE_ACCOUNT_NAME=""
STORAGE_CONTAINER_NAME="backups"
BACKUP_NAME=""

# Functions

# Logging function with severity levels
# Usage: log_message "Message text" "level"
log_message() {
    local message="$1"
    local level="${2:-INFO}"
    local timestamp=$(date "+%Y-%m-%d %H:%M:%S")
    
    # Format the message
    local formatted_message="[$timestamp] [$level] $message"
    
    # Define colors for console output
    local RED='\033[0;31m'
    local YELLOW='\033[0;33m'
    local GREEN='\033[0;32m'
    local BLUE='\033[0;34m'
    local NC='\033[0m' # No Color
    
    # Print to console with appropriate color
    case "$level" in
        "ERROR")
            echo -e "${RED}$formatted_message${NC}" >&2
            ;;
        "WARNING")
            echo -e "${YELLOW}$formatted_message${NC}" >&2
            ;;
        "SUCCESS")
            echo -e "${GREEN}$formatted_message${NC}"
            ;;
        "INFO")
            echo -e "${BLUE}$formatted_message${NC}"
            ;;
        *)
            echo "$formatted_message"
            ;;
    esac
    
    # Append to log file
    echo "$formatted_message" >> "$LOG_FILE"
}

# Check if required dependencies are installed
check_dependencies() {
    log_message "Checking dependencies..." "INFO"
    
    # Check if az CLI is installed
    if ! command -v az &> /dev/null; then
        log_message "Azure CLI is not installed. Please install it and try again." "ERROR"
        return 1
    fi
    
    # Check if jq is installed
    if ! command -v jq &> /dev/null; then
        log_message "jq is not installed. Please install it and try again." "ERROR"
        return 1
    fi
    
    log_message "All dependencies are installed." "SUCCESS"
    return 0
}

# Check if logged into Azure, prompt for login if needed
check_az_login() {
    log_message "Checking Azure login status..." "INFO"
    
    # Try to get the current subscription
    if ! az account show &> /dev/null; then
        log_message "Not logged in to Azure. Initiating login..." "WARNING"
        if ! az login --use-device-code; then
            log_message "Failed to log in to Azure." "ERROR"
            return 1
        fi
    fi
    
    log_message "Successfully logged in to Azure." "SUCCESS"
    return 0
}

# Set Azure subscription
set_subscription() {
    local subscription_id="$1"
    
    if [ -z "$subscription_id" ]; then
        log_message "No subscription ID provided, using default." "INFO"
        return 0
    fi
    
    log_message "Setting Azure subscription to: $subscription_id" "INFO"
    
    if ! az account set --subscription "$subscription_id"; then
        log_message "Failed to set Azure subscription." "ERROR"
        return 1
    fi
    
    log_message "Azure subscription set successfully." "SUCCESS"
    return 0
}

# Get outputs from Terraform
get_terraform_output() {
    local output_name="$1"
    local terraform_dir="$2"
    
    log_message "Retrieving Terraform output: $output_name" "INFO"
    
    # Navigate to Terraform directory
    pushd "$terraform_dir" > /dev/null || return 1
    
    # Get the output value
    local output_value
    output_value=$(terraform output -raw "$output_name" 2>/dev/null)
    local exit_code=$?
    
    # Return to original directory
    popd > /dev/null || return 1
    
    if [ $exit_code -ne 0 ]; then
        log_message "Failed to get Terraform output: $output_name" "ERROR"
        return 1
    fi
    
    echo "$output_value"
    return 0
}

# Create a new backup of an Azure SQL Database
create_sql_database_backup() {
    local resource_group="$1"
    local server_name="$2"
    local database_name="$3"
    local backup_name="$4"
    
    log_message "Creating backup of database: $database_name on server: $server_name" "INFO"
    
    # Create a backup request
    local result
    result=$(az sql db create-backup --resource-group "$resource_group" \
        --server "$server_name" \
        --database "$database_name" \
        --backup-name "$backup_name" \
        --output json)
    
    if [ $? -ne 0 ]; then
        log_message "Failed to create backup of database: $database_name" "ERROR"
        return 1
    fi
    
    # Get the backup operation ID
    local operation_id
    operation_id=$(echo "$result" | jq -r '.operationId')
    
    log_message "Backup operation started with ID: $operation_id" "INFO"
    
    # Monitor the backup operation
    local status="InProgress"
    while [ "$status" == "InProgress" ]; do
        log_message "Waiting for backup to complete..." "INFO"
        sleep 30
        
        # Check the status of the operation
        local status_result
        status_result=$(az sql db operation show --resource-group "$resource_group" \
            --server "$server_name" \
            --database "$database_name" \
            --operation-id "$operation_id" \
            --output json)
        
        status=$(echo "$status_result" | jq -r '.status')
    done
    
    if [ "$status" == "Succeeded" ]; then
        log_message "Backup completed successfully: $backup_name" "SUCCESS"
        return 0
    else
        log_message "Backup failed with status: $status" "ERROR"
        return 1
    fi
}

# Export a SQL database backup to Azure Storage
export_sql_database_backup() {
    local resource_group="$1"
    local server_name="$2"
    local database_name="$3"
    local storage_account="$4"
    local container_name="$5"
    
    log_message "Exporting database backup to storage: $database_name to $storage_account/$container_name" "INFO"
    
    # Generate a SAS token for the storage container (valid for 1 hour)
    local sas_token
    sas_token=$(az storage container generate-sas \
        --name "$container_name" \
        --account-name "$storage_account" \
        --permissions rwl \
        --expiry $(date -u -d "1 hour" '+%Y-%m-%dT%H:%MZ') \
        --output tsv)
    
    if [ $? -ne 0 ]; then
        log_message "Failed to generate SAS token for storage container" "ERROR"
        return 1
    fi
    
    # Export the database backup
    local bacpac_name="${database_name}_${TIMESTAMP}.bacpac"
    local export_result
    export_result=$(az sql db export \
        --resource-group "$resource_group" \
        --server "$server_name" \
        --name "$database_name" \
        --storage-key "$sas_token" \
        --storage-key-type "SharedAccessKey" \
        --storage-uri "https://${storage_account}.blob.core.windows.net/${container_name}/${bacpac_name}" \
        --output json)
    
    if [ $? -ne 0 ]; then
        log_message "Failed to initiate database export" "ERROR"
        return 1
    fi
    
    # Get the export operation ID
    local operation_id
    operation_id=$(echo "$export_result" | jq -r '.operationId')
    
    log_message "Export operation started with ID: $operation_id" "INFO"
    
    # Monitor the export operation
    local status="InProgress"
    while [ "$status" == "InProgress" ]; do
        log_message "Waiting for export to complete..." "INFO"
        sleep 30
        
        # Check the status of the operation
        local status_result
        status_result=$(az sql db operation show --resource-group "$resource_group" \
            --server "$server_name" \
            --database "$database_name" \
            --operation-id "$operation_id" \
            --output json)
        
        status=$(echo "$status_result" | jq -r '.status')
    done
    
    if [ "$status" == "Succeeded" ]; then
        log_message "Export completed successfully: $bacpac_name" "SUCCESS"
        return 0
    else
        log_message "Export failed with status: $status" "ERROR"
        return 1
    fi
}

# List available backups for a SQL database
list_sql_database_backups() {
    local resource_group="$1"
    local server_name="$2"
    local database_name="$3"
    
    log_message "Listing available backups for database: $database_name" "INFO"
    
    # Get the list of restore points
    local result
    result=$(az sql db restore-point list \
        --resource-group "$resource_group" \
        --server "$server_name" \
        --database "$database_name" \
        --output json)
    
    if [ $? -ne 0 ]; then
        log_message "Failed to list restore points for database: $database_name" "ERROR"
        return 1
    fi
    
    echo "$result"
    return 0
}

# Remove backups older than the specified retention period
remove_old_backups() {
    local storage_account="$1"
    local container_name="$2"
    local retention_days="$3"
    
    log_message "Removing backups older than $retention_days days from $storage_account/$container_name" "INFO"
    
    # Get the list of backup files
    local backups
    backups=$(az storage blob list \
        --account-name "$storage_account" \
        --container-name "$container_name" \
        --query "[?contains(name, '.bacpac')]" \
        --output json)
    
    if [ $? -ne 0 ]; then
        log_message "Failed to list backup files in storage" "ERROR"
        return 1
    fi
    
    # Calculate the cutoff date
    local cutoff_date
    cutoff_date=$(date -d "$retention_days days ago" +%s)
    
    # Count of removed backups
    local removed_count=0
    
    # Filter backups older than the retention period and delete them
    for backup in $(echo "$backups" | jq -r '.[] | @base64'); do
        local name
        local last_modified
        name=$(echo "$backup" | base64 --decode | jq -r '.name')
        last_modified=$(echo "$backup" | base64 --decode | jq -r '.properties.lastModified')
        
        # Convert lastModified to seconds since epoch
        local backup_date
        backup_date=$(date -d "$last_modified" +%s)
        
        if [ "$backup_date" -lt "$cutoff_date" ]; then
            log_message "Removing old backup: $name (last modified: $last_modified)" "INFO"
            
            # Delete the backup
            if az storage blob delete \
                --account-name "$storage_account" \
                --container-name "$container_name" \
                --name "$name" \
                --output none; then
                ((removed_count++))
            else
                log_message "Failed to delete backup: $name" "WARNING"
            fi
        fi
    done
    
    log_message "Removed $removed_count old backup(s)" "SUCCESS"
    return $removed_count
}

# Test the integrity of a database backup
test_backup_integrity() {
    local storage_account="$1"
    local container_name="$2"
    local backup_file="$3"
    
    log_message "Testing integrity of backup: $backup_file" "INFO"
    
    # Download the backup metadata (without downloading the entire file)
    local metadata
    metadata=$(az storage blob show \
        --account-name "$storage_account" \
        --container-name "$container_name" \
        --name "$backup_file" \
        --output json)
    
    if [ $? -ne 0 ]; then
        log_message "Failed to get metadata for backup: $backup_file" "ERROR"
        return 1
    fi
    
    # Check if the file exists and has content
    local content_length
    content_length=$(echo "$metadata" | jq -r '.properties.contentLength')
    
    if [ -z "$content_length" ] || [ "$content_length" -eq 0 ]; then
        log_message "Backup file has zero size: $backup_file" "ERROR"
        return 1
    fi
    
    # Check if the file has the correct content type
    local content_type
    content_type=$(echo "$metadata" | jq -r '.properties.contentType')
    
    if [ "$content_type" != "application/octet-stream" ]; then
        log_message "Backup file has incorrect content type: $content_type" "WARNING"
    fi
    
    log_message "Backup integrity check passed: $backup_file" "SUCCESS"
    return 0
}

# Show script usage information
show_usage() {
    echo "Usage: backup-database.sh [OPTIONS]"
    echo
    echo "Automates the backup process for Azure SQL Databases used by the Security Patrol Application"
    echo
    echo "Options:"
    echo "  -e, --environment       Target environment (dev, test, staging, prod) [default: dev]"
    echo "  -s, --subscription-id   Azure subscription ID"
    echo "  -g, --resource-group    Azure resource group name (overrides Terraform output if specified)"
    echo "  --server-name           SQL Server name (overrides Terraform output if specified)"
    echo "  --database-name         SQL Database name (overrides Terraform output if specified)"
    echo "  -t, --terraform-dir     Path to Terraform directory containing state [default: ../terraform]"
    echo "  --backup-type           Type of backup to perform (Full, Differential, Log) [default: Full]"
    echo "  -x, --export            Export backup to Azure Storage"
    echo "  --storage-account       Storage account name for backup export (overrides Terraform output if specified)"
    echo "  --container-name        Storage container name for backup export [default: backups]"
    echo "  -r, --retention-days    Number of days to retain backups [default: 30]"
    echo "  --test-integrity        Test backup integrity after creation"
    echo "  -l, --log-file          Path to log file [default: ./backup-database.log]"
    echo "  -v, --verbose           Enable verbose logging"
    echo "  -h, --help              Display this help message"
    echo
    echo "Examples:"
    echo "  backup-database.sh -e prod -x --retention-days 90 --test-integrity"
    echo "  backup-database.sh --resource-group my-rg --server-name my-server --database-name my-db -x"
}

# Perform cleanup operations before script exit
cleanup() {
    log_message "Cleaning up resources..." "INFO"
    
    # Remove any temporary files created during execution
    
    log_message "Script execution completed" "INFO"
}

# Main script execution

# Set up trap to ensure cleanup on script exit
trap cleanup EXIT

# Parse command line arguments
TERRAFORM_DIR="../terraform"
BACKUP_TYPE="Full"
EXPORT_BACKUP=false
RETENTION_DAYS=30
TEST_INTEGRITY=false
VERBOSE=false
SUBSCRIPTION_ID=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -s|--subscription-id)
            SUBSCRIPTION_ID="$2"
            shift 2
            ;;
        -g|--resource-group)
            RESOURCE_GROUP_NAME="$2"
            shift 2
            ;;
        --server-name)
            SQL_SERVER_NAME="$2"
            shift 2
            ;;
        --database-name)
            SQL_DATABASE_NAME="$2"
            shift 2
            ;;
        -t|--terraform-dir)
            TERRAFORM_DIR="$2"
            shift 2
            ;;
        --backup-type)
            BACKUP_TYPE="$2"
            shift 2
            ;;
        -x|--export)
            EXPORT_BACKUP=true
            shift
            ;;
        --storage-account)
            STORAGE_ACCOUNT_NAME="$2"
            shift 2
            ;;
        --container-name)
            STORAGE_CONTAINER_NAME="$2"
            shift 2
            ;;
        -r|--retention-days)
            RETENTION_DAYS="$2"
            shift 2
            ;;
        --test-integrity)
            TEST_INTEGRITY=true
            shift
            ;;
        -l|--log-file)
            LOG_FILE="$2"
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
            echo "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Initialize logging
log_message "=== Starting SQL Database Backup Script ===" "INFO"
log_message "Environment: $ENVIRONMENT" "INFO"

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(dev|test|staging|prod)$ ]]; then
    log_message "Invalid environment specified: $ENVIRONMENT. Must be one of: dev, test, staging, prod" "ERROR"
    exit 1
fi

# Check dependencies
check_dependencies || exit 1

# Check Azure login
check_az_login || exit 1

# Set subscription if provided
if [ -n "$SUBSCRIPTION_ID" ]; then
    set_subscription "$SUBSCRIPTION_ID" || exit 1
fi

# Get resource information from Terraform if not provided via command line
if [ -z "$RESOURCE_GROUP_NAME" ]; then
    RESOURCE_GROUP_NAME=$(get_terraform_output "resource_group_name" "$TERRAFORM_DIR")
    if [ $? -ne 0 ] || [ -z "$RESOURCE_GROUP_NAME" ]; then
        log_message "Failed to get resource group name from Terraform output" "ERROR"
        exit 1
    fi
    log_message "Using resource group from Terraform: $RESOURCE_GROUP_NAME" "INFO"
fi

if [ -z "$SQL_SERVER_NAME" ]; then
    SQL_SERVER_NAME=$(get_terraform_output "sql_server_name" "$TERRAFORM_DIR")
    if [ $? -ne 0 ] || [ -z "$SQL_SERVER_NAME" ]; then
        log_message "Failed to get SQL server name from Terraform output" "ERROR"
        exit 1
    fi
    log_message "Using SQL server name from Terraform: $SQL_SERVER_NAME" "INFO"
fi

if [ -z "$SQL_DATABASE_NAME" ]; then
    SQL_DATABASE_NAME=$(get_terraform_output "sql_database_name" "$TERRAFORM_DIR")
    if [ $? -ne 0 ] || [ -z "$SQL_DATABASE_NAME" ]; then
        log_message "Failed to get SQL database name from Terraform output" "ERROR"
        exit 1
    fi
    log_message "Using SQL database name from Terraform: $SQL_DATABASE_NAME" "INFO"
fi

if $EXPORT_BACKUP && [ -z "$STORAGE_ACCOUNT_NAME" ]; then
    STORAGE_ACCOUNT_NAME=$(get_terraform_output "storage_account_name" "$TERRAFORM_DIR")
    if [ $? -ne 0 ] || [ -z "$STORAGE_ACCOUNT_NAME" ]; then
        log_message "Failed to get storage account name from Terraform output" "ERROR"
        exit 1
    fi
    log_message "Using storage account name from Terraform: $STORAGE_ACCOUNT_NAME" "INFO"
fi

if $EXPORT_BACKUP && [ "$STORAGE_CONTAINER_NAME" == "backups" ]; then
    # Try to get from Terraform, but fallback to default 'backups' if not found
    CONTAINER_NAME=$(get_terraform_output "backups_container_name" "$TERRAFORM_DIR" 2>/dev/null) || CONTAINER_NAME=""
    if [ -n "$CONTAINER_NAME" ]; then
        STORAGE_CONTAINER_NAME="$CONTAINER_NAME"
    fi
    log_message "Using storage container name: $STORAGE_CONTAINER_NAME" "INFO"
fi

# Create backup name with timestamp and environment
BACKUP_NAME="${SQL_DATABASE_NAME}_${ENVIRONMENT}_${TIMESTAMP}"
log_message "Backup name: $BACKUP_NAME" "INFO"

# Create the SQL database backup
if ! create_sql_database_backup "$RESOURCE_GROUP_NAME" "$SQL_SERVER_NAME" "$SQL_DATABASE_NAME" "$BACKUP_NAME"; then
    log_message "Database backup failed" "ERROR"
    exit 1
fi

# Export the backup to Azure Storage if requested
if $EXPORT_BACKUP; then
    if [ -z "$STORAGE_ACCOUNT_NAME" ] || [ -z "$STORAGE_CONTAINER_NAME" ]; then
        log_message "Storage account name and container name are required for backup export" "ERROR"
        exit 1
    fi
    
    if ! export_sql_database_backup "$RESOURCE_GROUP_NAME" "$SQL_SERVER_NAME" "$SQL_DATABASE_NAME" "$STORAGE_ACCOUNT_NAME" "$STORAGE_CONTAINER_NAME"; then
        log_message "Database export failed" "ERROR"
        exit 1
    fi
    
    # Test backup integrity if requested
    if $TEST_INTEGRITY; then
        bacpac_name="${SQL_DATABASE_NAME}_${TIMESTAMP}.bacpac"
        if ! test_backup_integrity "$STORAGE_ACCOUNT_NAME" "$STORAGE_CONTAINER_NAME" "$bacpac_name"; then
            log_message "Backup integrity test failed" "ERROR"
            exit 1
        fi
    fi
    
    # Remove old backups if export was successful
    removed_count=$(remove_old_backups "$STORAGE_ACCOUNT_NAME" "$STORAGE_CONTAINER_NAME" "$RETENTION_DAYS")
    log_message "Removed $removed_count backups based on $RETENTION_DAYS day retention policy" "INFO"
fi

# Log successful completion
log_message "Database backup operations completed successfully" "SUCCESS"
log_message "Backup name: $BACKUP_NAME" "INFO"
if $EXPORT_BACKUP; then
    log_message "Exported to: $STORAGE_ACCOUNT_NAME/$STORAGE_CONTAINER_NAME/${SQL_DATABASE_NAME}_${TIMESTAMP}.bacpac" "INFO"
fi

exit 0