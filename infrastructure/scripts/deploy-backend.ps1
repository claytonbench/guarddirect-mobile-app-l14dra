<#
.SYNOPSIS
    Deploys the Security Patrol backend services to Azure.

.DESCRIPTION
    This script automates the build, containerization, and deployment of the Security Patrol
    backend API services to Azure App Service. It handles infrastructure configuration,
    container image building and pushing, environment variable setup, and deployment validation.

.PARAMETER Environment
    Target environment for deployment (dev, test, staging, prod).

.PARAMETER SubscriptionId
    Azure subscription ID where deployment will occur.

.PARAMETER ResourceGroup
    (Optional) Azure resource group name. If not specified, will be retrieved from Terraform output.

.PARAMETER TerraformDir
    Path to Terraform directory containing state. Default is '../terraform'.

.PARAMETER BuildOnly
    If specified, only builds the Docker image without deploying.

.PARAMETER SkipBuild
    If specified, skips building the Docker image and uses existing image.

.PARAMETER ImageTag
    Tag for the Docker image. Default is 'latest'.

.PARAMETER DeployToSlot
    If specified, deploys to staging slot instead of production slot.

.PARAMETER SwapAfterDeployment
    If specified, swaps staging slot to production after successful deployment.

.PARAMETER Verbose
    Enable verbose logging.

.EXAMPLE
    ./deploy-backend.ps1 -Environment dev -SubscriptionId "12345678-1234-1234-1234-1234567890ab"

.EXAMPLE
    ./deploy-backend.ps1 -Environment prod -SubscriptionId "12345678-1234-1234-1234-1234567890ab" -DeployToSlot -SwapAfterDeployment

.NOTES
    Author: Security Patrol Development Team
    Last Update: 2023-06-01
    Requires: Az PowerShell module, Docker CLI, Terraform CLI
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "test", "staging", "prod")]
    [string]$Environment,

    [Parameter(Mandatory=$true)]
    [ValidatePattern("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$")]
    [string]$SubscriptionId,

    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "",

    [Parameter(Mandatory=$false)]
    [ValidateScript({Test-Path $_ -PathType Container})]
    [string]$TerraformDir = "../terraform",

    [Parameter(Mandatory=$false)]
    [switch]$BuildOnly,

    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,

    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest",

    [Parameter(Mandatory=$false)]
    [switch]$DeployToSlot,

    [Parameter(Mandatory=$false)]
    [switch]$SwapAfterDeployment,

    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

#region Script Initialization

# Get the directory of the current script
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Definition
$LOG_FILE = Join-Path $SCRIPT_DIR "deploy-backend-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$VERBOSE = $PSBoundParameters.ContainsKey('Verbose')

# Security Patrol container repository and image naming convention
$IMAGE_REPOSITORY = "securitypatrol"
$IMAGE_NAME = "security-patrol-api"
$FULL_IMAGE_NAME = "$IMAGE_REPOSITORY/$IMAGE_NAME"

#endregion

#region Logging Functions

function Write-Log {
    <#
    .SYNOPSIS
        Writes a log message with timestamp and severity level.
    .PARAMETER Message
        The message to log.
    .PARAMETER Level
        The severity level of the message (INFO, WARNING, ERROR, DEBUG).
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true, Position=0)]
        [string]$Message,

        [Parameter(Mandatory=$false, Position=1)]
        [ValidateSet("INFO", "WARNING", "ERROR", "DEBUG")]
        [string]$Level = "INFO"
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    # Write to console with appropriate colors
    switch ($Level) {
        "INFO" { Write-Host $logMessage -ForegroundColor White }
        "WARNING" { Write-Host $logMessage -ForegroundColor Yellow }
        "ERROR" { Write-Host $logMessage -ForegroundColor Red }
        "DEBUG" { 
            if ($VERBOSE) {
                Write-Host $logMessage -ForegroundColor Gray
            }
        }
        default { Write-Host $logMessage }
    }
    
    # Write to log file
    Add-Content -Path $LOG_FILE -Value $logMessage
}

#endregion

#region Dependency Functions

function Test-Dependencies {
    <#
    .SYNOPSIS
        Checks if required dependencies are installed.
    .RETURNS
        True if all dependencies are installed, false otherwise.
    #>
    [CmdletBinding()]
    param()

    $allDependenciesPresent = $true

    # Check for Az PowerShell module
    Write-Log "Checking for Az PowerShell module..." "DEBUG"
    if (-not (Get-Module -ListAvailable -Name Az.Accounts)) {
        Write-Log "Az PowerShell module is not installed. Install it using: Install-Module -Name Az -AllowClobber" "ERROR"
        $allDependenciesPresent = $false
    }
    else {
        Write-Log "Az PowerShell module is installed." "DEBUG"
    }

    # Check for Docker
    Write-Log "Checking for Docker..." "DEBUG"
    try {
        $dockerVersion = docker --version
        Write-Log "Docker is installed: $dockerVersion" "DEBUG"
    }
    catch {
        Write-Log "Docker is not installed or not in PATH. Please install Docker." "ERROR"
        $allDependenciesPresent = $false
    }

    # Check for Terraform
    Write-Log "Checking for Terraform..." "DEBUG"
    try {
        $terraformVersion = terraform --version
        Write-Log "Terraform is installed: $($terraformVersion -split '\n' | Select-Object -First 1)" "DEBUG"
    }
    catch {
        Write-Log "Terraform is not installed or not in PATH. Please install Terraform." "ERROR"
        $allDependenciesPresent = $false
    }

    return $allDependenciesPresent
}

#endregion

#region Azure Authentication Functions

function Test-AzLogin {
    <#
    .SYNOPSIS
        Checks if the user is logged into Azure and prompts for login if needed.
    .RETURNS
        True if login is successful, false otherwise.
    #>
    [CmdletBinding()]
    param()

    Write-Log "Checking Azure login status..." "DEBUG"
    
    try {
        $context = Get-AzContext
        if ($null -eq $context) {
            Write-Log "Not logged in to Azure. Initiating login..." "INFO"
            Connect-AzAccount -ErrorAction Stop
            $context = Get-AzContext
            if ($null -eq $context) {
                Write-Log "Failed to log in to Azure." "ERROR"
                return $false
            }
        }
        else {
            Write-Log "Already logged in to Azure as $($context.Account.Id) on subscription $($context.Subscription.Name)" "INFO"
        }
        return $true
    }
    catch {
        Write-Log "Error checking Azure login status: $_" "ERROR"
        return $false
    }
}

function Select-AzSubscription {
    <#
    .SYNOPSIS
        Selects the specified Azure subscription.
    .PARAMETER SubscriptionId
        The ID of the subscription to select.
    .RETURNS
        True if subscription selection is successful, false otherwise.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$SubscriptionId
    )

    Write-Log "Selecting Azure subscription: $SubscriptionId" "DEBUG"
    
    try {
        Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop
        $currentContext = Get-AzContext
        Write-Log "Successfully selected subscription: $($currentContext.Subscription.Name) ($($currentContext.Subscription.Id))" "INFO"
        return $true
    }
    catch {
        Write-Log "Error selecting subscription: $_" "ERROR"
        return $false
    }
}

#endregion

#region Terraform Functions

function Get-TerraformOutput {
    <#
    .SYNOPSIS
        Retrieves output values from Terraform state.
    .PARAMETER OutputName
        The name of the output value to retrieve.
    .PARAMETER TerraformDir
        The directory containing the Terraform configuration.
    .RETURNS
        The value of the specified Terraform output.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$OutputName,
        
        [Parameter(Mandatory=$true)]
        [string]$TerraformDir
    )

    Write-Log "Retrieving Terraform output: $OutputName" "DEBUG"
    
    try {
        Push-Location $TerraformDir
        $outputValue = terraform output -raw $OutputName 2>$null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Log "Failed to retrieve Terraform output $OutputName." "ERROR"
            $outputValue = $null
        }
        else {
            Write-Log "Successfully retrieved Terraform output $OutputName" "DEBUG"
        }
    }
    catch {
        Write-Log "Error retrieving Terraform output $OutputName: $_" "ERROR"
        $outputValue = $null
    }
    finally {
        Pop-Location
    }
    
    return $outputValue
}

#endregion

#region Docker Functions

function Build-DockerImage {
    <#
    .SYNOPSIS
        Builds the Docker image for the backend API.
    .PARAMETER ImageName
        The name of the Docker image.
    .PARAMETER ImageTag
        The tag for the Docker image.
    .PARAMETER DockerfilePath
        The path to the Dockerfile.
    .PARAMETER BuildContext
        The build context directory.
    .RETURNS
        True if build is successful, false otherwise.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ImageName,
        
        [Parameter(Mandatory=$true)]
        [string]$ImageTag,
        
        [Parameter(Mandatory=$true)]
        [string]$DockerfilePath,
        
        [Parameter(Mandatory=$true)]
        [string]$BuildContext
    )

    Write-Log "Building Docker image: $ImageName:$ImageTag" "INFO"
    
    try {
        Push-Location $BuildContext
        
        # Build the Docker image
        $buildArgs = @(
            "build",
            "-t", "$ImageName`:$ImageTag",
            "-f", $DockerfilePath,
            "--build-arg", "ASPNETCORE_ENVIRONMENT=$Environment",
            "--no-cache",
            "."
        )
        
        Write-Log "Running: docker $($buildArgs -join ' ')" "DEBUG"
        $buildProcess = Start-Process -FilePath "docker" -ArgumentList $buildArgs -NoNewWindow -PassThru -Wait
        
        if ($buildProcess.ExitCode -eq 0) {
            Write-Log "Docker image built successfully: $ImageName`:$ImageTag" "INFO"
            return $true
        }
        else {
            Write-Log "Failed to build Docker image. Exit code: $($buildProcess.ExitCode)" "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "Error building Docker image: $_" "ERROR"
        return $false
    }
    finally {
        Pop-Location
    }
}

function Push-DockerImageToACR {
    <#
    .SYNOPSIS
        Tags and pushes the Docker image to Azure Container Registry.
    .PARAMETER ImageName
        The name of the Docker image.
    .PARAMETER ImageTag
        The tag for the Docker image.
    .PARAMETER AcrName
        The name of the Azure Container Registry.
    .RETURNS
        True if push is successful, false otherwise.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ImageName,
        
        [Parameter(Mandatory=$true)]
        [string]$ImageTag,
        
        [Parameter(Mandatory=$true)]
        [string]$AcrName
    )

    Write-Log "Pushing Docker image to ACR: $AcrName" "INFO"
    
    try {
        # Login to ACR
        Write-Log "Logging in to ACR: $AcrName" "DEBUG"
        $loginProcess = Start-Process -FilePath "az" -ArgumentList "acr", "login", "--name", $AcrName -NoNewWindow -PassThru -Wait -RedirectStandardOutput $null
        
        if ($loginProcess.ExitCode -ne 0) {
            Write-Log "Failed to login to ACR. Exit code: $($loginProcess.ExitCode)" "ERROR"
            return $false
        }
        
        # Tag the image for ACR
        $acrImageName = "$AcrName.azurecr.io/$ImageName`:$ImageTag"
        Write-Log "Tagging image: $ImageName`:$ImageTag -> $acrImageName" "DEBUG"
        $tagProcess = Start-Process -FilePath "docker" -ArgumentList "tag", "$ImageName`:$ImageTag", $acrImageName -NoNewWindow -PassThru -Wait
        
        if ($tagProcess.ExitCode -ne 0) {
            Write-Log "Failed to tag Docker image. Exit code: $($tagProcess.ExitCode)" "ERROR"
            return $false
        }
        
        # Push the image to ACR
        Write-Log "Pushing image to ACR: $acrImageName" "DEBUG"
        $pushProcess = Start-Process -FilePath "docker" -ArgumentList "push", $acrImageName -NoNewWindow -PassThru -Wait
        
        if ($pushProcess.ExitCode -ne 0) {
            Write-Log "Failed to push Docker image to ACR. Exit code: $($pushProcess.ExitCode)" "ERROR"
            return $false
        }
        
        Write-Log "Docker image pushed successfully to ACR: $acrImageName" "INFO"
        return $true
    }
    catch {
        Write-Log "Error pushing Docker image to ACR: $_" "ERROR"
        return $false
    }
}

#endregion

#region App Service Functions

function Update-AppServiceSettings {
    <#
    .SYNOPSIS
        Updates the App Service configuration with environment variables and settings.
    .PARAMETER ResourceGroup
        The name of the resource group.
    .PARAMETER AppServiceName
        The name of the App Service.
    .PARAMETER Settings
        A hashtable of settings to update.
    .RETURNS
        True if update is successful, false otherwise.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ResourceGroup,
        
        [Parameter(Mandatory=$true)]
        [string]$AppServiceName,
        
        [Parameter(Mandatory=$true)]
        [hashtable]$Settings
    )

    Write-Log "Updating App Service settings for: $AppServiceName" "INFO"
    
    try {
        $appSettings = @{}
        foreach ($key in $Settings.Keys) {
            $appSettings[$key] = $Settings[$key]
            Write-Log "Setting $key = $($Settings[$key])" "DEBUG"
        }
        
        $deployment = Set-AzWebApp -ResourceGroupName $ResourceGroup -Name $AppServiceName -AppSettings $appSettings
        
        if ($null -ne $deployment) {
            Write-Log "App Service settings updated successfully." "INFO"
            return $true
        }
        else {
            Write-Log "Failed to update App Service settings." "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "Error updating App Service settings: $_" "ERROR"
        return $false
    }
}

function Update-AppServiceConnectionStrings {
    <#
    .SYNOPSIS
        Updates the App Service connection strings.
    .PARAMETER ResourceGroup
        The name of the resource group.
    .PARAMETER AppServiceName
        The name of the App Service.
    .PARAMETER ConnectionStrings
        A hashtable of connection strings to update.
    .RETURNS
        True if update is successful, false otherwise.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ResourceGroup,
        
        [Parameter(Mandatory=$true)]
        [string]$AppServiceName,
        
        [Parameter(Mandatory=$true)]
        [hashtable]$ConnectionStrings
    )

    Write-Log "Updating App Service connection strings for: $AppServiceName" "INFO"
    
    try {
        $connStrings = @{}
        foreach ($key in $ConnectionStrings.Keys) {
            $connStrings[$key] = @{
                Type = "SQLAzure"
                Value = $ConnectionStrings[$key]
            }
            Write-Log "Connection string $key configured" "DEBUG"
        }
        
        $deployment = Set-AzWebApp -ResourceGroupName $ResourceGroup -Name $AppServiceName -ConnectionStrings $connStrings
        
        if ($null -ne $deployment) {
            Write-Log "App Service connection strings updated successfully." "INFO"
            return $true
        }
        else {
            Write-Log "Failed to update App Service connection strings." "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "Error updating App Service connection strings: $_" "ERROR"
        return $false
    }
}

function Deploy-ContainerToAppService {
    <#
    .SYNOPSIS
        Deploys the container image to Azure App Service.
    .PARAMETER ResourceGroup
        The name of the resource group.
    .PARAMETER AppServiceName
        The name of the App Service.
    .PARAMETER ContainerImageUri
        The URI of the container image.
    .PARAMETER RegistryUrl
        The URL of the container registry.
    .PARAMETER RegistryUsername
        The username for the container registry.
    .PARAMETER RegistryPassword
        The password for the container registry.
    .RETURNS
        True if deployment is successful, false otherwise.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ResourceGroup,
        
        [Parameter(Mandatory=$true)]
        [string]$AppServiceName,
        
        [Parameter(Mandatory=$true)]
        [string]$ContainerImageUri,
        
        [Parameter(Mandatory=$true)]
        [string]$RegistryUrl,
        
        [Parameter(Mandatory=$true)]
        [string]$RegistryUsername,
        
        [Parameter(Mandatory=$true)]
        [string]$RegistryPassword,

        [Parameter(Mandatory=$false)]
        [string]$Slot = ""
    )

    Write-Log "Deploying container image to App Service: $AppServiceName" "INFO"
    if (-not [string]::IsNullOrEmpty($Slot)) {
        Write-Log "Deploying to slot: $Slot" "INFO"
    }
    
    try {
        # Configure the container settings
        $containerConfig = @{
            ResourceGroupName = $ResourceGroup
            Name = $AppServiceName
            ContainerImageName = $ContainerImageUri
            ContainerRegistryUrl = $RegistryUrl
            ContainerRegistryUser = $RegistryUsername
            ContainerRegistryPassword = (ConvertTo-SecureString $RegistryPassword -AsPlainText -Force)
        }
        
        # Add slot parameter if deploying to a slot
        if (-not [string]::IsNullOrEmpty($Slot)) {
            $containerConfig.Add("Slot", $Slot)
        }
        
        # Deploy the container
        Write-Log "Configuring container settings..." "DEBUG"
        if ([string]::IsNullOrEmpty($Slot)) {
            $result = Set-AzWebApp @containerConfig
        }
        else {
            $result = Set-AzWebAppSlot @containerConfig
        }
        
        if ($null -ne $result) {
            Write-Log "Container settings configured successfully." "DEBUG"

            # Restart the App Service or slot to apply changes
            Write-Log "Restarting App Service to apply changes..." "INFO"
            if ([string]::IsNullOrEmpty($Slot)) {
                $restart = Restart-AzWebApp -ResourceGroupName $ResourceGroup -Name $AppServiceName
            }
            else {
                $restart = Restart-AzWebAppSlot -ResourceGroupName $ResourceGroup -Name $AppServiceName -Slot $Slot
            }
            
            if ($null -ne $restart) {
                Write-Log "App Service restarted successfully." "INFO"
                return $true
            }
            else {
                Write-Log "Failed to restart App Service." "WARNING"
                return $true # Still return true as the deployment was successful
            }
        }
        else {
            Write-Log "Failed to configure container settings." "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "Error deploying container to App Service: $_" "ERROR"
        return $false
    }
}

function Test-Deployment {
    <#
    .SYNOPSIS
        Tests the deployed API to ensure it's functioning correctly.
    .PARAMETER ApiUrl
        The URL of the API to test.
    .RETURNS
        True if tests pass, false otherwise.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ApiUrl
    )

    Write-Log "Testing deployment at URL: $ApiUrl" "INFO"
    
    # Wait for the App Service to start
    Write-Log "Waiting for App Service to start (30 seconds)..." "INFO"
    Start-Sleep -Seconds 30
    
    try {
        # Test the health endpoint
        $healthEndpoint = "$ApiUrl/health"
        Write-Log "Testing health endpoint: $healthEndpoint" "DEBUG"
        
        $maxRetries = 5
        $retryCount = 0
        $success = $false
        
        while (-not $success -and $retryCount -lt $maxRetries) {
            try {
                $response = Invoke-WebRequest -Uri $healthEndpoint -Method Get -UseBasicParsing
                
                if ($response.StatusCode -eq 200) {
                    Write-Log "Health check passed. Status code: $($response.StatusCode), Content: $($response.Content)" "INFO"
                    $success = $true
                }
                else {
                    Write-Log "Health check received non-200 response. Status code: $($response.StatusCode), Content: $($response.Content)" "WARNING"
                    $retryCount++
                    Start-Sleep -Seconds 10
                }
            }
            catch {
                Write-Log "Health check failed. Retry $retryCount of $maxRetries. Error: $_" "WARNING"
                $retryCount++
                Start-Sleep -Seconds 10
            }
        }
        
        return $success
    }
    catch {
        Write-Log "Error testing deployment: $_" "ERROR"
        return $false
    }
}

function Swap-AppServiceSlots {
    <#
    .SYNOPSIS
        Swaps App Service production and staging slots.
    .PARAMETER ResourceGroup
        The name of the resource group.
    .PARAMETER AppServiceName
        The name of the App Service.
    .RETURNS
        True if swap is successful, false otherwise.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ResourceGroup,
        
        [Parameter(Mandatory=$true)]
        [string]$AppServiceName
    )

    Write-Log "Swapping App Service slots for: $AppServiceName" "INFO"
    
    try {
        $swap = Switch-AzWebAppSlot -ResourceGroupName $ResourceGroup -Name $AppServiceName -SourceSlotName "staging" -DestinationSlotName "production"
        
        if ($null -ne $swap) {
            Write-Log "App Service slots swapped successfully." "INFO"
            return $true
        }
        else {
            Write-Log "Failed to swap App Service slots." "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "Error swapping App Service slots: $_" "ERROR"
        return $false
    }
}

#endregion

#region Main Script

# Initialize script and log file
Write-Log "Starting Security Patrol backend deployment script" "INFO"
Write-Log "Environment: $Environment" "INFO"
Write-Log "Terraform directory: $TerraformDir" "INFO"
Write-Log "Build only: $BuildOnly" "INFO"
Write-Log "Skip build: $SkipBuild" "INFO"
Write-Log "Image tag: $ImageTag" "INFO"
Write-Log "Deploy to slot: $DeployToSlot" "INFO"
Write-Log "Swap after deployment: $SwapAfterDeployment" "INFO"

try {
    # Check dependencies
    if (-not (Test-Dependencies)) {
        Write-Log "Required dependencies not installed. Please install them and try again." "ERROR"
        exit 1
    }

    # Verify Azure login and subscription
    if (-not (Test-AzLogin)) {
        Write-Log "Azure authentication failed. Please login to Azure and try again." "ERROR"
        exit 1
    }

    if (-not (Select-AzSubscription -SubscriptionId $SubscriptionId)) {
        Write-Log "Failed to select Azure subscription. Please verify subscription ID and try again." "ERROR"
        exit 1
    }

    # Get resource information from Terraform if not provided
    if ([string]::IsNullOrEmpty($ResourceGroup)) {
        $ResourceGroup = Get-TerraformOutput -OutputName "resource_group_name" -TerraformDir $TerraformDir
        if ([string]::IsNullOrEmpty($ResourceGroup)) {
            Write-Log "Failed to retrieve resource group name from Terraform." "ERROR"
            exit 1
        }
        Write-Log "Retrieved resource group name from Terraform: $ResourceGroup" "INFO"
    }

    # Get App Service name from Terraform
    $AppServiceName = Get-TerraformOutput -OutputName "app_service_name" -TerraformDir $TerraformDir
    if ([string]::IsNullOrEmpty($AppServiceName)) {
        Write-Log "Failed to retrieve App Service name from Terraform." "ERROR"
        exit 1
    }
    Write-Log "Retrieved App Service name from Terraform: $AppServiceName" "INFO"

    # Get other resource information from Terraform
    $DatabaseConnectionString = Get-TerraformOutput -OutputName "database_connection_string" -TerraformDir $TerraformDir
    $StorageAccountName = Get-TerraformOutput -OutputName "storage_account_name" -TerraformDir $TerraformDir
    $AppInsightsConnectionString = Get-TerraformOutput -OutputName "app_insights_connection_string" -TerraformDir $TerraformDir

    # Define Docker image and paths
    $DockerfilePath = Join-Path (Split-Path -Parent $SCRIPT_DIR) "../src/backend/Dockerfile"
    $BuildContext = Join-Path (Split-Path -Parent $SCRIPT_DIR) "../src/backend"

    # Build Docker image if not skipped
    if (-not $SkipBuild) {
        Write-Log "Starting Docker image build..." "INFO"
        
        if (-not (Test-Path $DockerfilePath)) {
            Write-Log "Dockerfile not found at path: $DockerfilePath" "ERROR"
            exit 1
        }
        
        if (-not (Test-Path $BuildContext)) {
            Write-Log "Build context directory not found at path: $BuildContext" "ERROR"
            exit 1
        }
        
        if (-not (Build-DockerImage -ImageName $FULL_IMAGE_NAME -ImageTag $ImageTag -DockerfilePath $DockerfilePath -BuildContext $BuildContext)) {
            Write-Log "Docker image build failed." "ERROR"
            exit 1
        }
        
        # If BuildOnly flag is set, exit after building
        if ($BuildOnly) {
            Write-Log "Image built successfully. Exiting as BuildOnly flag is set." "INFO"
            exit 0
        }
    }
    else {
        Write-Log "Skipping Docker image build as SkipBuild flag is set." "INFO"
    }

    # Get ACR information
    $ACR = Get-AzContainerRegistry -ResourceGroupName $ResourceGroup
    if ($null -eq $ACR) {
        Write-Log "Failed to retrieve Azure Container Registry from resource group: $ResourceGroup" "ERROR"
        exit 1
    }
    
    $AcrName = $ACR.Name
    Write-Log "Retrieved ACR name: $AcrName" "INFO"
    
    # Get ACR credentials
    $AcrCredentials = Get-AzContainerRegistryCredential -ResourceGroupName $ResourceGroup -Name $AcrName
    if ($null -eq $AcrCredentials) {
        Write-Log "Failed to retrieve ACR credentials." "ERROR"
        exit 1
    }
    
    $AcrUsername = $AcrCredentials.Username
    $AcrPassword = $AcrCredentials.Password
    
    # Push Docker image to ACR
    if (-not (Push-DockerImageToACR -ImageName $FULL_IMAGE_NAME -ImageTag $ImageTag -AcrName $AcrName)) {
        Write-Log "Failed to push Docker image to ACR." "ERROR"
        exit 1
    }
    
    # Full image URI for deployment
    $ContainerImageUri = "$AcrName.azurecr.io/$FULL_IMAGE_NAME`:$ImageTag"
    Write-Log "Container image URI for deployment: $ContainerImageUri" "INFO"

    # Configure app settings
    $appSettings = @{
        DOCKER_REGISTRY_SERVER_URL = "https://$AcrName.azurecr.io"
        DOCKER_REGISTRY_SERVER_USERNAME = $AcrUsername
        DOCKER_REGISTRY_SERVER_PASSWORD = $AcrPassword
        WEBSITES_ENABLE_APP_SERVICE_STORAGE = "false"
        ASPNETCORE_ENVIRONMENT = $Environment.ToUpperFirst()
        APPLICATIONINSIGHTS_CONNECTION_STRING = $AppInsightsConnectionString
    }
    
    # Determine deployment target (production or slot)
    $deploymentSlot = if ($DeployToSlot) { "staging" } else { "" }
    
    # Deploy to App Service or slot
    Write-Log "Deploying container to App Service${deploymentTarget}" "INFO"
    $deploymentTarget = if ([string]::IsNullOrEmpty($deploymentSlot)) { " production slot" } else { " slot: $deploymentSlot" }
    
    $deploySuccess = Deploy-ContainerToAppService `
        -ResourceGroup $ResourceGroup `
        -AppServiceName $AppServiceName `
        -ContainerImageUri $ContainerImageUri `
        -RegistryUrl "https://$AcrName.azurecr.io" `
        -RegistryUsername $AcrUsername `
        -RegistryPassword $AcrPassword `
        -Slot $deploymentSlot
    
    if (-not $deploySuccess) {
        Write-Log "Failed to deploy container to App Service$deploymentTarget." "ERROR"
        exit 1
    }
    
    # Test the deployment
    $apiBaseUrl = if ([string]::IsNullOrEmpty($deploymentSlot)) {
        "https://$AppServiceName.azurewebsites.net"
    } else {
        "https://$AppServiceName-$deploymentSlot.azurewebsites.net"
    }
    
    if (-not (Test-Deployment -ApiUrl $apiBaseUrl)) {
        Write-Log "Deployment verification failed. The API is not responding correctly." "ERROR"
        exit 1
    }
    
    # If requested, swap staging and production slots
    if ($DeployToSlot -and $SwapAfterDeployment) {
        Write-Log "Swapping staging and production slots..." "INFO"
        
        if (-not (Swap-AppServiceSlots -ResourceGroup $ResourceGroup -AppServiceName $AppServiceName)) {
            Write-Log "Failed to swap App Service slots." "ERROR"
            exit 1
        }
        
        # Test the production deployment after slot swap
        if (-not (Test-Deployment -ApiUrl "https://$AppServiceName.azurewebsites.net")) {
            Write-Log "Post-swap deployment verification failed. The API is not responding correctly in production." "ERROR"
            exit 1
        }
        
        Write-Log "Production slot swap completed and verified successfully." "INFO"
    }
    
    Write-Log "Security Patrol backend deployment completed successfully." "INFO"
    exit 0
}
catch {
    Write-Log "Unhandled exception: $_" "ERROR"
    Write-Log $_.ScriptStackTrace "ERROR"
    exit 1
}

#endregion