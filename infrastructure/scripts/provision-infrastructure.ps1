<#
.SYNOPSIS
    Provisions Azure infrastructure for the Security Patrol Application using Terraform.

.DESCRIPTION
    This script automates the provisioning of Azure infrastructure resources for the
    Security Patrol Application backend services using Terraform. It handles Azure
    authentication, environment selection, Terraform initialization, planning, and
    application of infrastructure changes.

.PARAMETER Environment
    Target environment for infrastructure provisioning (dev, staging, prod).

.PARAMETER SubscriptionId
    Azure subscription ID for deployment. If not provided, the current subscription will be used.

.PARAMETER TerraformDir
    Path to Terraform directory containing configuration files.

.PARAMETER PlanOnly
    Only create Terraform plan without applying changes.

.PARAMETER Destroy
    Destroy the Terraform-managed infrastructure.

.PARAMETER AutoApprove
    Automatically approve Terraform plan application without confirmation.

.PARAMETER OutputFile
    File to export Terraform outputs to.

.PARAMETER AdditionalVars
    Additional Terraform variables to pass to the plan command.

.PARAMETER Verbose
    Enable verbose logging.

.EXAMPLE
    .\provision-infrastructure.ps1 -Environment dev -SubscriptionId "00000000-0000-0000-0000-000000000000"

.EXAMPLE
    .\provision-infrastructure.ps1 -Environment prod -PlanOnly -Verbose

.EXAMPLE
    .\provision-infrastructure.ps1 -Environment staging -Destroy -AutoApprove

.NOTES
    Author: Security Patrol Application Team
    Required Dependencies: Az PowerShell module (v9.0+), Terraform CLI (v1.5+)
    Minimum Permissions: Contributor role on target subscription
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment = "dev",

    [Parameter(Mandatory = $false)]
    [ValidatePattern('^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$')]
    [string]$SubscriptionId = "",

    [Parameter(Mandatory = $false)]
    [ValidateScript({ Test-Path $_ -PathType Container })]
    [string]$TerraformDir = (Join-Path (Split-Path $PSScriptRoot -Parent) "terraform"),

    [Parameter(Mandatory = $false)]
    [switch]$PlanOnly = $false,

    [Parameter(Mandatory = $false)]
    [switch]$Destroy = $false,

    [Parameter(Mandatory = $false)]
    [switch]$AutoApprove = $false,

    [Parameter(Mandatory = $false)]
    [string]$OutputFile = "terraform-outputs.json",

    [Parameter(Mandatory = $false)]
    [hashtable]$AdditionalVars = @{},

    [Parameter(Mandatory = $false)]
    [switch]$Verbose = $false
)

# Global variables
$SCRIPT_DIR = $PSScriptRoot
$LOG_FILE = Join-Path $SCRIPT_DIR "provision-infrastructure.log"
$VERBOSE = $Verbose

# Function definitions
function Write-Log {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateSet("INFO", "WARN", "ERROR", "DEBUG")]
        [string]$Level = "INFO"
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"

    # Output to console with appropriate color
    switch ($Level) {
        "INFO" { Write-Host $logMessage -ForegroundColor White }
        "WARN" { Write-Host $logMessage -ForegroundColor Yellow }
        "ERROR" { Write-Host $logMessage -ForegroundColor Red }
        "DEBUG" {
            if ($VERBOSE) {
                Write-Host $logMessage -ForegroundColor Gray
            }
        }
        default { Write-Host $logMessage }
    }

    # Append to log file
    Add-Content -Path $LOG_FILE -Value $logMessage
}

function Test-Dependencies {
    [CmdletBinding()]
    param ()

    $dependenciesOk = $true
    
    # Check Az PowerShell module
    Write-Log "Checking Az PowerShell module..." -Level DEBUG
    try {
        $azModule = Get-Module -Name Az -ListAvailable
        if (-not $azModule) {
            Write-Log "Az PowerShell module not found. Please install it using: Install-Module -Name Az -Scope CurrentUser -Repository PSGallery -Force" -Level ERROR
            $dependenciesOk = $false
        }
        else {
            Write-Log "Az PowerShell module found (version $($azModule[0].Version))" -Level DEBUG
        }
    }
    catch {
        Write-Log "Error checking Az PowerShell module: $_" -Level ERROR
        $dependenciesOk = $false
    }

    # Check Terraform
    Write-Log "Checking Terraform CLI..." -Level DEBUG
    try {
        $terraform = Get-Command terraform -ErrorAction SilentlyContinue
        if (-not $terraform) {
            Write-Log "Terraform CLI not found. Please install Terraform and ensure it's in your PATH" -Level ERROR
            $dependenciesOk = $false
        }
        else {
            $tfVersion = (terraform version -json | ConvertFrom-Json).terraform_version
            Write-Log "Terraform CLI found (version $tfVersion)" -Level DEBUG
        }
    }
    catch {
        Write-Log "Error checking Terraform CLI: $_" -Level ERROR
        $dependenciesOk = $false
    }

    return $dependenciesOk
}

function Test-AzLogin {
    [CmdletBinding()]
    param ()

    Write-Log "Checking Azure login status..." -Level DEBUG
    try {
        $context = Get-AzContext
        if (-not $context) {
            Write-Log "No Azure context found. Prompting for login..." -Level WARN
            Connect-AzAccount -ErrorAction Stop
            $context = Get-AzContext
            if (-not $context) {
                Write-Log "Failed to login to Azure" -Level ERROR
                return $false
            }
        }
        
        Write-Log "Azure login confirmed. Connected as $($context.Account.Id) to subscription $($context.Subscription.Name) ($($context.Subscription.Id))" -Level INFO
        return $true
    }
    catch {
        Write-Log "Error checking Azure login: $_" -Level ERROR
        return $false
    }
}

function Select-AzSubscription {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$SubscriptionId
    )

    Write-Log "Selecting Azure subscription $SubscriptionId..." -Level INFO
    try {
        Set-AzContext -SubscriptionId $SubscriptionId -ErrorAction Stop | Out-Null
        $context = Get-AzContext
        if ($context.Subscription.Id -eq $SubscriptionId) {
            Write-Log "Successfully selected subscription $($context.Subscription.Name) ($SubscriptionId)" -Level INFO
            return $true
        }
        else {
            Write-Log "Failed to select subscription $SubscriptionId" -Level ERROR
            return $false
        }
    }
    catch {
        Write-Log "Error selecting subscription $SubscriptionId: $_" -Level ERROR
        return $false
    }
}

function Initialize-Terraform {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$TerraformDir
    )

    Write-Log "Initializing Terraform in $TerraformDir..." -Level INFO
    try {
        Push-Location $TerraformDir
        
        Write-Log "Running terraform init..." -Level DEBUG
        $initOutput = terraform init 2>&1
        $initExitCode = $LASTEXITCODE

        if ($initExitCode -ne 0) {
            Write-Log "Terraform initialization failed with exit code $initExitCode" -Level ERROR
            Write-Log "Output: $initOutput" -Level ERROR
            Pop-Location
            return $false
        }

        Write-Log "Terraform initialized successfully" -Level INFO
        Pop-Location
        return $true
    }
    catch {
        Write-Log "Error initializing Terraform: $_" -Level ERROR
        if ((Get-Location).Path -eq $TerraformDir) {
            Pop-Location
        }
        return $false
    }
}

function New-TerraformPlan {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$TerraformDir,

        [Parameter(Mandatory = $true)]
        [string]$Environment,

        [Parameter(Mandatory = $true)]
        [string]$PlanFile,

        [Parameter(Mandatory = $false)]
        [hashtable]$AdditionalVars = @{}
    )

    Write-Log "Creating Terraform plan for environment $Environment..." -Level INFO
    try {
        Push-Location $TerraformDir
        
        # Construct var file path
        $varFile = Join-Path $TerraformDir "environments" "$Environment.tfvars"
        if (-not (Test-Path $varFile)) {
            Write-Log "Terraform var file not found: $varFile" -Level ERROR
            Pop-Location
            return $false
        }

        # Build terraform plan command
        $planCmd = "terraform plan -var-file=`"$varFile`""
        
        # Add additional variables
        foreach ($key in $AdditionalVars.Keys) {
            $value = $AdditionalVars[$key]
            $planCmd += " -var='$key=$value'"
        }
        
        # Add output plan file
        $planCmd += " -out=`"$PlanFile`""
        
        # Execute terraform plan
        Write-Log "Running: $planCmd" -Level DEBUG
        Invoke-Expression $planCmd
        $planExitCode = $LASTEXITCODE

        if ($planExitCode -ne 0) {
            Write-Log "Terraform plan creation failed with exit code $planExitCode" -Level ERROR
            Pop-Location
            return $false
        }

        Write-Log "Terraform plan created successfully and saved to $PlanFile" -Level INFO
        Pop-Location
        return $true
    }
    catch {
        Write-Log "Error creating Terraform plan: $_" -Level ERROR
        if ((Get-Location).Path -eq $TerraformDir) {
            Pop-Location
        }
        return $false
    }
}

function Invoke-TerraformApply {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$TerraformDir,

        [Parameter(Mandatory = $true)]
        [string]$PlanFile,

        [Parameter(Mandatory = $false)]
        [bool]$AutoApprove = $false
    )

    Write-Log "Applying Terraform plan from $PlanFile..." -Level INFO
    try {
        Push-Location $TerraformDir
        
        # Build terraform apply command
        $applyCmd = "terraform apply"
        if ($AutoApprove) {
            $applyCmd += " -auto-approve"
        }
        $applyCmd += " `"$PlanFile`""
        
        # Execute terraform apply
        Write-Log "Running: $applyCmd" -Level DEBUG
        Invoke-Expression $applyCmd
        $applyExitCode = $LASTEXITCODE

        if ($applyExitCode -ne 0) {
            Write-Log "Terraform apply failed with exit code $applyExitCode" -Level ERROR
            Pop-Location
            return $false
        }

        Write-Log "Terraform apply completed successfully" -Level INFO
        Pop-Location
        return $true
    }
    catch {
        Write-Log "Error applying Terraform plan: $_" -Level ERROR
        if ((Get-Location).Path -eq $TerraformDir) {
            Pop-Location
        }
        return $false
    }
}

function Invoke-TerraformDestroy {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$TerraformDir,

        [Parameter(Mandatory = $true)]
        [string]$Environment,

        [Parameter(Mandatory = $false)]
        [bool]$AutoApprove = $false
    )

    Write-Log "Destroying Terraform-managed infrastructure for environment $Environment..." -Level INFO
    try {
        Push-Location $TerraformDir
        
        # Construct var file path
        $varFile = Join-Path $TerraformDir "environments" "$Environment.tfvars"
        if (-not (Test-Path $varFile)) {
            Write-Log "Terraform var file not found: $varFile" -Level ERROR
            Pop-Location
            return $false
        }

        # Build terraform destroy command
        $destroyCmd = "terraform destroy -var-file=`"$varFile`""
        if ($AutoApprove) {
            $destroyCmd += " -auto-approve"
        }
        
        # Execute terraform destroy
        Write-Log "Running: $destroyCmd" -Level DEBUG
        Invoke-Expression $destroyCmd
        $destroyExitCode = $LASTEXITCODE

        if ($destroyExitCode -ne 0) {
            Write-Log "Terraform destroy failed with exit code $destroyExitCode" -Level ERROR
            Pop-Location
            return $false
        }

        Write-Log "Terraform destroy completed successfully" -Level INFO
        Pop-Location
        return $true
    }
    catch {
        Write-Log "Error destroying Terraform infrastructure: $_" -Level ERROR
        if ((Get-Location).Path -eq $TerraformDir) {
            Pop-Location
        }
        return $false
    }
}

function Get-TerraformOutput {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$TerraformDir,

        [Parameter(Mandatory = $true)]
        [string]$OutputName
    )

    try {
        Push-Location $TerraformDir
        
        $output = terraform output -raw $OutputName 2>&1
        $outputExitCode = $LASTEXITCODE

        if ($outputExitCode -ne 0) {
            Write-Log "Failed to get Terraform output $OutputName: $output" -Level ERROR
            Pop-Location
            return $null
        }

        Pop-Location
        return $output
    }
    catch {
        Write-Log "Error getting Terraform output $OutputName: $_" -Level ERROR
        if ((Get-Location).Path -eq $TerraformDir) {
            Pop-Location
        }
        return $null
    }
}

function Export-TerraformOutputs {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$TerraformDir,

        [Parameter(Mandatory = $true)]
        [string]$OutputFile
    )

    Write-Log "Exporting Terraform outputs to $OutputFile..." -Level INFO
    try {
        Push-Location $TerraformDir
        
        $output = terraform output -json 2>&1
        $outputExitCode = $LASTEXITCODE

        if ($outputExitCode -ne 0) {
            Write-Log "Failed to get Terraform outputs: $output" -Level ERROR
            Pop-Location
            return $false
        }

        # Save output to file
        $output | Out-File -FilePath $OutputFile -Encoding utf8 -Force
        
        Write-Log "Terraform outputs exported successfully to $OutputFile" -Level INFO
        Pop-Location
        return $true
    }
    catch {
        Write-Log "Error exporting Terraform outputs: $_" -Level ERROR
        if ((Get-Location).Path -eq $TerraformDir) {
            Pop-Location
        }
        return $false
    }
}

# Main script execution
try {
    # Banner and initialization
    Write-Host "=======================================================================" -ForegroundColor Cyan
    Write-Host "  Security Patrol Application - Infrastructure Provisioning Script" -ForegroundColor Cyan
    Write-Host "=======================================================================" -ForegroundColor Cyan
    Write-Host ""

    # Initialize log file
    if (Test-Path $LOG_FILE) {
        Write-Host "Appending to existing log file: $LOG_FILE" -ForegroundColor Gray
    }
    else {
        New-Item -Path $LOG_FILE -ItemType File -Force | Out-Null
        Write-Host "Created new log file: $LOG_FILE" -ForegroundColor Gray
    }

    Write-Log "Starting infrastructure provisioning script for environment: $Environment" -Level INFO
    Write-Log "Script parameters:" -Level DEBUG
    Write-Log "  Environment: $Environment" -Level DEBUG
    Write-Log "  SubscriptionId: $(if($SubscriptionId -eq '') {'Using current context'} else {$SubscriptionId})" -Level DEBUG
    Write-Log "  TerraformDir: $TerraformDir" -Level DEBUG
    Write-Log "  PlanOnly: $PlanOnly" -Level DEBUG
    Write-Log "  Destroy: $Destroy" -Level DEBUG
    Write-Log "  AutoApprove: $AutoApprove" -Level DEBUG
    Write-Log "  OutputFile: $OutputFile" -Level DEBUG
    Write-Log "  AdditionalVars: $($AdditionalVars.Keys -join ', ')" -Level DEBUG
    Write-Log "  Verbose: $VERBOSE" -Level DEBUG

    # Check dependencies
    if (-not (Test-Dependencies)) {
        Write-Log "Required dependencies are missing. Please install them and try again." -Level ERROR
        exit 1
    }

    # Check Azure login
    if (-not (Test-AzLogin)) {
        Write-Log "Azure login failed. Exiting script." -Level ERROR
        exit 1
    }

    # Select subscription if provided
    if ($SubscriptionId -ne "") {
        if (-not (Select-AzSubscription -SubscriptionId $SubscriptionId)) {
            Write-Log "Failed to select Azure subscription. Exiting script." -Level ERROR
            exit 1
        }
    }
    else {
        $context = Get-AzContext
        $SubscriptionId = $context.Subscription.Id
        Write-Log "Using current subscription: $($context.Subscription.Name) ($SubscriptionId)" -Level INFO
    }

    # Ensure Terraform directory exists
    if (-not (Test-Path $TerraformDir -PathType Container)) {
        Write-Log "Terraform directory not found: $TerraformDir" -Level ERROR
        exit 1
    }

    # Initialize Terraform
    if (-not (Initialize-Terraform -TerraformDir $TerraformDir)) {
        Write-Log "Terraform initialization failed. Exiting script." -Level ERROR
        exit 1
    }

    # Perform Terraform operations based on parameters
    if ($Destroy) {
        # Execute terraform destroy
        Write-Log "Initiating infrastructure destruction for environment $Environment..." -Level WARN
        
        if (-not (Invoke-TerraformDestroy -TerraformDir $TerraformDir -Environment $Environment -AutoApprove $AutoApprove)) {
            Write-Log "Terraform destroy failed. Exiting script." -Level ERROR
            exit 1
        }
        
        Write-Log "Infrastructure destruction completed successfully for environment $Environment" -Level INFO
    }
    else {
        # Create plan file path
        $planFile = Join-Path $TerraformDir "terraform.$Environment.tfplan"
        
        # Create terraform plan
        if (-not (New-TerraformPlan -TerraformDir $TerraformDir -Environment $Environment -PlanFile $planFile -AdditionalVars $AdditionalVars)) {
            Write-Log "Terraform plan creation failed. Exiting script." -Level ERROR
            exit 1
        }
        
        # Apply terraform plan if not in plan-only mode
        if (-not $PlanOnly) {
            if (-not (Invoke-TerraformApply -TerraformDir $TerraformDir -PlanFile $planFile -AutoApprove $AutoApprove)) {
                Write-Log "Terraform apply failed. Exiting script." -Level ERROR
                exit 1
            }
            
            # Export terraform outputs
            $outputFilePath = Join-Path $SCRIPT_DIR $OutputFile
            if (-not (Export-TerraformOutputs -TerraformDir $TerraformDir -OutputFile $outputFilePath)) {
                Write-Log "Failed to export Terraform outputs. Infrastructure was provisioned, but output export failed." -Level WARN
            }
            else {
                Write-Log "Terraform outputs exported to $outputFilePath" -Level INFO
            }
            
            Write-Log "Infrastructure provisioning completed successfully for environment $Environment" -Level INFO
        }
        else {
            Write-Log "Plan created successfully. To apply this plan, run the script without the -PlanOnly parameter." -Level INFO
        }
    }

    # Script completed successfully
    Write-Host ""
    Write-Host "=======================================================================" -ForegroundColor Green
    Write-Host "  Infrastructure provisioning script completed successfully" -ForegroundColor Green
    Write-Host "=======================================================================" -ForegroundColor Green
    exit 0
}
catch {
    Write-Log "Unhandled exception occurred: $_" -Level ERROR
    Write-Log "Exception details: $($_.Exception)" -Level ERROR
    Write-Log "Stack trace: $($_.ScriptStackTrace)" -Level ERROR
    
    Write-Host ""
    Write-Host "=======================================================================" -ForegroundColor Red
    Write-Host "  Infrastructure provisioning script failed with an error" -ForegroundColor Red
    Write-Host "  See log file for details: $LOG_FILE" -ForegroundColor Red
    Write-Host "=======================================================================" -ForegroundColor Red
    exit 1
}