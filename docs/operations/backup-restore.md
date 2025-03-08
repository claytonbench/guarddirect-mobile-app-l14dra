## Introduction

This document provides comprehensive guidance on backup and restore procedures for the Security Patrol application. It covers database backups, blob storage backups, application configuration backups, and restore procedures for disaster recovery.

Backup and restore procedures are critical components of the Security Patrol application's operational resilience, ensuring that data can be recovered in the event of system failures, data corruption, or other disasters. This document serves as a reference for operations teams, developers, and stakeholders responsible for maintaining the application's data integrity and availability.

## Backup Strategy
The Security Patrol application implements a comprehensive backup strategy to ensure data integrity and availability in the event of failures or disasters.

### Backup Objectives
The backup strategy is designed to meet the following objectives:

| Objective | Target | Description |
|-----------|--------|-------------|
| Recovery Point Objective (RPO) | 1 hour | Maximum acceptable data loss in the event of a disaster |
| Recovery Time Objective (RTO) | 4 hours | Maximum acceptable time to restore service after a disaster |
| Backup Frequency | Varies by data type | Frequency of backup operations for different data types |
| Backup Retention | Varies by environment | Period for which backups are retained |
| Backup Verification | Daily | Frequency of backup integrity verification |

These objectives guide the implementation of backup procedures and ensure that the system can recover from failures within acceptable timeframes.

### Backup Types
The Security Patrol application uses the following types of backups:

1. **Database Backups**:
   - **Full Backups**: Complete backup of the database
   - **Differential Backups**: Backup of changes since the last full backup
   - **Transaction Log Backups**: Backup of transaction logs for point-in-time recovery

2. **Blob Storage Backups**:
   - **Snapshot Backups**: Point-in-time snapshots of blob containers
   - **Copy Backups**: Full copies of blob data to separate storage accounts

3. **Configuration Backups**:
   - **App Settings Backups**: Backup of application configuration settings
   - **Key Vault Backups**: Backup of secrets and certificates

4. **Mobile App Backups**:
   - **App Package Backups**: Backup of published app packages
   - **App Data Backups**: Backup of app-specific data

Each backup type has specific procedures, schedules, and retention policies as detailed in the following sections.

### Backup Schedule
The following backup schedule is implemented for the Security Patrol application:

| Data Type | Environment | Backup Type | Frequency | Retention |
|-----------|------------|-------------|-----------|----------|
| SQL Database | Production | Full | Daily | 30 days |
| SQL Database | Production | Differential | Every 12 hours | 7 days |
| SQL Database | Production | Transaction Log | Hourly | 24 hours |
| SQL Database | Staging | Full | Daily | 14 days |
| SQL Database | Staging | Differential | Daily | 7 days |
| SQL Database | Dev/Test | Full | Weekly | 7 days |
| Blob Storage | Production | Snapshot | Daily | 30 days |
| Blob Storage | Production | Copy | Weekly | 90 days |
| Blob Storage | Staging | Snapshot | Weekly | 14 days |
| Blob Storage | Dev/Test | Snapshot | On demand | 7 days |
| App Configuration | All | Full | After changes | 5 versions |
| Key Vault | All | Full | After changes | 5 versions |
| Mobile App Packages | All | Full | After releases | All versions |

This schedule ensures that backups are created with appropriate frequency based on the environment and data criticality.

### Backup Storage
Backups are stored in the following locations:

1. **Primary Backups**:
   - Azure SQL Database automatic backups (stored by the service)
   - Azure Blob Storage snapshots (stored within the service)
   - Azure Key Vault backups (stored within the service)

2. **Secondary Backups**:
   - Exported database backups (.bacpac files) in Azure Storage
   - Blob Storage copies in separate storage accounts
   - Configuration exports in Azure Storage

3. **Tertiary Backups** (for critical data):
   - Cross-region copies in secondary Azure region
   - Offline backups for regulatory compliance (where required)

Backup storage is configured with appropriate redundancy and access controls to ensure data protection and availability.

### Backup Security
The following security measures are implemented for backups:

1. **Access Control**:
   - Restricted access to backup storage using Azure RBAC
   - Separate credentials for backup operations
   - Audit logging of all backup access

2. **Data Protection**:
   - Encryption of backups at rest using Azure Storage encryption
   - Encryption of backups in transit using TLS
   - Immutable storage for critical backups

3. **Operational Security**:
   - Secure handling of backup credentials
   - Regular rotation of backup access keys
   - Monitoring of backup operations for anomalies

These security measures ensure that backups are protected from unauthorized access and tampering.

## Database Backup Procedures
This section details the procedures for backing up the Azure SQL Database used by the Security Patrol application.

### Automatic Backups
Azure SQL Database automatically creates the following backups:

1. **Full Database Backups**: Created weekly
2. **Differential Database Backups**: Created every 12 hours
3. **Transaction Log Backups**: Created every 5-10 minutes

These automatic backups are managed by the Azure SQL Database service and support point-in-time restore within the retention period. The retention period is configured based on the environment:

| Environment | Retention Period | Configuration Method |
|------------|------------------|---------------------|
| Production | 30 days | Azure Portal or Terraform |
| Staging | 14 days | Azure Portal or Terraform |
| Dev/Test | 7 days | Azure Portal or Terraform |

To configure the retention period using Azure CLI:

```bash
# Set retention period for a database
az sql db update --resource-group <resource_group> --server <server_name> --name <database_name> --retention-days <days>
```

To configure the retention period using Azure PowerShell:

```powershell
# Set retention period for a database
Set-AzSqlDatabaseBackupShortTermRetentionPolicy -ResourceGroupName <resource_group> -ServerName <server_name> -DatabaseName <database_name> -RetentionDays <days>
```

### Manual Database Backups
In addition to automatic backups, manual database backups can be created for specific purposes such as pre-deployment backups or on-demand backups for testing.

The Security Patrol application includes scripts for creating manual database backups:

1. **For Linux/macOS environments**: `infrastructure/scripts/backup-database.sh`
2. **For Windows environments**: `infrastructure/scripts/backup-database.ps1`

**Using the Bash script**:

```bash
# Navigate to the scripts directory
cd infrastructure/scripts

# Create a full backup of the production database
./backup-database.sh -e prod -x --backup-type Full

# Create a backup with custom retention and export to storage
./backup-database.sh -e prod -x --backup-type Full -r 60 --test-integrity
```

**Using the PowerShell script**:

```powershell
# Navigate to the scripts directory
cd infrastructure/scripts

# Create a full backup of the production database
./backup-database.ps1 -Environment prod -Export -BackupType Full

# Create a backup with custom retention and export to storage
./backup-database.ps1 -Environment prod -Export -BackupType Full -RetentionDays 60 -TestIntegrity
```

These scripts perform the following actions:

1. Connect to the Azure SQL Database
2. Create a backup of the specified type
3. Export the backup to Azure Storage (if specified)
4. Test the backup integrity (if specified)
5. Remove old backups based on retention policy
6. Log the backup operation details

For detailed script parameters and options, refer to the script help:

```bash
./backup-database.sh --help
```

```powershell
./backup-database.ps1 -Help
```

### Database Export
For long-term retention or cross-environment migration, database exports can be created as .bacpac files. These exports contain both the schema and data of the database.

**Using Azure CLI**:

```bash
# Export database to storage account
az sql db export --resource-group <resource_group> --server <server_name> --name <database_name> \
  --admin-user <admin_username> --admin-password <admin_password> \
  --storage-key-type StorageAccessKey --storage-key <storage_key> \
  --storage-uri https://<storage_account>.blob.core.windows.net/<container>/<bacpac_file>.bacpac
```

**Using Azure PowerShell**:

```powershell
# Export database to storage account
$credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList <admin_username>, (ConvertTo-SecureString -String <admin_password> -AsPlainText -Force)

$storageContext = New-AzStorageContext -StorageAccountName <storage_account> -StorageAccountKey <storage_key>
$storageUri = New-AzStorageContainerSASToken -Context $storageContext -Container <container> -Permission rwdl

Export-AzSqlDatabase -ResourceGroupName <resource_group> -ServerName <server_name> -DatabaseName <database_name> \
  -StorageKeyType StorageAccessKey -StorageKey <storage_key> \
  -StorageUri "https://<storage_account>.blob.core.windows.net/<container>/<bacpac_file>.bacpac" \
  -AdministratorLogin $credential.UserName -AdministratorLoginPassword $credential.Password
```

Database exports should be scheduled according to the backup schedule defined in the Backup Strategy section.

### Long-term Retention
For regulatory compliance or business continuity purposes, long-term retention (LTR) policies can be configured for Azure SQL Database backups.

**Using Azure CLI**:

```bash
# Configure long-term retention policy
az sql db ltr-policy set --resource-group <resource_group> --server <server_name> --database <database_name> \
  --weekly-retention P1W --monthly-retention P1M --yearly-retention P1Y --week-of-year 1
```

**Using Azure PowerShell**:

```powershell
# Configure long-term retention policy
Set-AzSqlDatabaseBackupLongTermRetentionPolicy -ResourceGroupName <resource_group> -ServerName <server_name> -DatabaseName <database_name> \
  -WeeklyRetention P1W -MonthlyRetention P1M -YearlyRetention P1Y -WeekOfYear 1
```

Long-term retention policies should be configured based on regulatory requirements and data retention policies.

### Backup Monitoring
Database backup operations should be monitored to ensure they are completing successfully and meeting the defined RPO.

**Monitoring Automatic Backups**:

```bash
# Check last backup time for a database
az sql db show --resource-group <resource_group> --server <server_name> --name <database_name> --query "lastBackupDate"
```

**Monitoring Manual Backups**:

- Check the backup script logs in the specified log file
- Verify backup files in the storage account
- Check backup status in Azure Portal

**Monitoring Backup Storage**:

```bash
# Check storage usage for backups
az storage blob list --account-name <storage_account> --container-name <container> --query "[].properties.contentLength" --output tsv | awk '{sum += $1} END {print sum/1024/1024/1024 " GB"}'
```

Backup monitoring should be integrated with the overall monitoring strategy for the Security Patrol application.

### Backup Verification
Regular verification of database backups is essential to ensure they can be successfully restored when needed.

**Automated Verification**:

The backup scripts include an option to test backup integrity after creation:

```bash
./backup-database.sh -e prod -x --test-integrity
```

```powershell
./backup-database.ps1 -Environment prod -Export -TestIntegrity
```

**Manual Verification**:

Periodically, a full restore test should be performed to verify backup integrity:

1. Restore the database to a test environment
2. Verify database schema and data integrity
3. Test application functionality with the restored database
4. Document the verification results

Backup verification should be performed according to the schedule defined in the Backup Strategy section.

## Blob Storage Backup Procedures
This section details the procedures for backing up the Azure Blob Storage used by the Security Patrol application for storing photos and other files.

### Blob Storage Snapshots
Azure Blob Storage snapshots provide point-in-time copies of blobs that can be used for backup and recovery.

**Using Azure CLI**:

```bash
# Create a snapshot of a blob
az storage blob snapshot --account-name <storage_account> --container-name <container> --name <blob_name> --auth-mode key --account-key <account_key>

# Create snapshots for all blobs in a container
for blob in $(az storage blob list --account-name <storage_account> --container-name <container> --auth-mode key --account-key <account_key> --query "[].name" -o tsv); do
  az storage blob snapshot --account-name <storage_account> --container-name <container> --name "$blob" --auth-mode key --account-key <account_key>
done
```

**Using Azure PowerShell**:

```powershell
# Create a snapshot of a blob
$context = New-AzStorageContext -StorageAccountName <storage_account> -StorageAccountKey <account_key>
New-AzStorageBlobSnapshot -Context $context -Container <container> -Blob <blob_name>

# Create snapshots for all blobs in a container
$context = New-AzStorageContext -StorageAccountName <storage_account> -StorageAccountKey <account_key>
$blobs = Get-AzStorageBlob -Context $context -Container <container>
foreach ($blob in $blobs) {
  New-AzStorageBlobSnapshot -Context $context -Container <container> -Blob $blob.Name
}
```

Blob snapshots should be created according to the schedule defined in the Backup Strategy section.

### Blob Storage Replication
Azure Storage offers built-in replication capabilities that can be used as part of the backup strategy:

1. **Locally Redundant Storage (LRS)**: Replicates data within a single data center
2. **Zone-Redundant Storage (ZRS)**: Replicates data across multiple data centers within a region
3. **Geo-Redundant Storage (GRS)**: Replicates data to a secondary region
4. **Read-Access Geo-Redundant Storage (RA-GRS)**: GRS with read access to the secondary region

The Security Patrol application uses the following replication settings:

| Environment | Replication Type | Configuration Method |
|------------|------------------|---------------------|
| Production | GRS | Terraform |
| Staging | ZRS | Terraform |
| Dev/Test | LRS | Terraform |

To configure replication using Azure CLI:

```bash
# Update storage account replication type
az storage account update --resource-group <resource_group> --name <storage_account> --sku Standard_GRS
```

To configure replication using Azure PowerShell:

```powershell
# Update storage account replication type
Set-AzStorageAccount -ResourceGroupName <resource_group> -Name <storage_account> -SkuName Standard_GRS
```

Replication settings should be configured during infrastructure provisioning and updated as needed based on data protection requirements.

### Blob Storage Copy Backups
For additional protection, blob data can be copied to a separate storage account in the same or different region.

**Using AzCopy**:

```bash
# Copy all blobs from one container to another
azcopy copy "https://<source_account>.blob.core.windows.net/<source_container>/*" "https://<destination_account>.blob.core.windows.net/<destination_container>/" --recursive

# Copy with SAS tokens for authentication
azcopy copy "https://<source_account>.blob.core.windows.net/<source_container>/*?<source_sas>" "https://<destination_account>.blob.core.windows.net/<destination_container>/?<destination_sas>" --recursive
```

**Using Azure PowerShell**:

```powershell
# Copy all blobs from one container to another
$sourceContext = New-AzStorageContext -StorageAccountName <source_account> -StorageAccountKey <source_key>
$destinationContext = New-AzStorageContext -StorageAccountName <destination_account> -StorageAccountKey <destination_key>

$blobs = Get-AzStorageBlob -Context $sourceContext -Container <source_container>
foreach ($blob in $blobs) {
  Start-AzStorageBlobCopy -Context $sourceContext -SrcContainer <source_container> -SrcBlob $blob.Name -DestContext $destinationContext -DestContainer <destination_container> -DestBlob $blob.Name
}
```

Blob copy backups should be scheduled according to the backup schedule defined in the Backup Strategy section.

### Blob Soft Delete
Azure Blob Storage soft delete feature provides protection against accidental deletion or overwrite of data. When enabled, deleted blobs are retained for a specified period and can be recovered if needed.

**Using Azure CLI**:

```bash
# Enable blob soft delete
az storage account blob-service-properties update --resource-group <resource_group> --account-name <storage_account> --enable-delete-retention true --delete-retention-days 30

# Recover a deleted blob
az storage blob undelete --account-name <storage_account> --container-name <container> --name <blob_name> --auth-mode key --account-key <account_key>
```

**Using Azure PowerShell**:

```powershell
# Enable blob soft delete
Enable-AzStorageBlobDeleteRetentionPolicy -ResourceGroupName <resource_group> -StorageAccountName <storage_account> -RetentionDays 30

# Recover a deleted blob
$context = New-AzStorageContext -StorageAccountName <storage_account> -StorageAccountKey <account_key>
Restore-AzStorageDeletedBlob -Context $context -Container <container> -Blob <blob_name>
```

Soft delete should be enabled for all storage accounts used by the Security Patrol application, with retention periods aligned with the backup strategy.

### Immutable Blob Storage
For critical data that requires protection against modification or deletion, Azure Blob Storage immutable storage can be configured.

**Using Azure CLI**:

```bash
# Enable immutable storage with time-based retention
az storage container immutability-policy create --account-name <storage_account> --container-name <container> --period 365 --allow-protected-append-writes false
```

**Using Azure PowerShell**:

```powershell
# Enable immutable storage with time-based retention
$context = New-AzStorageContext -StorageAccountName <storage_account> -StorageAccountKey <account_key>
Add-AzStorageContainerLegalHold -Context $context -Container <container> -Tag "compliance"
```

Immutable storage should be configured for containers storing critical data such as security incident photos or regulatory compliance data.

### Blob Backup Monitoring
Blob storage backup operations should be monitored to ensure they are completing successfully and meeting the defined RPO.

**Monitoring Snapshots**:

```bash
# List snapshots for a blob
az storage blob list --account-name <storage_account> --container-name <container> --prefix <blob_name> --include s --auth-mode key --account-key <account_key> --output table
```

**Monitoring Copy Operations**:

```bash
# Check copy status
az storage blob show --account-name <destination_account> --container-name <destination_container> --name <blob_name> --auth-mode key --account-key <account_key> --query "properties.copy"
```

**Monitoring Storage Metrics**:

```bash
# Get storage metrics
az monitor metrics list --resource <storage_account_resource_id> --metric "BlobCount" --interval PT1H
```

Blob backup monitoring should be integrated with the overall monitoring strategy for the Security Patrol application.

## Configuration Backup Procedures
This section details the procedures for backing up application configuration settings, secrets, and other configuration data.

### App Service Configuration Backup
Azure App Service configuration includes application settings, connection strings, and other configuration that should be backed up regularly.

**Using Azure CLI**:

```bash
# Export App Service configuration
az webapp config appsettings list --resource-group <resource_group> --name <app_service_name> > appsettings-backup-$(date +%Y%m%d).json

# Export connection strings
az webapp config connection-string list --resource-group <resource_group> --name <app_service_name> > connectionstrings-backup-$(date +%Y%m%d).json
```

**Using Azure PowerShell**:

```powershell
# Export App Service configuration
$settings = Get-AzWebApp -ResourceGroupName <resource_group> -Name <app_service_name> | Select-Object -ExpandProperty SiteConfig | Select-Object -ExpandProperty AppSettings
$settings | ConvertTo-Json | Out-File -FilePath "appsettings-backup-$(Get-Date -Format 'yyyyMMdd').json"

# Export connection strings
$connections = Get-AzWebApp -ResourceGroupName <resource_group> -Name <app_service_name> | Select-Object -ExpandProperty SiteConfig | Select-Object -ExpandProperty ConnectionStrings
$connections | ConvertTo-Json | Out-File -FilePath "connectionstrings-backup-$(Get-Date -Format 'yyyyMMdd').json"
```

App Service configuration backups should be created before making significant changes and according to the schedule defined in the Backup Strategy section.

### Key Vault Backup
Azure Key Vault stores secrets, keys, and certificates used by the Security Patrol application. These should be backed up regularly.

**Using Azure CLI**:

```bash
# Backup a secret
az keyvault secret backup --vault-name <key_vault_name> --name <secret_name> --file <secret_name>-backup-$(date +%Y%m%d).bak

# Backup a key
az keyvault key backup --vault-name <key_vault_name> --name <key_name> --file <key_name>-backup-$(date +%Y%m%d).bak

# Backup a certificate
az keyvault certificate backup --vault-name <key_vault_name> --name <certificate_name> --file <certificate_name>-backup-$(date +%Y%m%d).bak
```

**Using Azure PowerShell**:

```powershell
# Backup a secret
Backup-AzKeyVaultSecret -VaultName <key_vault_name> -Name <secret_name> -OutputFile "<secret_name>-backup-$(Get-Date -Format 'yyyyMMdd').bak"

# Backup a key
Backup-AzKeyVaultKey -VaultName <key_vault_name> -Name <key_name> -OutputFile "<key_name>-backup-$(Get-Date -Format 'yyyyMMdd').bak"

# Backup a certificate
Backup-AzKeyVaultCertificate -VaultName <key_vault_name> -Name <certificate_name> -OutputFile "<certificate_name>-backup-$(Get-Date -Format 'yyyyMMdd').bak"
```

Key Vault backups should be created after adding or modifying secrets, keys, or certificates, and according to the schedule defined in the Backup Strategy section.

### Infrastructure Configuration Backup
The infrastructure configuration for the Security Patrol application is managed using Terraform. The Terraform state and configuration files should be backed up regularly.

**Terraform State Backup**:

When using remote state storage in Azure Storage, the state is automatically versioned. Additional backups can be created manually:

```bash
# Create a backup of the Terraform state
cd infrastructure/terraform
terraform state pull > terraform-state-backup-$(date +%Y%m%d).tfstate
```

**Terraform Configuration Backup**:

Terraform configuration files are stored in version control, which provides history and backup capabilities. Additional backups can be created for critical environments:

```bash
# Create a backup of Terraform configuration
cd infrastructure
tar -czf terraform-config-backup-$(date +%Y%m%d).tar.gz terraform/
```

Infrastructure configuration backups should be created before making significant changes and according to the schedule defined in the Backup Strategy section.

### Mobile App Configuration Backup
The mobile app configuration includes build-time constants, API endpoints, and other settings that should be backed up regularly.

**Build-time Configuration**:

```bash
# Backup mobile app constants
cp src/android/SecurityPatrol/Constants/AppConstants.cs AppConstants-backup-$(date +%Y%m%d).cs
cp src/android/SecurityPatrol/Constants/ApiEndpoints.cs ApiEndpoints-backup-$(date +%Y%m%d).cs
```

**App Center Configuration**:

```bash
# Export App Center configuration using App Center CLI
appcenter apps get-current
appcenter analytics app-versions
appcenter crashes list-versions
```

Mobile app configuration backups should be created before making significant changes and according to the schedule defined in the Backup Strategy section.

### Configuration Backup Storage
Configuration backups should be stored securely to prevent unauthorized access to sensitive information.

**Secure Storage Options**:

1. **Azure Storage**: Store backups in a dedicated, secured container
2. **Azure Key Vault**: Store small configuration backups as secrets
3. **Secure File Repository**: Store backups in a secure, access-controlled repository

**Using Azure Storage for Configuration Backups**:

```bash
# Upload configuration backup to Azure Storage
az storage blob upload --account-name <storage_account> --container-name <container> --name config-backups/appsettings-backup-$(date +%Y%m%d).json --file appsettings-backup-$(date +%Y%m%d).json --auth-mode key --account-key <account_key>
```

**Using Azure Key Vault for Configuration Backups**:

```bash
# Store small configuration backup in Key Vault
az keyvault secret set --vault-name <key_vault_name> --name "AppSettings-Backup-$(date +%Y%m%d)" --file appsettings-backup-$(date +%Y%m%d).json
```

Configuration backups should be encrypted and access-controlled to prevent unauthorized access to sensitive information.

## Restore Procedures
This section details the procedures for restoring data from backups in various scenarios.

### Database Restore Procedures
The following procedures can be used to restore the Azure SQL Database from backups.

**Point-in-Time Restore**:

Restore the database to a specific point in time within the retention period:

```bash
# Restore database to a point in time
az sql db restore --resource-group <resource_group> --server <server_name> --name <database_name> --dest-name <restored_database_name> --time "2023-07-15T13:10:00Z"
```

```powershell
# Restore database to a point in time
Restore-AzSqlDatabase -FromPointInTimeBackup -ResourceGroupName <resource_group> -ServerName <server_name> -DatabaseName <database_name> -TargetDatabaseName <restored_database_name> -PointInTime "2023-07-15T13:10:00Z"
```

**Geo-Restore**:

Restore the database from a geo-replicated backup in case of region outage:

```bash
# Geo-restore database
az sql db restore --resource-group <resource_group> --server <server_name> --name <database_name> --dest-name <restored_database_name> --dest-resource-group <dest_resource_group> --dest-server <dest_server_name> --time "2023-07-15T13:10:00Z"
```

```powershell
# Geo-restore database
Restore-AzSqlDatabase -FromGeoBackup -ResourceGroupName <dest_resource_group> -ServerName <dest_server_name> -TargetDatabaseName <restored_database_name> -ResourceId <source_database_resource_id>
```

**Restore from Backup File**:

Restore the database from a .bacpac file:

```bash
# Import database from bacpac file
az sql db import --resource-group <resource_group> --server <server_name> --name <database_name> --storage-key-type StorageAccessKey --storage-key <storage_key> --storage-uri "https://<storage_account>.blob.core.windows.net/<container>/<bacpac_file>.bacpac" --admin-user <admin_username> --admin-password <admin_password>
```

```powershell
# Import database from bacpac file
$credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList <admin_username>, (ConvertTo-SecureString -String <admin_password> -AsPlainText -Force)

Import-AzSqlDatabaseBackup -ResourceGroupName <resource_group> -ServerName <server_name> -DatabaseName <database_name> \
  -StorageKeyType StorageAccessKey -StorageKey <storage_key> \
  -StorageUri "https://<storage_account>.blob.core.windows.net/<container>/<bacpac_file>.bacpac" \
  -AdministratorLogin $credential.UserName -AdministratorLoginPassword $credential.Password
```

**Post-Restore Actions**:

After restoring a database, the following actions should be performed:

1. Verify database integrity and consistency
2. Update connection strings if using a different database name
3. Test application functionality with the restored database
4. Monitor performance to ensure the restored database is functioning correctly
5. Document the restore operation details

### Blob Storage Restore Procedures
The following procedures can be used to restore Azure Blob Storage data from backups.

**Restore from Snapshot**:

Restore a blob from a snapshot:

```bash
# List snapshots for a blob
az storage blob list --account-name <storage_account> --container-name <container> --prefix <blob_name> --include s --auth-mode key --account-key <account_key> --output table

# Copy a snapshot to the original blob (restore)
az storage blob copy start --account-name <storage_account> --container-name <container> --name <blob_name> --source-account-name <storage_account> --source-container <container> --source-blob <blob_name> --source-snapshot <snapshot_timestamp> --auth-mode key --account-key <account_key>
```

```powershell
# List snapshots for a blob
$context = New-AzStorageContext -StorageAccountName <storage_account> -StorageAccountKey <account_key>
Get-AzStorageBlob -Context $context -Container <container> -Blob <blob_name> -IncludeSnapshot

# Copy a snapshot to the original blob (restore)
$context = New-AzStorageContext -StorageAccountName <storage_account> -StorageAccountKey <account_key>
Start-AzStorageBlobCopy -Context $context -SrcContainer <container> -SrcBlob <blob_name> -DestContainer <container> -DestBlob <blob_name> -SrcSnapshot <snapshot_timestamp>
```

**Restore from Soft Delete**:

Recover a deleted blob within the retention period:

```bash
# List deleted blobs
az storage blob list --account-name <storage_account> --container-name <container> --include d --auth-mode key --account-key <account_key> --output table

# Undelete a blob
az storage blob undelete --account-name <storage_account> --container-name <container> --name <blob_name> --auth-mode key --account-key <account_key>
```

```powershell
# List deleted blobs
$context = New-AzStorageContext -StorageAccountName <storage_account> -StorageAccountKey <account_key>
Get-AzStorageBlob -Context $context -Container <container> -IncludeDeleted

# Undelete a blob
$context = New-AzStorageContext -StorageAccountName <storage_account> -StorageAccountKey <account_key>
Restore-AzStorageDeletedBlob -Context $context -Container <container> -Blob <blob_name>
```

**Restore from Copy Backup**:

Restore data from a backup copy in another storage account:

```bash
# Copy blobs from backup storage to original storage
azcopy copy "https://<backup_account>.blob.core.windows.net/<backup_container>/<path>/*" "https://<original_account>.blob.core.windows.net/<original_container>/<path>/" --recursive
```

```powershell
# Copy blobs from backup storage to original storage
$sourceContext = New-AzStorageContext -StorageAccountName <backup_account> -StorageAccountKey <backup_key>
$destinationContext = New-AzStorageContext -StorageAccountName <original_account> -StorageAccountKey <original_key>

$blobs = Get-AzStorageBlob -Context $sourceContext -Container <backup_container> -Prefix <path>
foreach ($blob in $blobs) {
  $destBlob = $blob.Name
  Start-AzStorageBlobCopy -Context $sourceContext -SrcContainer <backup_container> -SrcBlob $blob.Name -DestContext $destinationContext -DestContainer <original_container> -DestBlob $destBlob
}
```

**Post-Restore Actions**:

After restoring blob storage data, the following actions should be performed:

1. Verify data integrity and completeness
2. Test application access to the restored data
3. Update any references or metadata if necessary
4. Monitor storage performance and access patterns
5. Document the restore operation details

### Configuration Restore Procedures
The following procedures can be used to restore application configuration from backups.

**Restore App Service Configuration**:

Restore application settings and connection strings:

```bash
# Restore application settings
az webapp config appsettings set --resource-group <resource_group> --name <app_service_name> --settings @appsettings-backup.json

# Restore connection strings
az webapp config connection-string set --resource-group <resource_group> --name <app_service_name> --connection-string-type SQLAzure --settings @connectionstrings-backup.json
```

```powershell
# Restore application settings
$settings = Get-Content -Path "appsettings-backup.json" | ConvertFrom-Json
foreach ($setting in $settings) {
  $settingName = $setting.name
  $settingValue = $setting.value
  Set-AzWebAppSetting -ResourceGroupName <resource_group> -Name <app_service_name> -AppSetting @{$settingName = $settingValue}
}
```

**Restore Key Vault Secrets**:

Restore secrets, keys, and certificates from backups:

```bash
# Restore a secret
az keyvault secret restore --vault-name <key_vault_name> --file <secret_backup_file>

# Restore a key
az keyvault key restore --vault-name <key_vault_name> --name <key_name> --file <key_backup_file>

# Restore a certificate
az keyvault certificate restore --vault-name <key_vault_name> --name <certificate_name> --file <certificate_backup_file>
```

```powershell
# Restore a secret
Restore-AzKeyVaultSecret -VaultName <key_vault_name> -InputFile <secret_backup_file>

# Restore a key
Restore-AzKeyVaultKey -VaultName <key_vault_name> -InputFile <key_backup_file>

# Restore a certificate
Restore-AzKeyVaultCertificate -VaultName <key_vault_name> -InputFile <certificate_backup_file>
```

**Restore Infrastructure Configuration**:

Restore Terraform state and configuration:

```bash
# Restore Terraform state
cd infrastructure/terraform
cp terraform-state-backup.tfstate terraform.tfstate

# Apply Terraform configuration
terraform init
terraform plan
terraform apply