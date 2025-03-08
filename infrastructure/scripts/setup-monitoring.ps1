<#
.SYNOPSIS
    Sets up Azure monitoring infrastructure for the Security Patrol Application.

.DESCRIPTION
    This script automates the deployment and configuration of Azure monitoring resources
    including Application Insights, Log Analytics workspace, alerts, dashboards, and 
    diagnostic settings for the Security Patrol Application.

.PARAMETER DeploymentMethod
    Method to use for deploying monitoring resources (ARM, Bicep, or PowerShell)

.PARAMETER Environment
    Target environment for monitoring setup (dev, staging, prod)

.PARAMETER ResourceGroupName
    Azure resource group name where monitoring resources will be created

.PARAMETER Location
    Azure region for monitoring resources

.PARAMETER SubscriptionId
    Azure subscription ID

.PARAMETER AppServiceName
    Name of the App Service to monitor

.PARAMETER SqlServerName
    Name of the SQL Server to monitor

.PARAMETER SqlDatabaseName
    Name of the SQL Database to monitor

.PARAMETER StorageAccountName
    Name of the Storage Account to monitor

.PARAMETER AlertEmailAddress
    Email address for receiving monitoring alerts

.PARAMETER AppInsightsRetentionDays
    Number of days to retain monitoring data in Application Insights

.PARAMETER LogAnalyticsRetentionDays
    Number of days to retain monitoring data in Log Analytics

.PARAMETER ApiResponseTimeThreshold
    Threshold in milliseconds for API response time alerts

.PARAMETER ApiFailureRateThreshold
    Threshold percentage for API failure rate alerts

.PARAMETER OutputFile
    Path to save monitoring configuration as JSON

.PARAMETER Verbose
    Enable verbose logging

.PARAMETER Force
    Force recreation of resources even if they exist

.EXAMPLE
    .\setup-monitoring.ps1 -Environment dev -ResourceGroupName rg-security-patrol-dev -Location eastus -AppServiceName app-security-patrol-dev

.EXAMPLE
    .\setup-monitoring.ps1 -DeploymentMethod ARM -Environment prod -ResourceGroupName rg-security-patrol-prod -SubscriptionId "00000000-0000-0000-0000-000000000000"

.NOTES
    This script requires the Az PowerShell module and appropriate Azure permissions.
    Run Connect-AzAccount before running this script if not already authenticated.
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [ValidateSet("ARM", "Bicep", "PowerShell")]
    [string]$DeploymentMethod = "ARM",

    [Parameter(Mandatory = $false)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment = "dev",

    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $false)]
    [string]$Location = "eastus",

    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId = "",

    [Parameter(Mandatory = $false)]
    [string]$AppServiceName = "",

    [Parameter(Mandatory = $false)]
    [string]$SqlServerName = "",

    [Parameter(Mandatory = $false)]
    [string]$SqlDatabaseName = "SecurityPatrolDb",

    [Parameter(Mandatory = $false)]
    [string]$StorageAccountName = "",

    [Parameter(Mandatory = $false)]
    [string]$AlertEmailAddress = "devops@example.com",

    [Parameter(Mandatory = $false)]
    [ValidateRange(30, 730)]
    [int]$AppInsightsRetentionDays = 90,

    [Parameter(Mandatory = $false)]
    [ValidateRange(30, 730)]
    [int]$LogAnalyticsRetentionDays = 30,

    [Parameter(Mandatory = $false)]
    [ValidateRange(100, 5000)]
    [int]$ApiResponseTimeThreshold = 1000,

    [Parameter(Mandatory = $false)]
    [ValidateRange(1, 100)]
    [int]$ApiFailureRateThreshold = 5,

    [Parameter(Mandatory = $false)]
    [string]$OutputFile = "./monitoring-config.json",

    [Parameter(Mandatory = $false)]
    [switch]$Verbose,

    [Parameter(Mandatory = $false)]
    [switch]$Force
)

# Set script variables
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path
$LOG_FILE = Join-Path $SCRIPT_DIR "setup-monitoring.log"
$VERBOSE = $PSBoundParameters.ContainsKey('Verbose')

# Log function
function Write-Log {
    param (
        [string]$level,
        [string]$message
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$level] $message"
    
    switch ($level) {
        "INFO" { 
            Write-Host $logMessage -ForegroundColor Green 
        }
        "WARNING" { 
            Write-Host $logMessage -ForegroundColor Yellow 
        }
        "ERROR" { 
            Write-Host $logMessage -ForegroundColor Red 
        }
        "DEBUG" { 
            if ($VERBOSE) {
                Write-Host $logMessage -ForegroundColor Cyan 
            }
        }
        default { 
            Write-Host $logMessage 
        }
    }
    
    Add-Content -Path $LOG_FILE -Value $logMessage
}

# Check dependencies
function Test-Dependencies {
    Write-Log "INFO" "Checking dependencies..."
    
    $modulesNeeded = @("Az", "Az.ApplicationInsights", "Az.Monitor", "Az.OperationalInsights")
    $allModulesPresent = $true
    
    foreach ($module in $modulesNeeded) {
        if (!(Get-Module -ListAvailable -Name $module)) {
            Write-Log "ERROR" "Module $module is not installed. Please install it using: Install-Module -Name $module -Force"
            $allModulesPresent = $false
        }
        else {
            Write-Log "DEBUG" "Module $module is installed."
        }
    }
    
    return $allModulesPresent
}

# Check Azure login
function Test-AzLogin {
    Write-Log "INFO" "Checking Azure login..."
    
    try {
        $context = Get-AzContext
        if (!$context) {
            Write-Log "INFO" "Not logged in to Azure. Prompting for login..."
            Connect-AzAccount
            $context = Get-AzContext
            if (!$context) {
                Write-Log "ERROR" "Failed to login to Azure."
                return $false
            }
        }
        
        Write-Log "INFO" "Logged in as $($context.Account) on subscription $($context.Subscription.Name)"
        return $true
    }
    catch {
        Write-Log "ERROR" "Error checking Azure login: $_"
        return $false
    }
}

# Select Azure subscription
function Select-AzSubscription {
    param (
        [string]$subscriptionId
    )
    
    if ([string]::IsNullOrEmpty($subscriptionId)) {
        Write-Log "INFO" "Using current subscription."
        return $true
    }
    
    try {
        Write-Log "INFO" "Setting Azure context to subscription $subscriptionId..."
        Set-AzContext -SubscriptionId $subscriptionId
        Write-Log "INFO" "Azure context set to subscription $subscriptionId."
        return $true
    }
    catch {
        Write-Log "ERROR" "Error setting subscription context: $_"
        return $false
    }
}

# Create or validate resource group
function New-ResourceGroup {
    param (
        [string]$resourceGroupName,
        [string]$location,
        [hashtable]$tags
    )
    
    try {
        # Check if resource group exists
        $rg = Get-AzResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue
        
        if ($rg) {
            Write-Log "INFO" "Resource group $resourceGroupName already exists."
        }
        else {
            Write-Log "INFO" "Creating resource group $resourceGroupName in $location..."
            $rg = New-AzResourceGroup -Name $resourceGroupName -Location $location -Tag $tags
            Write-Log "INFO" "Resource group $resourceGroupName created successfully."
        }
        
        return $rg
    }
    catch {
        Write-Log "ERROR" "Error creating or validating resource group: $_"
        return $null
    }
}

# Create or update Log Analytics workspace
function New-LogAnalyticsWorkspace {
    param (
        [string]$resourceGroupName,
        [string]$name,
        [string]$location,
        [int]$retentionDays,
        [hashtable]$tags
    )
    
    try {
        # Check if workspace exists
        $workspace = Get-AzOperationalInsightsWorkspace -ResourceGroupName $resourceGroupName -Name $name -ErrorAction SilentlyContinue
        
        if ($workspace -and !$Force) {
            Write-Log "INFO" "Log Analytics workspace $name already exists."
        }
        else {
            if ($workspace) {
                Write-Log "INFO" "Updating Log Analytics workspace $name..."
            }
            else {
                Write-Log "INFO" "Creating Log Analytics workspace $name in $location..."
            }
            
            $workspace = New-AzOperationalInsightsWorkspace -ResourceGroupName $resourceGroupName -Name $name -Location $location -Sku "PerGB2018" -RetentionInDays $retentionDays -Tag $tags -Force
            
            Write-Log "INFO" "Log Analytics workspace $name created/updated successfully."
        }
        
        return $workspace
    }
    catch {
        Write-Log "ERROR" "Error creating or updating Log Analytics workspace: $_"
        return $null
    }
}

# Create or update Application Insights
function New-ApplicationInsights {
    param (
        [string]$resourceGroupName,
        [string]$name,
        [string]$location,
        [string]$workspaceId,
        [int]$retentionDays,
        [hashtable]$tags
    )
    
    try {
        # Check if Application Insights exists
        $appInsights = Get-AzApplicationInsights -ResourceGroupName $resourceGroupName -Name $name -ErrorAction SilentlyContinue
        
        if ($appInsights -and !$Force) {
            Write-Log "INFO" "Application Insights $name already exists."
        }
        else {
            if ($appInsights) {
                Write-Log "INFO" "Updating Application Insights $name..."
                # Update Application Insights
                $appInsights = Set-AzApplicationInsights -ResourceGroupName $resourceGroupName -Name $name -Tag $tags
            }
            else {
                Write-Log "INFO" "Creating Application Insights $name in $location..."
                # Create Application Insights
                $appInsights = New-AzApplicationInsights -ResourceGroupName $resourceGroupName -Name $name -Location $location -Kind web -ApplicationType web -WorkspaceResourceId $workspaceId -RetentionInDays $retentionDays -Tag $tags
            }
            
            Write-Log "INFO" "Application Insights $name created/updated successfully."
        }
        
        return $appInsights
    }
    catch {
        Write-Log "ERROR" "Error creating or updating Application Insights: $_"
        return $null
    }
}

# Create or update Action Group
function New-ActionGroup {
    param (
        [string]$resourceGroupName,
        [string]$name,
        [string]$shortName,
        [string]$emailAddress,
        [hashtable]$tags
    )
    
    try {
        # Check if Action Group exists
        $actionGroup = Get-AzActionGroup -ResourceGroupName $resourceGroupName -Name $name -ErrorAction SilentlyContinue
        
        if ($actionGroup -and !$Force) {
            Write-Log "INFO" "Action Group $name already exists."
        }
        else {
            if ($actionGroup) {
                Write-Log "INFO" "Updating Action Group $name..."
                # Remove the existing action group
                Remove-AzActionGroup -ResourceGroupName $resourceGroupName -Name $name
            }
            else {
                Write-Log "INFO" "Creating Action Group $name..."
            }
            
            # Create or update action group
            $emailReceiver = New-AzActionGroupReceiver -Name "EmailReceiver" -EmailReceiver -EmailAddress $emailAddress
            $actionGroup = Set-AzActionGroup -ResourceGroupName $resourceGroupName -Name $name -ShortName $shortName -Receiver $emailReceiver -Tag $tags
            
            Write-Log "INFO" "Action Group $name created/updated successfully."
        }
        
        return $actionGroup
    }
    catch {
        Write-Log "ERROR" "Error creating or updating Action Group: $_"
        return $null
    }
}

# Create or update Metric Alert
function New-MetricAlert {
    param (
        [string]$resourceGroupName,
        [string]$name,
        [string]$description,
        [int]$severity,
        [string]$resourceId,
        [string]$metricName,
        [string]$operator,
        [double]$threshold,
        [string]$aggregation,
        [string]$actionGroupId,
        [hashtable]$tags
    )
    
    try {
        # Check if Metric Alert exists
        $alert = Get-AzMetricAlertRuleV2 -ResourceGroupName $resourceGroupName -Name $name -ErrorAction SilentlyContinue
        
        if ($alert -and !$Force) {
            Write-Log "INFO" "Metric Alert $name already exists."
        }
        else {
            if ($alert) {
                Write-Log "INFO" "Updating Metric Alert $name..."
                # Remove existing alert
                Remove-AzMetricAlertRuleV2 -ResourceGroupName $resourceGroupName -Name $name
            }
            else {
                Write-Log "INFO" "Creating Metric Alert $name..."
            }
            
            # Create alert criteria
            $criteria = New-AzMetricAlertRuleV2Criteria -MetricName $metricName -TimeAggregation $aggregation -Operator $operator -Threshold $threshold
            
            # Create or update alert
            $alert = Add-AzMetricAlertRuleV2 -ResourceGroupName $resourceGroupName -Name $name -Description $description -Severity $severity -TargetResourceId $resourceId -Criterion $criteria -ActionGroupId $actionGroupId -WindowSize 00:15:00 -Frequency 00:05:00 -Tag $tags
            
            Write-Log "INFO" "Metric Alert $name created/updated successfully."
        }
        
        return $alert
    }
    catch {
        Write-Log "ERROR" "Error creating or updating Metric Alert: $_"
        return $null
    }
}

# Create or update Availability Test (Web Test)
function New-AvailabilityTest {
    param (
        [string]$resourceGroupName,
        [string]$name,
        [string]$appInsightsId,
        [string]$appInsightsName,
        [string]$url,
        [int]$frequency,
        [hashtable]$tags
    )
    
    try {
        # Unfortunately, there's no direct PowerShell cmdlet for web tests in the Az module
        # We'll use ARM template deployment for this
        
        Write-Log "INFO" "Creating/updating availability test $name for URL $url..."
        
        $templateJson = @{
            '$schema'      = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#'
            contentVersion = '1.0.0.0'
            resources      = @(
                @{
                    type       = 'Microsoft.Insights/webtests'
                    apiVersion = '2022-06-15'
                    name       = $name
                    location   = (Get-AzResourceGroup -Name $resourceGroupName).Location
                    tags       = $tags + @{
                        "hidden-link:$appInsightsId" = 'Resource'
                    }
                    properties = @{
                        SyntheticMonitorId = $name
                        Name               = "API Health Check"
                        Description        = "Checks the health of the Security Patrol API"
                        Enabled            = $true
                        Frequency          = $frequency
                        Timeout            = 30
                        Kind               = "ping"
                        RetryEnabled       = $true
                        Locations          = @(
                            @{ Id = "us-ca-sjc-azr" },
                            @{ Id = "us-tx-sn1-azr" },
                            @{ Id = "us-il-ch1-azr" },
                            @{ Id = "us-va-ash-azr" },
                            @{ Id = "us-fl-mia-edge" }
                        )
                        Configuration      = @{
                            WebTest = "<WebTest xmlns=`"http://microsoft.com/schemas/VisualStudio/TeamTest/2010`" Name=`"API Health Check`"><Items><Request Method=`"GET`" Url=`"$url`" /></Items></WebTest>"
                        }
                    }
                }
            )
        }
        
        $templateFile = [System.IO.Path]::GetTempFileName()
        $templateJson | ConvertTo-Json -Depth 10 | Set-Content -Path $templateFile
        
        # Deploy the template
        $deploymentName = "webtest-deployment-$(Get-Date -Format 'yyyyMMddHHmmss')"
        $deployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $deploymentName -TemplateFile $templateFile
        
        # Clean up the temp file
        Remove-Item -Path $templateFile -Force
        
        Write-Log "INFO" "Availability test $name created/updated successfully."
        
        return $deployment
    }
    catch {
        Write-Log "ERROR" "Error creating or updating Availability Test: $_"
        return $null
    }
}

# Create or update Diagnostic Settings
function New-DiagnosticSetting {
    param (
        [string]$resourceId,
        [string]$name,
        [string]$workspaceId,
        [array]$logCategories,
        [bool]$enableMetrics = $true
    )
    
    try {
        # Check if diagnostic setting exists
        $diagnosticSetting = Get-AzDiagnosticSetting -ResourceId $resourceId -Name $name -ErrorAction SilentlyContinue
        
        if ($diagnosticSetting -and !$Force) {
            Write-Log "INFO" "Diagnostic Setting $name already exists for resource $resourceId."
        }
        else {
            if ($diagnosticSetting) {
                Write-Log "INFO" "Updating Diagnostic Setting $name..."
                # Remove existing diagnostic setting
                Remove-AzDiagnosticSetting -ResourceId $resourceId -Name $name
            }
            else {
                Write-Log "INFO" "Creating Diagnostic Setting $name for resource $resourceId..."
            }
            
            $logs = @()
            foreach ($category in $logCategories) {
                $logs += @{
                    Category = $category
                    Enabled  = $true
                }
            }
            
            $metrics = @()
            if ($enableMetrics) {
                $metrics += @{
                    Category = "AllMetrics"
                    Enabled  = $true
                }
            }
            
            # Create or update diagnostic setting
            $parameters = @{
                ResourceId      = $resourceId
                Name            = $name
                WorkspaceId     = $workspaceId
                Enabled         = $true
            }
            
            if ($logs.Count -gt 0) {
                $parameters.Add("Log", $logs)
            }
            
            if ($metrics.Count -gt 0) {
                $parameters.Add("Metric", $metrics)
            }
            
            $diagnosticSetting = Set-AzDiagnosticSetting @parameters
            
            Write-Log "INFO" "Diagnostic Setting $name created/updated successfully."
        }
        
        return $diagnosticSetting
    }
    catch {
        Write-Log "ERROR" "Error creating or updating Diagnostic Setting: $_"
        return $null
    }
}

# Create or update Dashboard
function New-Dashboard {
    param (
        [string]$resourceGroupName,
        [string]$name,
        [string]$location,
        [string]$appInsightsId,
        [string]$dashboardType,
        [hashtable]$tags
    )
    
    try {
        # Check if dashboard exists
        $dashboard = Get-AzPortalDashboard -ResourceGroupName $resourceGroupName -Name $name -ErrorAction SilentlyContinue
        
        if ($dashboard -and !$Force) {
            Write-Log "INFO" "Dashboard $name already exists."
        }
        else {
            if ($dashboard) {
                Write-Log "INFO" "Updating Dashboard $name..."
                # Remove existing dashboard
                Remove-AzPortalDashboard -ResourceGroupName $resourceGroupName -Name $name
            }
            else {
                Write-Log "INFO" "Creating Dashboard $name..."
            }
            
            # Create dashboard definition based on type
            $dashboardProperties = @{
                lenses = @{
                    "0" = @{
                        order = 0
                        parts = @{}
                    }
                }
            }
            
            # Add different tiles based on dashboard type
            switch ($dashboardType) {
                "Executive" {
                    $dashboardProperties.lenses."0".parts."0" = @{
                        position = @{
                            x       = 0
                            y       = 0
                            colSpan = 6
                            rowSpan = 4
                        }
                        metadata = @{
                            inputs = @(
                                @{
                                    name  = "ComponentId"
                                    value = $appInsightsId
                                }
                            )
                            type    = "Extension/AppInsightsExtension/PartType/AppMapGalPt"
                            settings = @{}
                        }
                    }
                    
                    $dashboardProperties.lenses."0".parts."1" = @{
                        position = @{
                            x       = 6
                            y       = 0
                            colSpan = 6
                            rowSpan = 4
                        }
                        metadata = @{
                            inputs = @(
                                @{
                                    name  = "ComponentId"
                                    value = $appInsightsId
                                }
                            )
                            type    = "Extension/AppInsightsExtension/PartType/AvailabilityNavButtonGalleryPt"
                            settings = @{}
                        }
                    }
                }
                "Technical" {
                    $dashboardProperties.lenses."0".parts."0" = @{
                        position = @{
                            x       = 0
                            y       = 0
                            colSpan = 6
                            rowSpan = 4
                        }
                        metadata = @{
                            inputs = @(
                                @{
                                    name  = "ComponentId"
                                    value = $appInsightsId
                                }
                            )
                            type    = "Extension/AppInsightsExtension/PartType/PerformanceNavButtonGalleryPt"
                            settings = @{}
                        }
                    }
                    
                    $dashboardProperties.lenses."0".parts."1" = @{
                        position = @{
                            x       = 6
                            y       = 0
                            colSpan = 6
                            rowSpan = 4
                        }
                        metadata = @{
                            inputs = @(
                                @{
                                    name  = "ComponentId"
                                    value = $appInsightsId
                                }
                            )
                            type    = "Extension/AppInsightsExtension/PartType/FailuresNavButtonGalleryPt"
                            settings = @{}
                        }
                    }
                }
                "Operations" {
                    $dashboardProperties.lenses."0".parts."0" = @{
                        position = @{
                            x       = 0
                            y       = 0
                            colSpan = 6
                            rowSpan = 4
                        }
                        metadata = @{
                            inputs = @(
                                @{
                                    name  = "ComponentId"
                                    value = $appInsightsId
                                }
                            )
                            type    = "Extension/AppInsightsExtension/PartType/UsageNavButtonPart"
                            settings = @{}
                        }
                    }
                    
                    $dashboardProperties.lenses."0".parts."1" = @{
                        position = @{
                            x       = 6
                            y       = 0
                            colSpan = 6
                            rowSpan = 4
                        }
                        metadata = @{
                            inputs = @(
                                @{
                                    name  = "ComponentId"
                                    value = $appInsightsId
                                }
                            )
                            type    = "Extension/AppInsightsExtension/PartType/UsageUsersGalleryPart"
                            settings = @{}
                        }
                    }
                }
                default {
                    # Default dashboard with basic monitoring
                    $dashboardProperties.lenses."0".parts."0" = @{
                        position = @{
                            x       = 0
                            y       = 0
                            colSpan = 6
                            rowSpan = 4
                        }
                        metadata = @{
                            inputs = @(
                                @{
                                    name  = "ComponentId"
                                    value = $appInsightsId
                                }
                            )
                            type    = "Extension/AppInsightsExtension/PartType/OverviewPart"
                            settings = @{}
                        }
                    }
                }
            }
            
            # Create or update dashboard
            $dashboard = New-AzPortalDashboard -ResourceGroupName $resourceGroupName -Name $name -Location $location -DashboardJson ($dashboardProperties | ConvertTo-Json -Depth 10) -Tag $tags
            
            Write-Log "INFO" "Dashboard $name created/updated successfully."
        }
        
        return $dashboard
    }
    catch {
        Write-Log "ERROR" "Error creating or updating Dashboard: $_"
        return $null
    }
}

# Export monitoring configuration to JSON
function Export-MonitoringConfig {
    param (
        [string]$appInsightsId,
        [string]$appInsightsKey,
        [string]$workspaceId,
        [string]$outputFile
    )
    
    try {
        Write-Log "INFO" "Exporting monitoring configuration to $outputFile..."
        
        $config = @{
            appInsightsId           = $appInsightsId
            appInsightsKey          = $appInsightsKey
            workspaceId             = $workspaceId
            exportDate              = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
            environment             = $Environment
        }
        
        $config | ConvertTo-Json -Depth 10 | Set-Content -Path $outputFile
        
        Write-Log "INFO" "Monitoring configuration exported successfully to $outputFile."
        return $true
    }
    catch {
        Write-Log "ERROR" "Error exporting monitoring configuration: $_"
        return $false
    }
}

# Deploy using ARM template
function Deploy-ArmTemplate {
    param (
        [string]$resourceGroupName,
        [string]$templateFile,
        [hashtable]$parameters
    )
    
    try {
        Write-Log "INFO" "Deploying monitoring resources using ARM template..."
        
        # Use the ARM template
        $armTemplatePath = Join-Path $SCRIPT_DIR "../azure/arm-templates/monitoring.json"
        
        # Check if template exists
        if (!(Test-Path $armTemplatePath)) {
            Write-Log "ERROR" "ARM template not found at $armTemplatePath"
            return $null
        }
        
        # Deploy the template
        $deploymentName = "monitoring-deployment-$(Get-Date -Format 'yyyyMMddHHmmss')"
        $deployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $deploymentName -TemplateFile $armTemplatePath -TemplateParameterObject $parameters
        
        Write-Log "INFO" "ARM template deployment completed successfully."
        return $deployment
    }
    catch {
        Write-Log "ERROR" "Error deploying ARM template: $_"
        return $null
    }
}

# Deploy using Bicep template
function Deploy-BicepTemplate {
    param (
        [string]$resourceGroupName,
        [string]$templateFile,
        [hashtable]$parameters
    )
    
    try {
        Write-Log "INFO" "Deploying monitoring resources using Bicep template..."
        
        # Use the Bicep template
        $bicepTemplatePath = Join-Path $SCRIPT_DIR "../azure/bicep/monitoring.bicep"
        
        # Check if template exists
        if (!(Test-Path $bicepTemplatePath)) {
            Write-Log "ERROR" "Bicep template not found at $bicepTemplatePath"
            return $null
        }
        
        # Deploy the template
        $deploymentName = "monitoring-deployment-$(Get-Date -Format 'yyyyMMddHHmmss')"
        $deployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $deploymentName -TemplateFile $bicepTemplatePath -TemplateParameterObject $parameters
        
        Write-Log "INFO" "Bicep template deployment completed successfully."
        return $deployment
    }
    catch {
        Write-Log "ERROR" "Error deploying Bicep template: $_"
        return $null
    }
}

# Clean up function
function Invoke-Cleanup {
    Write-Log "INFO" "Performing cleanup operations..."
    # Remove temporary files if any
    
    Write-Log "INFO" "Cleanup completed."
}

# Main script execution
try {
    Write-Log "INFO" "Starting monitoring setup for Security Patrol Application in $Environment environment..."
    
    # Check dependencies
    if (!(Test-Dependencies)) {
        Write-Log "ERROR" "Missing dependencies. Please install required PowerShell modules."
        exit 1
    }
    
    # Check Azure login
    if (!(Test-AzLogin)) {
        Write-Log "ERROR" "Azure login failed. Please login manually using Connect-AzAccount and try again."
        exit 1
    }
    
    # Select subscription if provided
    if (![string]::IsNullOrEmpty($SubscriptionId)) {
        if (!(Select-AzSubscription -subscriptionId $SubscriptionId)) {
            Write-Log "ERROR" "Failed to select subscription $SubscriptionId."
            exit 1
        }
    }
    
    # Create or validate resource group
    $tags = @{
        Application = "SecurityPatrol"
        Environment = $Environment
        Component   = "Monitoring"
        ManagedBy   = "PowerShell"
    }
    
    $resourceGroup = New-ResourceGroup -resourceGroupName $ResourceGroupName -location $Location -tags $tags
    if (!$resourceGroup) {
        Write-Log "ERROR" "Failed to create or validate resource group."
        exit 1
    }
    
    # Determine deployment method
    if ($DeploymentMethod -eq "ARM") {
        # Deploy using ARM template
        $workspaceName = "law-security-patrol-$Environment"
        $appInsightsName = "ai-security-patrol-$Environment"
        $actionGroupName = "ag-security-patrol-$Environment"
        $dashboardName = "dashboard-security-patrol-$Environment"
        
        # Get App Service ID if name provided
        $appServiceId = ""
        if (![string]::IsNullOrEmpty($AppServiceName)) {
            $appService = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -ErrorAction SilentlyContinue
            if ($appService) {
                $appServiceId = $appService.Id
            }
            else {
                Write-Log "WARNING" "App Service $AppServiceName not found. Will proceed without App Service monitoring."
            }
        }
        
        # Get SQL Database ID if name provided
        $sqlDatabaseId = ""
        if (![string]::IsNullOrEmpty($SqlServerName) -and ![string]::IsNullOrEmpty($SqlDatabaseName)) {
            $sqlDatabase = Get-AzSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -DatabaseName $SqlDatabaseName -ErrorAction SilentlyContinue
            if ($sqlDatabase) {
                $sqlDatabaseId = $sqlDatabase.ResourceId
            }
            else {
                Write-Log "WARNING" "SQL Database $SqlDatabaseName on server $SqlServerName not found. Will proceed without SQL monitoring."
            }
        }
        
        # Get Storage Account ID if name provided
        $storageAccountId = ""
        if (![string]::IsNullOrEmpty($StorageAccountName)) {
            $storageAccount = Get-AzStorageAccount -ResourceGroupName $ResourceGroupName -Name $StorageAccountName -ErrorAction SilentlyContinue
            if ($storageAccount) {
                $storageAccountId = $storageAccount.Id
            }
            else {
                Write-Log "WARNING" "Storage Account $StorageAccountName not found. Will proceed without Storage Account monitoring."
            }
        }
        
        # Prepare parameters for ARM template
        $armParameters = @{
            resourceGroupName        = $ResourceGroupName
            location                 = $Location
            environment              = $Environment
            appServiceId             = $appServiceId
            appInsightsName          = $appInsightsName
            logAnalyticsWorkspaceName = $workspaceName
            actionGroupName          = $actionGroupName
            alertEmailAddresses      = @($AlertEmailAddress)
            dashboardName            = $dashboardName
            appInsightsRetentionDays = $AppInsightsRetentionDays
            logAnalyticsRetentionDays = $LogAnalyticsRetentionDays
            apiResponseTimeThresholdMs = $ApiResponseTimeThreshold
            apiFailureRateThreshold  = $ApiFailureRateThreshold
            tags                     = $tags
        }
        
        # Deploy the ARM template
        $deployment = Deploy-ArmTemplate -resourceGroupName $ResourceGroupName -templateFile $armTemplatePath -parameters $armParameters
        
        if (!$deployment) {
            Write-Log "ERROR" "ARM template deployment failed."
            exit 1
        }
        
        # Extract outputs from deployment
        $appInsightsId = $deployment.Outputs.appInsightsId.Value
        $appInsightsKey = $deployment.Outputs.appInsightsKey.Value
        $workspaceId = $deployment.Outputs.logAnalyticsWorkspaceId.Value
        
        # Export the monitoring configuration
        Export-MonitoringConfig -appInsightsId $appInsightsId -appInsightsKey $appInsightsKey -workspaceId $workspaceId -outputFile $OutputFile
        
    }
    elseif ($DeploymentMethod -eq "Bicep") {
        # Deploy using Bicep template
        $workspaceName = "law-security-patrol-$Environment"
        $appInsightsName = "ai-security-patrol-$Environment"
        $actionGroupName = "ag-security-patrol-$Environment"
        $dashboardName = "dashboard-security-patrol-$Environment"
        
        # Get App Service ID if name provided
        $appServiceId = ""
        if (![string]::IsNullOrEmpty($AppServiceName)) {
            $appService = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -ErrorAction SilentlyContinue
            if ($appService) {
                $appServiceId = $appService.Id
            }
            else {
                Write-Log "WARNING" "App Service $AppServiceName not found. Will proceed without App Service monitoring."
            }
        }
        
        # Prepare parameters for Bicep template
        $bicepParameters = @{
            location                 = $Location
            environment              = $Environment
            appServiceId             = $appServiceId
            appInsightsName          = $appInsightsName
            logAnalyticsWorkspaceName = $workspaceName
            actionGroupName          = $actionGroupName
            alertEmailAddresses      = @($AlertEmailAddress)
            dashboardName            = $dashboardName
            appInsightsRetentionDays = $AppInsightsRetentionDays
            logAnalyticsRetentionDays = $LogAnalyticsRetentionDays
            apiResponseTimeThresholdMs = $ApiResponseTimeThreshold
            apiFailureRateThreshold  = $ApiFailureRateThreshold
            tags                     = $tags
        }
        
        # Deploy the Bicep template
        $deployment = Deploy-BicepTemplate -resourceGroupName $ResourceGroupName -templateFile $bicepTemplatePath -parameters $bicepParameters
        
        if (!$deployment) {
            Write-Log "ERROR" "Bicep template deployment failed."
            exit 1
        }
        
        # Extract outputs from deployment
        $appInsightsId = $deployment.Outputs.appInsightsId.Value
        $appInsightsKey = $deployment.Outputs.appInsightsKey.Value
        $workspaceId = $deployment.Outputs.logAnalyticsWorkspaceId.Value
        
        # Export the monitoring configuration
        Export-MonitoringConfig -appInsightsId $appInsightsId -appInsightsKey $appInsightsKey -workspaceId $workspaceId -outputFile $OutputFile
        
    }
    else {
        # Deploy using PowerShell commands
        Write-Log "INFO" "Deploying monitoring resources using PowerShell commands..."
        
        # Generate resource names
        $workspaceName = "law-security-patrol-$Environment"
        $appInsightsName = "ai-security-patrol-$Environment"
        $actionGroupName = "ag-security-patrol-$Environment"
        $dashboardName = "dashboard-security-patrol-$Environment"
        $webTestName = "webtest-security-patrol-$Environment"
        
        # Create Log Analytics workspace
        $workspace = New-LogAnalyticsWorkspace -resourceGroupName $ResourceGroupName -name $workspaceName -location $Location -retentionDays $LogAnalyticsRetentionDays -tags $tags
        if (!$workspace) {
            Write-Log "ERROR" "Failed to create Log Analytics workspace."
            exit 1
        }
        
        # Create Application Insights
        $appInsights = New-ApplicationInsights -resourceGroupName $ResourceGroupName -name $appInsightsName -location $Location -workspaceId $workspace.ResourceId -retentionDays $AppInsightsRetentionDays -tags $tags
        if (!$appInsights) {
            Write-Log "ERROR" "Failed to create Application Insights."
            exit 1
        }
        
        # Create Action Group
        $actionGroup = New-ActionGroup -resourceGroupName $ResourceGroupName -name $actionGroupName -shortName "SecPatrol" -emailAddress $AlertEmailAddress -tags $tags
        if (!$actionGroup) {
            Write-Log "ERROR" "Failed to create Action Group."
            exit 1
        }
        
        # Create API alerts if App Service is provided
        if (![string]::IsNullOrEmpty($AppServiceName)) {
            # Get App Service
            $appService = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -ErrorAction SilentlyContinue
            if ($appService) {
                # Create API response time alert
                $responseTimeAlert = New-MetricAlert -resourceGroupName $ResourceGroupName -name "alert-api-response-time-$Environment" -description "Alert when API response time exceeds $ApiResponseTimeThreshold ms" -severity 2 -resourceId $appService.Id -metricName "HttpResponseTime" -operator "GreaterThan" -threshold $ApiResponseTimeThreshold -aggregation "Average" -actionGroupId $actionGroup.Id -tags $tags
                
                # Create API failure rate alert
                $failureRateAlert = New-MetricAlert -resourceGroupName $ResourceGroupName -name "alert-api-failure-rate-$Environment" -description "Alert when API failure rate exceeds $ApiFailureRateThreshold%" -severity 1 -resourceId $appService.Id -metricName "Http5xx" -operator "GreaterThan" -threshold $ApiFailureRateThreshold -aggregation "Total" -actionGroupId $actionGroup.Id -tags $tags
                
                # Create CPU usage alert
                $cpuAlert = New-MetricAlert -resourceGroupName $ResourceGroupName -name "alert-cpu-usage-$Environment" -description "Alert when CPU usage exceeds 80%" -severity 2 -resourceId $appService.Id -metricName "CpuPercentage" -operator "GreaterThan" -threshold 80 -aggregation "Average" -actionGroupId $actionGroup.Id -tags $tags
                
                # Create memory usage alert
                $memoryAlert = New-MetricAlert -resourceGroupName $ResourceGroupName -name "alert-memory-usage-$Environment" -description "Alert when memory usage exceeds 80%" -severity 2 -resourceId $appService.Id -metricName "MemoryPercentage" -operator "GreaterThan" -threshold 80 -aggregation "Average" -actionGroupId $actionGroup.Id -tags $tags
                
                # Create availability test (web test)
                $url = "https://$($appService.DefaultHostName)/health"
                $availabilityTest = New-AvailabilityTest -resourceGroupName $ResourceGroupName -name $webTestName -appInsightsId $appInsights.Id -appInsightsName $appInsightsName -url $url -frequency 300 -tags $tags
                
                # Create diagnostic settings for App Service
                $appServiceDiag = New-DiagnosticSetting -resourceId $appService.Id -name "diag-$AppServiceName" -workspaceId $workspace.ResourceId -logCategories @("AppServiceHTTPLogs", "AppServiceConsoleLogs", "AppServiceAppLogs", "AppServiceAuditLogs") -enableMetrics $true
            }
            else {
                Write-Log "WARNING" "App Service $AppServiceName not found. Skipping App Service monitoring setup."
            }
        }
        
        # Create SQL database alerts and diagnostic settings if SQL Server and Database are provided
        if (![string]::IsNullOrEmpty($SqlServerName) -and ![string]::IsNullOrEmpty($SqlDatabaseName)) {
            # Get SQL Database
            $sqlDatabase = Get-AzSqlDatabase -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -DatabaseName $SqlDatabaseName -ErrorAction SilentlyContinue
            if ($sqlDatabase) {
                # Create DTU usage alert
                $dtuAlert = New-MetricAlert -resourceGroupName $ResourceGroupName -name "alert-sql-dtu-$Environment" -description "Alert when DTU usage exceeds 80%" -severity 2 -resourceId $sqlDatabase.ResourceId -metricName "dtu_consumption_percent" -operator "GreaterThan" -threshold 80 -aggregation "Average" -actionGroupId $actionGroup.Id -tags $tags
                
                # Create storage usage alert
                $storageAlert = New-MetricAlert -resourceGroupName $ResourceGroupName -name "alert-sql-storage-$Environment" -description "Alert when storage usage exceeds 80%" -severity 2 -resourceId $sqlDatabase.ResourceId -metricName "storage_percent" -operator "GreaterThan" -threshold 80 -aggregation "Average" -actionGroupId $actionGroup.Id -tags $tags
                
                # Create diagnostic settings for SQL Database
                $sqlDiag = New-DiagnosticSetting -resourceId $sqlDatabase.ResourceId -name "diag-$SqlDatabaseName" -workspaceId $workspace.ResourceId -logCategories @("SQLInsights", "AutomaticTuning", "QueryStoreRuntimeStatistics", "QueryStoreWaitStatistics", "Errors", "DatabaseWaitStatistics", "Timeouts", "Blocks", "Deadlocks") -enableMetrics $true
            }
            else {
                Write-Log "WARNING" "SQL Database $SqlDatabaseName on server $SqlServerName not found. Skipping SQL monitoring setup."
            }
        }
        
        # Create Storage Account alerts and diagnostic settings if Storage Account is provided
        if (![string]::IsNullOrEmpty($StorageAccountName)) {
            # Get Storage Account
            $storageAccount = Get-AzStorageAccount -ResourceGroupName $ResourceGroupName -Name $StorageAccountName -ErrorAction SilentlyContinue
            if ($storageAccount) {
                # Create availability alert
                $availabilityAlert = New-MetricAlert -resourceGroupName $ResourceGroupName -name "alert-storage-availability-$Environment" -description "Alert when storage availability drops below 99.9%" -severity 1 -resourceId $storageAccount.Id -metricName "Availability" -operator "LessThan" -threshold 99.9 -aggregation "Average" -actionGroupId $actionGroup.Id -tags $tags
                
                # Create latency alert
                $latencyAlert = New-MetricAlert -resourceGroupName $ResourceGroupName -name "alert-storage-latency-$Environment" -description "Alert when storage latency exceeds 1000ms" -severity 2 -resourceId $storageAccount.Id -metricName "SuccessE2ELatency" -operator "GreaterThan" -threshold 1000 -aggregation "Average" -actionGroupId $actionGroup.Id -tags $tags
                
                # Create capacity alert
                $capacityAlert = New-MetricAlert -resourceGroupName $ResourceGroupName -name "alert-storage-capacity-$Environment" -description "Alert when storage usage exceeds 80%" -severity 2 -resourceId $storageAccount.Id -metricName "UsedCapacity" -operator "GreaterThan" -threshold ($storageAccount.PrimaryEndpoints.Blob.Length * 0.8) -aggregation "Average" -actionGroupId $actionGroup.Id -tags $tags
                
                # Create diagnostic settings for Storage Account
                $storageDiag = New-DiagnosticSetting -resourceId $storageAccount.Id -name "diag-$StorageAccountName" -workspaceId $workspace.ResourceId -enableMetrics $true
                
                # Create diagnostic settings for Blob Service
                $blobServiceResourceId = "$($storageAccount.Id)/blobServices/default"
                $blobDiag = New-DiagnosticSetting -resourceId $blobServiceResourceId -name "diag-$StorageAccountName-blob" -workspaceId $workspace.ResourceId -logCategories @("StorageRead", "StorageWrite", "StorageDelete") -enableMetrics $true
            }
            else {
                Write-Log "WARNING" "Storage Account $StorageAccountName not found. Skipping Storage monitoring setup."
            }
        }
        
        # Create dashboards
        $executiveDashboard = New-Dashboard -resourceGroupName $ResourceGroupName -name "$dashboardName-executive" -location $Location -appInsightsId $appInsights.Id -dashboardType "Executive" -tags $tags
        $technicalDashboard = New-Dashboard -resourceGroupName $ResourceGroupName -name "$dashboardName-technical" -location $Location -appInsightsId $appInsights.Id -dashboardType "Technical" -tags $tags
        $operationsDashboard = New-Dashboard -resourceGroupName $ResourceGroupName -name "$dashboardName-operations" -location $Location -appInsightsId $appInsights.Id -dashboardType "Operations" -tags $tags
        
        # Export monitoring configuration
        Export-MonitoringConfig -appInsightsId $appInsights.Id -appInsightsKey $appInsights.InstrumentationKey -workspaceId $workspace.ResourceId -outputFile $OutputFile
    }
    
    Write-Log "INFO" "Monitoring setup completed successfully for Security Patrol Application in $Environment environment."
    
    # Perform cleanup
    Invoke-Cleanup
    
    exit 0
}
catch {
    Write-Log "ERROR" "An unexpected error occurred: $_"
    Write-Log "ERROR" "Stack trace: $($_.ScriptStackTrace)"
    
    # Perform cleanup
    Invoke-Cleanup
    
    exit 1
}