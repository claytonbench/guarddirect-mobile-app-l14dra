<#
.SYNOPSIS
    Build and test script for the Security Patrol application.

.DESCRIPTION
    This script automates building and testing the Security Patrol application,
    including the MAUI mobile app and backend API components. It runs unit tests,
    integration tests, specialized tests, and end-to-end tests as specified.
    It also generates code coverage reports and verifies coverage thresholds.

.PARAMETER a, build-api
    Build and test API components.

.PARAMETER m, build-maui
    Build and test MAUI components.

.PARAMETER s, specialized-tests
    Run specialized tests (security, performance).

.PARAMETER e, e2e-tests
    Run end-to-end tests.

.PARAMETER c, configuration
    Build configuration (Debug/Release). Default is Debug.

.PARAMETER t, coverage-threshold
    Minimum code coverage percentage required. Default is 80.

.PARAMETER o, output-path
    Path for test results and coverage reports. Default is ./TestResults.

.PARAMETER h, help
    Display help information.

.EXAMPLE
    .\build-and-test.ps1 -a -m -c Release

.EXAMPLE
    .\build-and-test.ps1 -a -s -t 85 -o ./outputs/test-results

.NOTES
    Author: Security Patrol Development Team
    Version: 1.0
#>

# Script parameters
param (
    [Parameter(Mandatory = $false)]
    [Alias("a")]
    [switch]$buildApi,
    
    [Parameter(Mandatory = $false)]
    [Alias("m")]
    [switch]$buildMaui,
    
    [Parameter(Mandatory = $false)]
    [Alias("s")]
    [switch]$specializedTests,
    
    [Parameter(Mandatory = $false)]
    [Alias("e")]
    [switch]$e2eTests,
    
    [Parameter(Mandatory = $false)]
    [Alias("c")]
    [string]$configuration = "Debug",
    
    [Parameter(Mandatory = $false)]
    [Alias("t")]
    [double]$coverageThreshold = 80,
    
    [Parameter(Mandatory = $false)]
    [Alias("o")]
    [string]$outputPath = "./TestResults",
    
    [Parameter(Mandatory = $false)]
    [Alias("h")]
    [switch]$help
)

# Global variables
$BUILD_CONFIGURATION = $configuration
$TEST_CONFIGURATION = $configuration
$COVERAGE_THRESHOLD = $coverageThreshold
$RESULTS_DIRECTORY = $outputPath
$COVERAGE_DIRECTORY = Join-Path $RESULTS_DIRECTORY "CodeCoverage"

# Source test-report-generator.ps1 to access its functions
$scriptDirectory = $PSScriptRoot
$reportGeneratorScript = Join-Path $scriptDirectory "test-report-generator.ps1"
. $reportGeneratorScript

<#
.SYNOPSIS
    Sets up the environment for building and testing.

.DESCRIPTION
    Creates necessary directories for test results and coverage reports,
    verifies .NET SDK installation, and installs required tools.

.RETURNS
    True for success, false for failure.
#>
function Initialize-Environment {
    try {
        Write-Host "Initializing build and test environment..." -ForegroundColor Cyan
        
        # Create necessary directories
        if (-not (Test-Path $RESULTS_DIRECTORY)) {
            New-Item -ItemType Directory -Path $RESULTS_DIRECTORY -Force | Out-Null
            Write-Host "Created results directory: $RESULTS_DIRECTORY" -ForegroundColor Gray
        }
        
        if (-not (Test-Path $COVERAGE_DIRECTORY)) {
            New-Item -ItemType Directory -Path $COVERAGE_DIRECTORY -Force | Out-Null
            Write-Host "Created coverage directory: $COVERAGE_DIRECTORY" -ForegroundColor Gray
        }
        
        # Verify .NET SDK installation
        $dotnetVersion = dotnet --version
        if ($LASTEXITCODE -ne 0) {
            Write-Error ".NET SDK is not installed or not in PATH"
            return $false
        }
        Write-Host "Using .NET SDK version: $dotnetVersion" -ForegroundColor Gray
        
        # Check if reportgenerator is installed
        $reportGenerator = dotnet tool list -g | Select-String "dotnet-reportgenerator-globaltool"
        if (-not $reportGenerator) {
            Write-Host "Installing reportgenerator tool..." -ForegroundColor Yellow
            dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.1.19
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to install reportgenerator tool"
                return $false
            }
        }
        
        # Set environment variables for testing
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
        $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
        
        Write-Host "Environment initialized successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "Failed to initialize environment: $_"
        return $false
    }
}

<#
.SYNOPSIS
    Builds the specified solution.

.DESCRIPTION
    Runs dotnet restore and dotnet build on the specified solution.

.PARAMETER SolutionPath
    Path to the solution file.

.PARAMETER Configuration
    Build configuration (Debug/Release).

.RETURNS
    True for success, false for failure.
#>
function Build-Solution {
    param (
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath,
        
        [Parameter(Mandatory = $true)]
        [string]$Configuration
    )
    
    try {
        Write-Host "Building solution: $SolutionPath ($Configuration)..." -ForegroundColor Cyan
        
        # Check if solution file exists
        if (-not (Test-Path $SolutionPath)) {
            Write-Error "Solution file not found: $SolutionPath"
            return $false
        }
        
        # Restore NuGet packages
        Write-Host "Restoring NuGet packages..." -ForegroundColor Gray
        dotnet restore $SolutionPath
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to restore NuGet packages for $SolutionPath"
            return $false
        }
        
        # Build solution
        Write-Host "Building solution..." -ForegroundColor Gray
        dotnet build $SolutionPath --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to build solution $SolutionPath"
            return $false
        }
        
        Write-Host "Successfully built $SolutionPath" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "Error building solution $SolutionPath: $_"
        return $false
    }
}

<#
.SYNOPSIS
    Runs tests for the specified project.

.DESCRIPTION
    Runs dotnet test for the specified project with the given configuration and settings.

.PARAMETER ProjectPath
    Path to the test project.

.PARAMETER Configuration
    Test configuration (Debug/Release).

.PARAMETER SettingsPath
    Path to the runsettings file.

.PARAMETER ResultsPath
    Path where test results will be saved.

.PARAMETER CollectCoverage
    Whether to collect code coverage.

.RETURNS
    True for success, false for failure.
#>
function Run-Tests {
    param (
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,
        
        [Parameter(Mandatory = $true)]
        [string]$Configuration,
        
        [Parameter(Mandatory = $true)]
        [string]$SettingsPath,
        
        [Parameter(Mandatory = $true)]
        [string]$ResultsPath,
        
        [Parameter(Mandatory = $false)]
        [bool]$CollectCoverage = $true
    )
    
    try {
        Write-Host "Running tests for project: $ProjectPath..." -ForegroundColor Cyan
        
        # Check if project file exists
        if (-not (Test-Path $ProjectPath)) {
            Write-Error "Test project not found: $ProjectPath"
            return $false
        }
        
        # Check if settings file exists
        if (-not (Test-Path $SettingsPath)) {
            Write-Error "Test settings file not found: $SettingsPath"
            return $false
        }
        
        # Ensure results directory exists
        if (-not (Test-Path $ResultsPath)) {
            New-Item -ItemType Directory -Path $ResultsPath -Force | Out-Null
        }
        
        # Build the test command
        $testCommand = "dotnet test `"$ProjectPath`" --configuration $Configuration --settings `"$SettingsPath`" --results-directory `"$ResultsPath`" --logger `"trx;LogFileName=TestResults.trx`" --logger `"html;LogFileName=TestResults.html`" --no-build"
        
        # Add code coverage collection if specified
        if ($CollectCoverage) {
            $testCommand += " --collect `"XPlat Code Coverage`""
        }
        
        # Run the tests
        Write-Host "Executing test command: $testCommand" -ForegroundColor Gray
        Invoke-Expression $testCommand
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Tests failed for project $ProjectPath"
            return $false
        }
        
        Write-Host "Tests passed for project $ProjectPath" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "Error running tests for project $ProjectPath: $_"
        return $false
    }
}

<#
.SYNOPSIS
    Generates code coverage reports using ReportGenerator.

.DESCRIPTION
    Uses the Generate-Report function from test-report-generator.ps1 to generate code coverage reports.

.PARAMETER CoverageReportPath
    Path to the coverage data file.

.PARAMETER OutputPath
    Directory path where generated reports will be saved.

.PARAMETER TargetDirectory
    Directory containing the source code that was tested.

.RETURNS
    True for success, false for failure.
#>
function Generate-CoverageReport {
    param (
        [Parameter(Mandatory = $true)]
        [string]$CoverageReportPath,
        
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        
        [Parameter(Mandatory = $true)]
        [string]$TargetDirectory
    )
    
    try {
        Write-Host "Generating code coverage report..." -ForegroundColor Cyan
        
        # Call the Generate-Report function from test-report-generator.ps1
        $result = Generate-Report -CoverageReportPath $CoverageReportPath -OutputPath $OutputPath -TargetDirectory $TargetDirectory
        
        if (-not $result) {
            Write-Error "Failed to generate code coverage report"
            return $false
        }
        
        Write-Host "Successfully generated code coverage report at $OutputPath" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "Error generating code coverage report: $_"
        return $false
    }
}

<#
.SYNOPSIS
    Verifies that code coverage meets the specified threshold.

.DESCRIPTION
    Uses the Verify-CodeCoverage function from test-report-generator.ps1 to check if 
    code coverage meets the specified threshold.

.PARAMETER CoverageReportPath
    Path to the coverage report file.

.PARAMETER Threshold
    The minimum acceptable coverage percentage.

.RETURNS
    True for success, false for failure.
#>
function Verify-CodeCoverage {
    param (
        [Parameter(Mandatory = $true)]
        [string]$CoverageReportPath,
        
        [Parameter(Mandatory = $true)]
        [double]$Threshold
    )
    
    try {
        Write-Host "Verifying code coverage meets threshold of $Threshold%..." -ForegroundColor Cyan
        
        # Call the Verify-CodeCoverage function from test-report-generator.ps1
        $result = Verify-CodeCoverage -CoverageReportPath $CoverageReportPath -Threshold $Threshold
        
        if (-not $result) {
            Write-Error "Code coverage does not meet the threshold of $Threshold%"
            return $false
        }
        
        Write-Host "Code coverage meets or exceeds the threshold of $Threshold%" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "Error verifying code coverage: $_"
        return $false
    }
}

<#
.SYNOPSIS
    Builds and tests the API components.

.DESCRIPTION
    Builds the API solution, runs unit and integration tests, and verifies code coverage.

.RETURNS
    True for success, false for failure.
#>
function Build-And-Test-API {
    try {
        Write-Host "`n=== Building and Testing API Components ===`n" -ForegroundColor Cyan
        
        # Define paths
        $apiSolutionPath = "../../src/API/SecurityPatrol.API.sln"
        $apiUnitTestProjectPath = "../../src/API/SecurityPatrol.API.UnitTests/SecurityPatrol.API.UnitTests.csproj"
        $apiIntegrationTestProjectPath = "../../src/API/SecurityPatrol.API.IntegrationTests/SecurityPatrol.API.IntegrationTests.csproj"
        $apiSettingsPath = "../API/SecurityPatrol.API.Tests.runsettings"
        $apiUnitTestResultsPath = "$RESULTS_DIRECTORY/API/UnitTests"
        $apiIntegrationTestResultsPath = "$RESULTS_DIRECTORY/API/IntegrationTests"
        $apiCoverageReportPath = "$COVERAGE_DIRECTORY/API"
        $apiSourceDirectory = "../../src/API"
        
        # Build the API solution
        $buildSuccess = Build-Solution -SolutionPath $apiSolutionPath -Configuration $BUILD_CONFIGURATION
        if (-not $buildSuccess) {
            return $false
        }
        
        # Run API unit tests
        $unitTestSuccess = Run-Tests -ProjectPath $apiUnitTestProjectPath -Configuration $TEST_CONFIGURATION -SettingsPath $apiSettingsPath -ResultsPath $apiUnitTestResultsPath -CollectCoverage $true
        
        # Run API integration tests
        $integrationTestSuccess = Run-Tests -ProjectPath $apiIntegrationTestProjectPath -Configuration $TEST_CONFIGURATION -SettingsPath $apiSettingsPath -ResultsPath $apiIntegrationTestResultsPath -CollectCoverage $true
        
        # Find the coverage file
        $unitTestCoverageFile = Get-ChildItem -Path $apiUnitTestResultsPath -Filter "*.cobertura.xml" -Recurse | Select-Object -First 1
        $integrationTestCoverageFile = Get-ChildItem -Path $apiIntegrationTestResultsPath -Filter "*.cobertura.xml" -Recurse | Select-Object -First 1
        
        if (-not $unitTestCoverageFile -or -not $integrationTestCoverageFile) {
            Write-Error "Could not find coverage files for API tests"
            return $false
        }
        
        # Generate coverage report for API unit tests
        $unitReportSuccess = Generate-CoverageReport -CoverageReportPath $unitTestCoverageFile.FullName -OutputPath "$apiCoverageReportPath/UnitTests" -TargetDirectory $apiSourceDirectory
        
        # Generate coverage report for API integration tests
        $integrationReportSuccess = Generate-CoverageReport -CoverageReportPath $integrationTestCoverageFile.FullName -OutputPath "$apiCoverageReportPath/IntegrationTests" -TargetDirectory $apiSourceDirectory
        
        # Verify code coverage
        $coverageSuccess = Verify-CodeCoverage -CoverageReportPath $unitTestCoverageFile.FullName -Threshold $COVERAGE_THRESHOLD
        
        # Return overall success/failure
        return $unitTestSuccess -and $integrationTestSuccess -and $unitReportSuccess -and $integrationReportSuccess -and $coverageSuccess
    }
    catch {
        Write-Error "Error in Build-And-Test-API: $_"
        return $false
    }
}

<#
.SYNOPSIS
    Builds and tests the MAUI mobile application.

.DESCRIPTION
    Builds the MAUI solution, runs unit and integration tests, and verifies code coverage.

.RETURNS
    True for success, false for failure.
#>
function Build-And-Test-MAUI {
    try {
        Write-Host "`n=== Building and Testing MAUI Components ===`n" -ForegroundColor Cyan
        
        # Define paths
        $mauiSolutionPath = "../../src/MAUI/SecurityPatrol.MAUI.sln"
        $mauiUnitTestProjectPath = "../../src/MAUI/SecurityPatrol.MAUI.UnitTests/SecurityPatrol.MAUI.UnitTests.csproj"
        $mauiIntegrationTestProjectPath = "../../src/MAUI/SecurityPatrol.MAUI.IntegrationTests/SecurityPatrol.MAUI.IntegrationTests.csproj"
        $mauiSettingsPath = "../MAUI/SecurityPatrol.MAUI.Tests.runsettings"
        $mauiUnitTestResultsPath = "$RESULTS_DIRECTORY/MAUI/UnitTests"
        $mauiIntegrationTestResultsPath = "$RESULTS_DIRECTORY/MAUI/IntegrationTests"
        $mauiCoverageReportPath = "$COVERAGE_DIRECTORY/MAUI"
        $mauiSourceDirectory = "../../src/MAUI"
        
        # Install MAUI workload if needed
        $mauiCheck = dotnet workload list | Select-String "maui"
        if (-not $mauiCheck) {
            Write-Host "Installing MAUI workload..." -ForegroundColor Yellow
            dotnet workload install maui
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to install MAUI workload"
                return $false
            }
        }
        
        # Build the MAUI solution
        $buildSuccess = Build-Solution -SolutionPath $mauiSolutionPath -Configuration $BUILD_CONFIGURATION
        if (-not $buildSuccess) {
            return $false
        }
        
        # Run MAUI unit tests
        $unitTestSuccess = Run-Tests -ProjectPath $mauiUnitTestProjectPath -Configuration $TEST_CONFIGURATION -SettingsPath $mauiSettingsPath -ResultsPath $mauiUnitTestResultsPath -CollectCoverage $true
        
        # Run MAUI integration tests
        $integrationTestSuccess = Run-Tests -ProjectPath $mauiIntegrationTestProjectPath -Configuration $TEST_CONFIGURATION -SettingsPath $mauiSettingsPath -ResultsPath $mauiIntegrationTestResultsPath -CollectCoverage $true
        
        # Find the coverage file
        $unitTestCoverageFile = Get-ChildItem -Path $mauiUnitTestResultsPath -Filter "*.cobertura.xml" -Recurse | Select-Object -First 1
        $integrationTestCoverageFile = Get-ChildItem -Path $mauiIntegrationTestResultsPath -Filter "*.cobertura.xml" -Recurse | Select-Object -First 1
        
        if (-not $unitTestCoverageFile -or -not $integrationTestCoverageFile) {
            Write-Error "Could not find coverage files for MAUI tests"
            return $false
        }
        
        # Generate coverage report for MAUI unit tests
        $unitReportSuccess = Generate-CoverageReport -CoverageReportPath $unitTestCoverageFile.FullName -OutputPath "$mauiCoverageReportPath/UnitTests" -TargetDirectory $mauiSourceDirectory
        
        # Generate coverage report for MAUI integration tests
        $integrationReportSuccess = Generate-CoverageReport -CoverageReportPath $integrationTestCoverageFile.FullName -OutputPath "$mauiCoverageReportPath/IntegrationTests" -TargetDirectory $mauiSourceDirectory
        
        # Verify code coverage
        $coverageSuccess = Verify-CodeCoverage -CoverageReportPath $unitTestCoverageFile.FullName -Threshold $COVERAGE_THRESHOLD
        
        # Return overall success/failure
        return $unitTestSuccess -and $integrationTestSuccess -and $unitReportSuccess -and $integrationReportSuccess -and $coverageSuccess
    }
    catch {
        Write-Error "Error in Build-And-Test-MAUI: $_"
        return $false
    }
}

<#
.SYNOPSIS
    Runs specialized tests like security and performance tests.

.DESCRIPTION
    Executes specialized test suites including security tests, vulnerability scanning,
    and performance tests for both API and MAUI components.

.RETURNS
    True for success, false for failure.
#>
function Run-Specialized-Tests {
    try {
        Write-Host "`n=== Running Specialized Tests ===`n" -ForegroundColor Cyan
        
        # Define paths
        $securityTestProjectPath = "../../src/test/Security/SecurityPatrol.SecurityTests/SecurityPatrol.SecurityTests.csproj"
        $apiPerformanceTestProjectPath = "../../src/test/Performance/API/SecurityPatrol.API.PerformanceTests/SecurityPatrol.API.PerformanceTests.csproj"
        $mauiPerformanceTestProjectPath = "../../src/test/Performance/MAUI/SecurityPatrol.MAUI.PerformanceTests/SecurityPatrol.MAUI.PerformanceTests.csproj"
        $securityTestResultsPath = "$RESULTS_DIRECTORY/Specialized/Security"
        $apiPerformanceTestResultsPath = "$RESULTS_DIRECTORY/Specialized/Performance/API"
        $mauiPerformanceTestResultsPath = "$RESULTS_DIRECTORY/Specialized/Performance/MAUI"
        
        $allTestsPassed = $true
        
        # Run security tests
        Write-Host "Running security tests..." -ForegroundColor Yellow
        if (Test-Path $securityTestProjectPath) {
            $securityTestSuccess = Run-Tests -ProjectPath $securityTestProjectPath -Configuration $TEST_CONFIGURATION -SettingsPath "../API/SecurityPatrol.API.Tests.runsettings" -ResultsPath $securityTestResultsPath -CollectCoverage $false
            if (-not $securityTestSuccess) {
                Write-Warning "Security tests failed"
                $allTestsPassed = $false
            }
        }
        else {
            Write-Warning "Security test project not found at path: $securityTestProjectPath"
        }
        
        # Run vulnerability scan
        Write-Host "Running vulnerability scan..." -ForegroundColor Yellow
        dotnet list ../../src/API/SecurityPatrol.API/SecurityPatrol.API.csproj package --vulnerable --include-transitive
        dotnet list ../../src/MAUI/SecurityPatrol.MAUI/SecurityPatrol.MAUI.csproj package --vulnerable --include-transitive
        
        # Run API performance tests
        Write-Host "Running API performance tests..." -ForegroundColor Yellow
        if (Test-Path $apiPerformanceTestProjectPath) {
            $apiPerformanceTestSuccess = Run-Tests -ProjectPath $apiPerformanceTestProjectPath -Configuration $TEST_CONFIGURATION -SettingsPath "../API/SecurityPatrol.API.Tests.runsettings" -ResultsPath $apiPerformanceTestResultsPath -CollectCoverage $false
            if (-not $apiPerformanceTestSuccess) {
                Write-Warning "API performance tests failed"
                $allTestsPassed = $false
            }
        }
        else {
            Write-Warning "API performance test project not found at path: $apiPerformanceTestProjectPath"
        }
        
        # Run MAUI performance tests
        Write-Host "Running MAUI performance tests..." -ForegroundColor Yellow
        if (Test-Path $mauiPerformanceTestProjectPath) {
            $mauiPerformanceTestSuccess = Run-Tests -ProjectPath $mauiPerformanceTestProjectPath -Configuration $TEST_CONFIGURATION -SettingsPath "../MAUI/SecurityPatrol.MAUI.Tests.runsettings" -ResultsPath $mauiPerformanceTestResultsPath -CollectCoverage $false
            if (-not $mauiPerformanceTestSuccess) {
                Write-Warning "MAUI performance tests failed"
                $allTestsPassed = $false
            }
        }
        else {
            Write-Warning "MAUI performance test project not found at path: $mauiPerformanceTestProjectPath"
        }
        
        return $allTestsPassed
    }
    catch {
        Write-Error "Error in Run-Specialized-Tests: $_"
        return $false
    }
}

<#
.SYNOPSIS
    Runs end-to-end tests.

.DESCRIPTION
    Sets up the test environment and runs end-to-end tests that verify the entire system.

.RETURNS
    True for success, false for failure.
#>
function Run-E2E-Tests {
    try {
        Write-Host "`n=== Running End-to-End Tests ===`n" -ForegroundColor Cyan
        
        # Define paths
        $e2eTestProjectPath = "../../src/test/E2E/SecurityPatrol.E2ETests/SecurityPatrol.E2ETests.csproj"
        $e2eTestResultsPath = "$RESULTS_DIRECTORY/E2E"
        
        # Set up test environment
        Write-Host "Setting up test environment for E2E tests..." -ForegroundColor Yellow
        
        # Start API in test mode
        $apiProcess = $null
        $apiProjectPath = "../../src/API/SecurityPatrol.API/SecurityPatrol.API.csproj"
        if (Test-Path $apiProjectPath) {
            Write-Host "Starting API for E2E tests..." -ForegroundColor Yellow
            $env:ASPNETCORE_ENVIRONMENT = "Testing"
            $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$apiProjectPath`" --configuration $TEST_CONFIGURATION --no-build" -PassThru -NoNewWindow
            
            # Wait for API to start
            Start-Sleep -Seconds 10
        }
        else {
            Write-Warning "API project not found at path: $apiProjectPath"
            return $false
        }
        
        # Run E2E tests
        $e2eTestSuccess = $false
        if (Test-Path $e2eTestProjectPath) {
            $e2eTestSuccess = Run-Tests -ProjectPath $e2eTestProjectPath -Configuration $TEST_CONFIGURATION -SettingsPath "../API/SecurityPatrol.API.Tests.runsettings" -ResultsPath $e2eTestResultsPath -CollectCoverage $false
        }
        else {
            Write-Warning "E2E test project not found at path: $e2eTestProjectPath"
        }
        
        # Clean up test environment
        if ($apiProcess -ne $null) {
            Write-Host "Stopping API process..." -ForegroundColor Yellow
            Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
        }
        
        return $e2eTestSuccess
    }
    catch {
        Write-Error "Error in Run-E2E-Tests: $_"
        
        # Ensure cleanup of any started processes
        try {
            if ($apiProcess -ne $null) {
                Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
            }
        }
        catch {
            Write-Warning "Failed to clean up API process: $_"
        }
        
        return $false
    }
}

<#
.SYNOPSIS
    Displays usage information for the script.

.DESCRIPTION
    Prints script name, description, available options and parameters, and examples of usage.
#>
function Show-Usage {
    Write-Host "`nSecurity Patrol Build and Test Script" -ForegroundColor Cyan
    Write-Host "=================================="
    Write-Host "`nThis script automates building and testing the Security Patrol application.`n"
    
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -a, --build-api            Build and test API components"
    Write-Host "  -m, --build-maui           Build and test MAUI components"
    Write-Host "  -s, --specialized-tests    Run specialized tests"
    Write-Host "  -e, --e2e-tests            Run end-to-end tests"
    Write-Host "  -c, --configuration        Build configuration (Debug/Release). Default: Debug"
    Write-Host "  -t, --coverage-threshold   Minimum code coverage percentage required. Default: 80"
    Write-Host "  -o, --output-path          Path for test results and coverage reports. Default: ./TestResults"
    Write-Host "  -h, --help                 Display this help information"
    
    Write-Host "`nExamples:" -ForegroundColor Yellow
    Write-Host "  .\build-and-test.ps1 -a -m"
    Write-Host "  .\build-and-test.ps1 --build-api --build-maui -c Release"
    Write-Host "  .\build-and-test.ps1 -a -s -t 85 -o ./outputs/test-results"
    Write-Host "  .\build-and-test.ps1 --e2e-tests -c Release"
    Write-Host "`n"
}

<#
.SYNOPSIS
    Parses command line arguments.

.DESCRIPTION
    Processes command line arguments into a structured format for use in the script.

.PARAMETER Arguments
    The command line arguments to parse.

.RETURNS
    A hashtable containing the parsed arguments.
#>
function Parse-Arguments {
    param (
        [Parameter(Mandatory = $true)]
        [array]$Arguments
    )
    
    $parsedArgs = @{
        BuildApi = $false
        BuildMaui = $false
        SpecializedTests = $false
        E2ETests = $false
        Configuration = "Debug"
        CoverageThreshold = 80
        OutputPath = "./TestResults"
        Help = $false
    }
    
    for ($i = 0; $i -lt $Arguments.Count; $i++) {
        $arg = $Arguments[$i]
        
        switch -regex ($arg) {
            "^(-a|--build-api)$" {
                $parsedArgs.BuildApi = $true
            }
            "^(-m|--build-maui)$" {
                $parsedArgs.BuildMaui = $true
            }
            "^(-s|--specialized-tests)$" {
                $parsedArgs.SpecializedTests = $true
            }
            "^(-e|--e2e-tests)$" {
                $parsedArgs.E2ETests = $true
            }
            "^(-c|--configuration)$" {
                if ($i + 1 -lt $Arguments.Count) {
                    $parsedArgs.Configuration = $Arguments[++$i]
                }
            }
            "^(-t|--coverage-threshold)$" {
                if ($i + 1 -lt $Arguments.Count) {
                    $parsedArgs.CoverageThreshold = [double]$Arguments[++$i]
                }
            }
            "^(-o|--output-path)$" {
                if ($i + 1 -lt $Arguments.Count) {
                    $parsedArgs.OutputPath = $Arguments[++$i]
                }
            }
            "^(-h|--help)$" {
                $parsedArgs.Help = $true
            }
            default {
                Write-Warning "Unknown argument: $arg"
            }
        }
    }
    
    # Validate argument combinations
    if (-not ($parsedArgs.BuildApi -or $parsedArgs.BuildMaui -or $parsedArgs.SpecializedTests -or $parsedArgs.E2ETests -or $parsedArgs.Help)) {
        Write-Warning "No build or test options specified. Use -h or --help for usage information."
    }
    
    return $parsedArgs
}

<#
.SYNOPSIS
    Main function that orchestrates the build and test process.

.DESCRIPTION
    Parses arguments, initializes the environment, and runs the requested build and test operations.

.PARAMETER Arguments
    Command line arguments passed to the script.

.RETURNS
    Exit code (0 for success, non-zero for failure).
#>
function Main {
    param (
        [Parameter(Mandatory = $true)]
        [array]$Arguments
    )
    
    try {
        # Parse command line arguments
        $parsedArgs = Parse-Arguments -Arguments $Arguments
        
        # If help is requested, show usage and exit
        if ($parsedArgs.Help) {
            Show-Usage
            return 0
        }
        
        # Set global variables based on parsed arguments
        $global:BUILD_CONFIGURATION = $parsedArgs.Configuration
        $global:TEST_CONFIGURATION = $parsedArgs.Configuration
        $global:COVERAGE_THRESHOLD = $parsedArgs.CoverageThreshold
        $global:RESULTS_DIRECTORY = $parsedArgs.OutputPath
        $global:COVERAGE_DIRECTORY = Join-Path $global:RESULTS_DIRECTORY "CodeCoverage"
        
        # Display script header
        Write-Host "`n==================================================================" -ForegroundColor Cyan
        Write-Host "            Security Patrol Build and Test Script" -ForegroundColor Cyan
        Write-Host "==================================================================" -ForegroundColor Cyan
        Write-Host "Configuration:     $BUILD_CONFIGURATION"
        Write-Host "Coverage Threshold: $COVERAGE_THRESHOLD%"
        Write-Host "Results Directory: $RESULTS_DIRECTORY"
        Write-Host "==================================================================" -ForegroundColor Cyan
        
        # Initialize the environment
        $initSuccess = Initialize-Environment
        if (-not $initSuccess) {
            Write-Error "Failed to initialize the environment. Exiting."
            return 1
        }
        
        $overallSuccess = $true
        
        # Build and test API components if requested
        if ($parsedArgs.BuildApi) {
            $apiSuccess = Build-And-Test-API
            if (-not $apiSuccess) {
                Write-Error "API build and test failed"
                $overallSuccess = $false
            }
        }
        
        # Build and test MAUI components if requested
        if ($parsedArgs.BuildMaui) {
            $mauiSuccess = Build-And-Test-MAUI
            if (-not $mauiSuccess) {
                Write-Error "MAUI build and test failed"
                $overallSuccess = $false
            }
        }
        
        # Run specialized tests if requested
        if ($parsedArgs.SpecializedTests) {
            $specializedSuccess = Run-Specialized-Tests
            if (-not $specializedSuccess) {
                Write-Error "Specialized tests failed"
                $overallSuccess = $false
            }
        }
        
        # Run E2E tests if requested
        if ($parsedArgs.E2ETests) {
            $e2eSuccess = Run-E2E-Tests
            if (-not $e2eSuccess) {
                Write-Error "End-to-end tests failed"
                $overallSuccess = $false
            }
        }
        
        # Output summary of results
        Write-Host "`n==================================================================" -ForegroundColor Cyan
        Write-Host "                      Build and Test Results" -ForegroundColor Cyan
        Write-Host "==================================================================" -ForegroundColor Cyan
        
        if ($parsedArgs.BuildApi) {
            $apiStatus = if ($apiSuccess) { "PASSED" } else { "FAILED" }
            $apiColor = if ($apiSuccess) { "Green" } else { "Red" }
            Write-Host "API Build and Test: " -NoNewline
            Write-Host $apiStatus -ForegroundColor $apiColor
        }
        
        if ($parsedArgs.BuildMaui) {
            $mauiStatus = if ($mauiSuccess) { "PASSED" } else { "FAILED" }
            $mauiColor = if ($mauiSuccess) { "Green" } else { "Red" }
            Write-Host "MAUI Build and Test: " -NoNewline
            Write-Host $mauiStatus -ForegroundColor $mauiColor
        }
        
        if ($parsedArgs.SpecializedTests) {
            $specializedStatus = if ($specializedSuccess) { "PASSED" } else { "FAILED" }
            $specializedColor = if ($specializedSuccess) { "Green" } else { "Red" }
            Write-Host "Specialized Tests: " -NoNewline
            Write-Host $specializedStatus -ForegroundColor $specializedColor
        }
        
        if ($parsedArgs.E2ETests) {
            $e2eStatus = if ($e2eSuccess) { "PASSED" } else { "FAILED" }
            $e2eColor = if ($e2eSuccess) { "Green" } else { "Red" }
            Write-Host "End-to-End Tests: " -NoNewline
            Write-Host $e2eStatus -ForegroundColor $e2eColor
        }
        
        $overallStatus = if ($overallSuccess) { "PASSED" } else { "FAILED" }
        $overallColor = if ($overallSuccess) { "Green" } else { "Red" }
        Write-Host "`nOverall Build and Test: " -NoNewline
        Write-Host $overallStatus -ForegroundColor $overallColor
        
        Write-Host "`nTest results and coverage reports can be found at: $RESULTS_DIRECTORY"
        Write-Host "==================================================================" -ForegroundColor Cyan
        
        # Return appropriate exit code
        return if ($overallSuccess) { 0 } else { 1 }
    }
    catch {
        Write-Error "Unhandled exception in Main function: $_"
        return 1
    }
}

# Execute the main function with script arguments
$exitCode = Main -Arguments $args
exit $exitCode