<#
.SYNOPSIS
    Automates backup and export of Azure SQL Databases for the Security Patrol Application.

.DESCRIPTION
    This script manages Azure SQL Database backups by retrieving information about automatic restore points
    and optionally exporting databases to Azure Storage. It supports disaster recovery procedures with
    backup integrity checking and retention management.

.PARAMETER Environment
    Target environment for database backup (dev, test, staging, prod).

.PARAMETER SubscriptionId
    Azure subscription ID.

.PARAMETER ResourceGroupName
    Azure resource group name (overrides Terraform output if specified).

.PARAMETER ServerName
    SQL Server name (overrides Terraform output if specified).

.PARAMETER DatabaseName
    SQL Database name (overrides Terraform output if specified).

.PARAMETER TerraformDir
    Path to Terraform directory containing state.

.PARAMETER Export
    Export backup to Azure Storage.

.PARAMETER StorageAccountName
    Storage account name for backup export.

.PARAMETER StorageContainerName
    Storage container name for backup export.

.PARAMETER RetentionDays
    Number of days to retain backups.

.PARAMETER TestIntegrity
    Test backup integrity after creation.

.PARAMETER LogFile
    Path to log file.

.PARAMETER Verbose
    Enable verbose logging.

.PARAMETER Help
    Display help information.

.EXAMPLE
    .\backup-database.ps1 -Environment prod -SubscriptionId "00000000-0000-0000-0000-000000000000"

.EXAMPLE
    .\backup-database.ps1 -ResourceGroupName "security-patrol-rg" -ServerName "secpatrol-sql" -DatabaseName "secpatrol-db" -Export -StorageAccountName "secpatrolstorage"

.NOTES
    File Name      : backup-database.ps1
    Author         : Security Patrol Development Team
    Prerequisite   : Az PowerShell module
    Version        : 1.0
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory=$false)]
    [ValidateSet("dev", "test", "staging", "prod")]
    [string]$Environment = "dev",

    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId = "",

    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "",

    [Parameter(Mandatory=$false)]
    [string]$ServerName = "",

    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "",

    [Parameter(Mandatory=$false)]
    [string]$TerraformDir = "../terraform",

    [Parameter(Mandatory=$false)]
    [switch]$Export = $false,

    [Parameter(Mandatory=$false)]
    [string]$StorageAccountName = "",

    [Parameter(Mandatory=$false)]
    [string]$StorageContainerName = "backups",

    [Parameter(Mandatory=$false)]
    [ValidateRange(1, 365)]
    [int]$RetentionDays = 30,

    [Parameter(Mandatory=$false)]
    [switch]$TestIntegrity = $false,

    [Parameter(Mandatory=$false)]
    [string]$LogFile = "./backup-database.log",

    [Parameter(Mandatory=$false)]
    [switch]$Verbose = $false,

    [Parameter(Mandatory=$false)]
    [switch]$Help = $false
)

# Script global variables
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

# Function for logging
function Write-Log {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$Message,

        [Parameter(Mandatory=$false)]
        [ValidateSet("INFO", "WARNING", "ERROR", "SUCCESS")]
        [string]$Level = "INFO"
    )

    $logMessage = "$(Get-Date -Format "yyyy-MM-dd HH:mm:ss") [$Level] $Message"
    
    # Set console color based on level
    switch ($Level) {
        "INFO"    { $color = "White" }
        "WARNING" { $color = "Yellow" }
        "ERROR"   { $color = "Red" }
        "SUCCESS" { $color = "Green" }
        default   { $color = "White" }
    }
    
    Write-Host $logMessage -ForegroundColor $color
    
    # Write to log file if specified
    if ($LogFile) {
        $logMessage | Out-File -FilePath $LogFile -Append -Encoding utf8
    }
}

# Function to check Azure login status
function Test-AzLogin {
    try {
        $context = Get-AzContext
        if ($null -eq $context.Account) {
            Write-Log "Not logged into Azure. Initiating login..." -Level "INFO"
            Connect-AzAccount -ErrorAction Stop
        } else {
            Write-Log "Already logged into Azure as $($context.Account.Id)" -Level "INFO"
        }
        return $true
    } catch {
        Write-Log "Error during Azure login: $_" -Level "ERROR"
        return $false
    }
}

# Function to select Azure subscription
function Select-AzSubscription {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$SubscriptionId
    )

    try {
        Write-Log "Setting Azure subscription to: $SubscriptionId" -Level "INFO"
        Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop
        Write-Log "Azure subscription set successfully" -Level "SUCCESS"
        return $true
    } catch {
        Write-Log "Error setting Azure subscription: $_" -Level "ERROR"
        return $false
    }
}

# Function to get Terraform outputs
function Get-TerraformOutput {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$OutputName,

        [Parameter(Mandatory=$true)]
        [string]$TerraformDir
    )

    try {
        Push-Location $TerraformDir
        $output = terraform output -raw $OutputName 2>$null
        Pop-Location
        return $output
    } catch {
        Write-Log "Error retrieving Terraform output '$OutputName': $_" -Level "ERROR"
        return $null
    }
}

# Function to get SQL database backup information (restore points)
function Get-SqlDatabaseBackupInfo {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory=$true)]
        [string]$ServerName,

        [Parameter(Mandatory=$true)]
        [string]$DatabaseName
    )

    try {
        Write-Log "Retrieving backup information for database '$DatabaseName' on server '$ServerName'" -Level "INFO"
        
        # Get database details
        $database = Get-AzSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName -ErrorAction Stop
        
        if ($null -eq $database) {
            Write-Log "Database not found" -Level "ERROR"
            return $null
        }
        
        # Check available restore points
        $restorePoints = Get-AzSqlDatabaseRestorePoint -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName -ErrorAction Stop
        
        # Log information about the database and backups
        Write-Log "Database details: Edition=$($database.Edition), Status=$($database.Status)" -Level "INFO"
        Write-Log "Available restore points: $($restorePoints.Count)" -Level "INFO"
        
        foreach ($point in $restorePoints) {
            Write-Log "  Restore point: Type=$($point.RestorePointType), Created=$($point.RestorePointCreationDate)" -Level "INFO"
        }
        
        # Return backup information as a custom object
        $backupInfo = [PSCustomObject]@{
            Database = $database
            RestorePoints = $restorePoints
            LatestRestorePoint = if ($restorePoints.Count -gt 0) { $restorePoints | Sort-Object -Property RestorePointCreationDate -Descending | Select-Object -First 1 } else { $null }
            Timestamp = Get-Date
            Status = if ($restorePoints.Count -gt 0) { "Available" } else { "NoRestorePoints" }
        }
        
        Write-Log "Backup information retrieved successfully" -Level "SUCCESS"
        return $backupInfo
    } catch {
        Write-Log "Error retrieving database backup information: $_" -Level "ERROR"
        return $null
    }
}

# Function to export SQL database to storage
function Export-SqlDatabaseBackup {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory=$true)]
        [string]$ServerName,

        [Parameter(Mandatory=$true)]
        [string]$DatabaseName,

        [Parameter(Mandatory=$true)]
        [string]$StorageAccountName,

        [Parameter(Mandatory=$true)]
        [string]$StorageContainerName
    )

    try {
        Write-Log "Exporting database '$DatabaseName' to storage account '$StorageAccountName'" -Level "INFO"
        
        # Generate storage URI
        $bacpacFilename = "$Environment-$DatabaseName-$timestamp.bacpac"
        $storageUri = "https://$StorageAccountName.blob.core.windows.net/$StorageContainerName/$bacpacFilename"
        
        # Get storage account key
        $storageKey = (Get-AzStorageAccountKey -ResourceGroupName $ResourceGroupName -Name $StorageAccountName)[0].Value
        
        # Get SQL server admin credentials
        $sqlServer = Get-AzSqlServer -ResourceGroupName $ResourceGroupName -ServerName $ServerName
        $adminLogin = $sqlServer.SqlAdministratorLogin
        
        # Prompt for SQL admin password
        $adminPassword = Read-Host "Enter SQL admin password for $adminLogin@$ServerName" -AsSecureString
        
        # Export database
        $exportRequest = New-AzSqlDatabaseExport -ResourceGroupName $ResourceGroupName `
            -ServerName $ServerName `
            -DatabaseName $DatabaseName `
            -StorageKeyType "StorageAccessKey" `
            -StorageKey $storageKey `
            -StorageUri $storageUri `
            -AdministratorLogin $adminLogin `
            -AdministratorLoginPassword $adminPassword `
            -AuthenticationType "SQL" `
            -ErrorAction Stop
        
        Write-Log "Export operation initiated, request ID: $($exportRequest.OperationStatusLink)" -Level "INFO"
        
        # Monitor export operation
        $operationId = $exportRequest.OperationStatusLink
        $exportStatus = Get-AzSqlDatabaseImportExportStatus -OperationStatusLink $operationId
        
        while ($exportStatus.Status -eq "InProgress") {
            Write-Log "Export in progress - Status: $($exportStatus.Status), Percent complete: $($exportStatus.PercentComplete)%" -Level "INFO"
            Start-Sleep -Seconds 10
            $exportStatus = Get-AzSqlDatabaseImportExportStatus -OperationStatusLink $operationId
        }
        
        if ($exportStatus.Status -eq "Succeeded") {
            Write-Log "Database export completed successfully" -Level "SUCCESS"
            Write-Log "Export file: $storageUri" -Level "INFO"
        } else {
            Write-Log "Database export failed: $($exportStatus.ErrorMessage)" -Level "ERROR"
        }
        
        # Return export result as a custom object
        $exportResult = [PSCustomObject]@{
            Status = $exportStatus.Status
            ErrorMessage = $exportStatus.ErrorMessage
            FileName = $bacpacFilename
            StorageUri = $storageUri
            Timestamp = Get-Date
        }
        
        return $exportResult
    } catch {
        Write-Log "Error exporting database: $_" -Level "ERROR"
        return $null
    }
}

# Function to remove old backups
function Remove-OldBackups {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory=$true)]
        [string]$StorageAccountName,

        [Parameter(Mandatory=$true)]
        [string]$StorageContainerName,

        [Parameter(Mandatory=$true)]
        [int]$RetentionDays
    )

    try {
        Write-Log "Checking for backups older than $RetentionDays days in storage account '$StorageAccountName'" -Level "INFO"
        
        # Get storage account key
        $storageAccountKey = (Get-AzStorageAccountKey -ResourceGroupName $ResourceGroupName -Name $StorageAccountName)[0].Value
        
        # Create storage context
        $storageContext = New-AzStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $storageAccountKey
        
        # Get all blobs in container
        $blobs = Get-AzStorageBlob -Container $StorageContainerName -Context $storageContext
        
        # Filter to backup files older than retention days
        $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
        $oldBackups = $blobs | Where-Object { $_.LastModified -lt $cutoffDate -and $_.Name -like "*.bacpac" }
        
        $removedCount = 0
        if ($oldBackups.Count -gt 0) {
            Write-Log "Found $($oldBackups.Count) backup(s) older than $RetentionDays days" -Level "INFO"
            foreach ($backup in $oldBackups) {
                Write-Log "Removing old backup: $($backup.Name), Last modified: $($backup.LastModified)" -Level "INFO"
                Remove-AzStorageBlob -Blob $backup.Name -Container $StorageContainerName -Context $storageContext -Force
                $removedCount++
            }
            Write-Log "Removed $removedCount old backup(s)" -Level "SUCCESS"
        } else {
            Write-Log "No backups older than $RetentionDays days found" -Level "INFO"
        }
        
        return $removedCount
    } catch {
        Write-Log "Error removing old backups: $_" -Level "ERROR"
        return 0
    }
}

# Function to test backup integrity
function Test-BackupIntegrity {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory=$true)]
        [string]$StorageAccountName,

        [Parameter(Mandatory=$true)]
        [string]$StorageContainerName,

        [Parameter(Mandatory=$true)]
        [string]$BackupFile
    )

    try {
        Write-Log "Testing integrity of backup file '$BackupFile'" -Level "INFO"
        
        # Get storage account key
        $storageAccountKey = (Get-AzStorageAccountKey -ResourceGroupName $ResourceGroupName -Name $StorageAccountName)[0].Value
        
        # Create storage context
        $storageContext = New-AzStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $storageAccountKey
        
        # Get blob properties to check size
        $blob = Get-AzStorageBlob -Container $StorageContainerName -Blob $BackupFile -Context $storageContext
        
        if ($null -eq $blob) {
            Write-Log "Backup file not found in storage" -Level "ERROR"
            return $false
        }
        
        # Check if file size is greater than 0
        if ($blob.Length -gt 0) {
            Write-Log "Backup integrity check passed: File size is $([math]::Round($blob.Length / 1MB, 2)) MB" -Level "SUCCESS"
            return $true
        } else {
            Write-Log "Backup integrity check failed: File size is 0 bytes" -Level "ERROR"
            return $false
        }
    } catch {
        Write-Log "Error testing backup integrity: $_" -Level "ERROR"
        return $false
    }
}

# Function to display script usage
function Show-Usage {
    Write-Host ""
    Write-Host "USAGE: backup-database.ps1 [parameters]" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "PURPOSE:" -ForegroundColor Cyan
    Write-Host "  Manages Azure SQL Database backups and exports for the Security Patrol Application"
    Write-Host ""
    Write-Host "PARAMETERS:" -ForegroundColor Cyan
    Write-Host "  -Environment           Target environment (dev, test, staging, prod)"
    Write-Host "  -SubscriptionId        Azure subscription ID"
    Write-Host "  -ResourceGroupName     Azure resource group name"
    Write-Host "  -ServerName            SQL Server name"
    Write-Host "  -DatabaseName          SQL Database name"
    Write-Host "  -TerraformDir          Path to Terraform directory"
    Write-Host "  -Export                Export backup to Azure Storage"
    Write-Host "  -StorageAccountName    Storage account name for backup export"
    Write-Host "  -StorageContainerName  Storage container name for backup export"
    Write-Host "  -RetentionDays         Number of days to retain backups"
    Write-Host "  -TestIntegrity         Test backup integrity after creation"
    Write-Host "  -LogFile               Path to log file"
    Write-Host "  -Verbose               Enable verbose logging"
    Write-Host "  -Help                  Display this help information"
    Write-Host ""
    Write-Host "EXAMPLES:" -ForegroundColor Cyan
    Write-Host "  .\backup-database.ps1 -Environment prod -SubscriptionId '00000000-0000-0000-0000-000000000000'"
    Write-Host "  .\backup-database.ps1 -ResourceGroupName 'security-patrol-rg' -ServerName 'secpatrol-sql' -DatabaseName 'secpatrol-db' -Export -StorageAccountName 'secpatrolstorage'"
    Write-Host ""
}

#-----------------------------------------------------
# Main script execution
#-----------------------------------------------------

# Display help if requested
if ($Help) {
    Show-Usage
    exit 0
}

# Initialize logging
if (-not (Test-Path (Split-Path -Parent $LogFile) -PathType Container)) {
    try {
        New-Item -ItemType Directory -Path (Split-Path -Parent $LogFile) -Force | Out-Null
    } catch {
        Write-Host "Warning: Unable to create log directory. Logging to console only." -ForegroundColor Yellow
        $LogFile = $null
    }
}

Write-Log "Starting database backup management script" -Level "INFO"
Write-Log "Environment: $Environment" -Level "INFO"

# Check if Az module is installed
if (-not (Get-Module -ListAvailable -Name Az.Sql)) {
    Write-Log "Az.Sql module is not installed. Installing..." -Level "WARNING"
    try {
        Install-Module -Name Az.Sql -Scope CurrentUser -Force -AllowClobber
        Write-Log "Az.Sql module installed successfully" -Level "SUCCESS"
    } catch {
        Write-Log "Failed to install Az.Sql module: $_" -Level "ERROR"
        exit 1
    }
}

# Check Azure login status
if (-not (Test-AzLogin)) {
    Write-Log "Failed to authenticate with Azure. Exiting." -Level "ERROR"
    exit 1
}

# Set Azure subscription if provided
if ($SubscriptionId) {
    if (-not (Select-AzSubscription -SubscriptionId $SubscriptionId)) {
        Write-Log "Failed to set Azure subscription. Exiting." -Level "ERROR"
        exit 1
    }
}

# If resource group, server name, or database name not provided, try to get from Terraform outputs
if (-not $ResourceGroupName -or -not $ServerName -or -not $DatabaseName) {
    Write-Log "Retrieving database information from Terraform outputs..." -Level "INFO"
    if (-not (Test-Path $TerraformDir)) {
        Write-Log "Terraform directory not found: $TerraformDir" -Level "ERROR"
        exit 1
    }
    
    if (-not $ResourceGroupName) {
        $ResourceGroupName = Get-TerraformOutput -OutputName "resource_group_name" -TerraformDir $TerraformDir
        if (-not $ResourceGroupName) {
            Write-Log "Failed to retrieve resource group name from Terraform outputs" -Level "ERROR"
            exit 1
        }
        Write-Log "Retrieved resource group name from Terraform: $ResourceGroupName" -Level "INFO"
    }
    
    if (-not $ServerName) {
        $ServerName = Get-TerraformOutput -OutputName "sql_server_name" -TerraformDir $TerraformDir
        if (-not $ServerName) {
            Write-Log "Failed to retrieve SQL server name from Terraform outputs" -Level "ERROR"
            exit 1
        }
        Write-Log "Retrieved SQL server name from Terraform: $ServerName" -Level "INFO"
    }
    
    if (-not $DatabaseName) {
        $DatabaseName = Get-TerraformOutput -OutputName "sql_database_name" -TerraformDir $TerraformDir
        if (-not $DatabaseName) {
            Write-Log "Failed to retrieve SQL database name from Terraform outputs" -Level "ERROR"
            exit 1
        }
        Write-Log "Retrieved SQL database name from Terraform: $DatabaseName" -Level "INFO"
    }
}

# Get backup information (Azure SQL maintains automatic backups)
$backupInfo = Get-SqlDatabaseBackupInfo -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName

if ($null -eq $backupInfo) {
    Write-Log "Failed to retrieve database backup information. Exiting." -Level "ERROR"
    exit 1
}

if ($backupInfo.Status -eq "NoRestorePoints") {
    Write-Log "No restore points found for database. This is unusual for an Azure SQL Database." -Level "WARNING"
}

# Export backup to Azure Storage if requested
if ($Export) {
    if (-not $StorageAccountName) {
        Write-Log "Storage account name must be provided when using -Export parameter" -Level "ERROR"
        exit 1
    }
    
    # Create container if it doesn't exist
    try {
        $storageKey = (Get-AzStorageAccountKey -ResourceGroupName $ResourceGroupName -Name $StorageAccountName)[0].Value
        $storageContext = New-AzStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $storageKey
        $container = Get-AzStorageContainer -Name $StorageContainerName -Context $storageContext -ErrorAction SilentlyContinue
        if ($null -eq $container) {
            Write-Log "Creating storage container '$StorageContainerName'" -Level "INFO"
            New-AzStorageContainer -Name $StorageContainerName -Context $storageContext -ErrorAction Stop
        }
    } catch {
        Write-Log "Failed to access or create storage container: $_" -Level "ERROR"
        exit 1
    }
    
    # Export database to storage
    $exportResult = Export-SqlDatabaseBackup -ResourceGroupName $ResourceGroupName -ServerName $ServerName -DatabaseName $DatabaseName -StorageAccountName $StorageAccountName -StorageContainerName $StorageContainerName
    
    if ($null -eq $exportResult -or $exportResult.Status -ne "Succeeded") {
        Write-Log "Database export failed. Check the log for details." -Level "ERROR"
        exit 1
    }
    
    # Test backup integrity if requested
    if ($TestIntegrity) {
        $integrityResult = Test-BackupIntegrity -ResourceGroupName $ResourceGroupName -StorageAccountName $StorageAccountName -StorageContainerName $StorageContainerName -BackupFile $exportResult.FileName
        
        if (-not $integrityResult) {
            Write-Log "Backup integrity check failed. The backup may be corrupted." -Level "ERROR"
            exit 1
        }
    }
    
    # Clean up old backups based on retention policy
    $cleanupResult = Remove-OldBackups -ResourceGroupName $ResourceGroupName -StorageAccountName $StorageAccountName -StorageContainerName $StorageContainerName -RetentionDays $RetentionDays
    Write-Log "Backup cleanup removed $cleanupResult old backup(s)" -Level "INFO"
}

# Log summary
Write-Log "Backup management operation completed successfully" -Level "SUCCESS"
Write-Log "Summary:" -Level "INFO"
Write-Log "  Database: $DatabaseName" -Level "INFO"
Write-Log "  Server: $ServerName" -Level "INFO"
Write-Log "  Environment: $Environment" -Level "INFO"
Write-Log "  Available Restore Points: $($backupInfo.RestorePoints.Count)" -Level "INFO"
if ($backupInfo.LatestRestorePoint) {
    Write-Log "  Latest Restore Point: $($backupInfo.LatestRestorePoint.RestorePointCreationDate)" -Level "INFO"
}
if ($Export) {
    Write-Log "  Exported to: $StorageAccountName/$StorageContainerName/$($exportResult.FileName)" -Level "INFO"
    Write-Log "  Export Status: $($exportResult.Status)" -Level "INFO"
}

exit 0