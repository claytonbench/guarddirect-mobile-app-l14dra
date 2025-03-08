<#
.SYNOPSIS
    Test report generation and code coverage verification script for the Security Patrol application.

.DESCRIPTION
    This script provides functions to process test coverage data, generate HTML reports,
    and validate that code coverage meets specified thresholds. It integrates with
    CI/CD pipelines to provide quality gates based on code coverage metrics.

.NOTES
    File Name      : test-report-generator.ps1
    Author         : Security Patrol Dev Team
    Prerequisite   : PowerShell 5.1 or higher
                     dotnet-reportgenerator-globaltool package (v5.1.19)

.EXAMPLE
    .\test-report-generator.ps1 -CoverageReportPath ".\TestResults\coverage.cobertura.xml" -OutputPath ".\CoverageReport" -TargetDirectory ".\src"
    Generates HTML coverage report from the coverage data file.

.EXAMPLE
    .\test-report-generator.ps1 -Verify -CoverageReportPath ".\TestResults\coverage.cobertura.xml" -Threshold 85
    Verifies that code coverage meets or exceeds the specified threshold of 85%.
#>

# Default values for parameters
$DEFAULT_REPORT_TYPES = "Html;Cobertura"
$DEFAULT_COVERAGE_THRESHOLD = 80

# Add the System.Xml.Linq namespace for XML parsing
Add-Type -AssemblyName System.Xml.Linq

<#
.SYNOPSIS
    Generates HTML and Cobertura coverage reports from coverage data files.

.DESCRIPTION
    Uses reportgenerator tool to generate coverage reports in specified formats.
    Requires dotnet-reportgenerator-globaltool to be installed.

.PARAMETER CoverageReportPath
    Path to the coverage data file (typically a Cobertura XML file).

.PARAMETER OutputPath
    Directory path where generated reports will be saved.

.PARAMETER TargetDirectory
    Directory containing the source code that was tested.

.PARAMETER ReportTypes
    Semicolon-separated list of report types to generate (default: HTML and Cobertura).

.RETURNS
    True if report generation was successful, false otherwise.

.EXAMPLE
    Generate-Report -CoverageReportPath ".\TestResults\coverage.cobertura.xml" -OutputPath ".\CoverageReport" -TargetDirectory ".\src"
#>
function Generate-Report {
    param (
        [Parameter(Mandatory = $true)]
        [string]$CoverageReportPath,
        
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        
        [Parameter(Mandatory = $true)]
        [string]$TargetDirectory,
        
        [Parameter(Mandatory = $false)]
        [string]$ReportTypes = $DEFAULT_REPORT_TYPES
    )

    try {
        # Validate input parameters
        if (-not (Test-Path $CoverageReportPath)) {
            throw "Coverage report file does not exist: $CoverageReportPath"
        }

        if (-not (Test-Path $TargetDirectory)) {
            throw "Target directory does not exist: $TargetDirectory"
        }

        # Ensure output directory exists
        if (-not (Test-Path $OutputPath)) {
            New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
            Write-Verbose "Created output directory: $OutputPath"
        }

        # Build the reportgenerator command
        $reportGeneratorCmd = "reportgenerator `"-reports:$CoverageReportPath`" `"-targetdir:$OutputPath`" `"-reporttypes:$ReportTypes`" `"-sourcedirs:$TargetDirectory`""
        
        Write-Verbose "Executing command: $reportGeneratorCmd"
        
        # Execute reportgenerator
        $output = Invoke-Expression $reportGeneratorCmd
        
        # Check if the report was generated successfully
        if (Test-Path (Join-Path $OutputPath "index.htm")) {
            Write-Host "Coverage report generated successfully at: $OutputPath" -ForegroundColor Green
            return $true
        } else {
            Write-Error "Failed to generate coverage report."
            Write-Error "Output: $output"
            return $false
        }
    }
    catch {
        Write-Error "Error generating report: $_"
        return $false
    }
}

<#
.SYNOPSIS
    Verifies that code coverage meets or exceeds the specified threshold.

.DESCRIPTION
    Parses a Cobertura XML coverage report and checks if the overall coverage
    percentage meets or exceeds the specified threshold.

.PARAMETER CoverageReportPath
    Path to the Cobertura XML coverage report file.

.PARAMETER Threshold
    The minimum acceptable coverage percentage (default: 80).

.RETURNS
    True if coverage meets or exceeds threshold, false otherwise.

.EXAMPLE
    Verify-CodeCoverage -CoverageReportPath ".\CoverageReport\Cobertura.xml" -Threshold 85
#>
function Verify-CodeCoverage {
    param (
        [Parameter(Mandatory = $true)]
        [string]$CoverageReportPath,
        
        [Parameter(Mandatory = $false)]
        [double]$Threshold = $DEFAULT_COVERAGE_THRESHOLD
    )

    try {
        # Validate input parameters
        if (-not (Test-Path $CoverageReportPath)) {
            throw "Coverage report file does not exist: $CoverageReportPath"
        }

        # Parse the Cobertura XML report
        $coverageMetrics = Parse-CoberturaReport -ReportPath $CoverageReportPath
        
        # Get the line coverage percentage
        $lineCoveragePercentage = $coverageMetrics.LineCoveragePercentage
        
        # Compare with threshold
        $meetsThreshold = $lineCoveragePercentage -ge $Threshold
        
        # Format the coverage summary
        $formattedSummary = Format-CoverageReport -CoverageMetrics $coverageMetrics
        
        # Determine the color based on whether the threshold is met
        $color = if ($meetsThreshold) { "Green" } else { "Red" }
        
        # Output the result
        Write-Host "`nCODE COVERAGE RESULTS:" -ForegroundColor Cyan
        Write-Host $formattedSummary
        Write-Host "`nThreshold: " -NoNewline
        Write-Host "$Threshold%" -ForegroundColor Yellow
        Write-Host "Result: " -NoNewline
        
        if ($meetsThreshold) {
            Write-Host "PASSED" -ForegroundColor Green
            Write-Host "Coverage meets or exceeds the threshold of $Threshold%`n"
        } else {
            Write-Host "FAILED" -ForegroundColor Red
            Write-Host "Coverage is below the threshold of $Threshold%`n"
        }
        
        return $meetsThreshold
    }
    catch {
        Write-Error "Error verifying code coverage: $_"
        return $false
    }
}

<#
.SYNOPSIS
    Parses a Cobertura XML coverage report to extract coverage metrics.

.DESCRIPTION
    Extracts line, branch, and method coverage metrics from a Cobertura XML report.

.PARAMETER ReportPath
    Path to the Cobertura XML coverage report file.

.RETURNS
    A hashtable containing coverage metrics including line, branch, and method coverage.

.EXAMPLE
    Parse-CoberturaReport -ReportPath ".\CoverageReport\Cobertura.xml"
#>
function Parse-CoberturaReport {
    param (
        [Parameter(Mandatory = $true)]
        [string]$ReportPath
    )

    try {
        # Validate the report path
        if (-not (Test-Path $ReportPath)) {
            throw "Report file does not exist: $ReportPath"
        }

        # Load the XML document
        $xml = [System.Xml.Linq.XDocument]::Load($ReportPath)
        $coverageElement = $xml.Root

        # Extract coverage metrics
        $linesCovered = [double]::Parse($coverageElement.Attribute("lines-covered").Value)
        $linesValid = [double]::Parse($coverageElement.Attribute("lines-valid").Value)
        
        # Branch coverage might not be available in all reports
        $branchesCovered = 0
        $branchesValid = 0
        
        if ($coverageElement.Attribute("branches-covered") -and $coverageElement.Attribute("branches-valid")) {
            $branchesCovered = [double]::Parse($coverageElement.Attribute("branches-covered").Value)
            $branchesValid = [double]::Parse($coverageElement.Attribute("branches-valid").Value)
        }
        
        # Calculate coverage percentages
        $lineCoveragePercentage = if ($linesValid -gt 0) { [Math]::Round(($linesCovered / $linesValid) * 100, 2) } else { 0 }
        $branchCoveragePercentage = if ($branchesValid -gt 0) { [Math]::Round(($branchesCovered / $branchesValid) * 100, 2) } else { 0 }
        
        # Return the coverage metrics as a hashtable
        return @{
            LinesCovered = $linesCovered
            LinesValid = $linesValid
            LineCoveragePercentage = $lineCoveragePercentage
            BranchesCovered = $branchesCovered
            BranchesValid = $branchesValid
            BranchCoveragePercentage = $branchCoveragePercentage
        }
    }
    catch {
        Write-Error "Error parsing Cobertura report: $_"
        throw
    }
}

<#
.SYNOPSIS
    Formats coverage metrics into a readable summary.

.DESCRIPTION
    Takes a hashtable of coverage metrics and formats it into a readable string
    with color coding based on coverage thresholds.

.PARAMETER CoverageMetrics
    A hashtable containing coverage metrics from Parse-CoberturaReport.

.RETURNS
    A formatted string containing the coverage summary.

.EXAMPLE
    Format-CoverageReport -CoverageMetrics $metrics
#>
function Format-CoverageReport {
    param (
        [Parameter(Mandatory = $true)]
        [hashtable]$CoverageMetrics
    )

    # Define thresholds for color coding
    $goodThreshold = 80
    $warningThreshold = 60

    # Format line coverage
    $lineCoverage = $CoverageMetrics.LineCoveragePercentage
    $lineColor = Get-CoverageColor -CoveragePercentage $lineCoverage -GoodThreshold $goodThreshold -WarningThreshold $warningThreshold
    $lineText = "Line Coverage: $lineCoverage% ($($CoverageMetrics.LinesCovered)/$($CoverageMetrics.LinesValid))"
    
    # Format branch coverage if available
    $branchText = ""
    if ($CoverageMetrics.BranchesValid -gt 0) {
        $branchCoverage = $CoverageMetrics.BranchCoveragePercentage
        $branchColor = Get-CoverageColor -CoveragePercentage $branchCoverage -GoodThreshold $goodThreshold -WarningThreshold $warningThreshold
        $branchText = "`nBranch Coverage: $branchCoverage% ($($CoverageMetrics.BranchesCovered)/$($CoverageMetrics.BranchesValid))"
    }
    
    # Build the summary string
    $summary = ""
    Write-ColoredOutput -Text $lineText -Color $lineColor
    
    if ($branchText) {
        Write-ColoredOutput -Text $branchText -Color $branchColor
    }
    
    return $summary
}

<#
.SYNOPSIS
    Determines the color code for a coverage percentage based on thresholds.

.DESCRIPTION
    Returns a color code (Green, Yellow, or Red) based on how the coverage
    percentage compares to the specified thresholds.

.PARAMETER CoveragePercentage
    The coverage percentage to evaluate.

.PARAMETER GoodThreshold
    The threshold above which coverage is considered good (green).

.PARAMETER WarningThreshold
    The threshold above which coverage is considered warning (yellow).
    Below this threshold is considered poor (red).

.RETURNS
    A string representing the color code: "Green", "Yellow", or "Red".

.EXAMPLE
    Get-CoverageColor -CoveragePercentage 85 -GoodThreshold 80 -WarningThreshold 60
#>
function Get-CoverageColor {
    param (
        [Parameter(Mandatory = $true)]
        [double]$CoveragePercentage,
        
        [Parameter(Mandatory = $true)]
        [double]$GoodThreshold,
        
        [Parameter(Mandatory = $true)]
        [double]$WarningThreshold
    )
    
    if ($CoveragePercentage -ge $GoodThreshold) {
        return "Green"
    }
    elseif ($CoveragePercentage -ge $WarningThreshold) {
        return "Yellow"
    }
    else {
        return "Red"
    }
}

<#
.SYNOPSIS
    Writes text to the console with the specified color.

.DESCRIPTION
    Temporarily changes the console foreground color, writes the text,
    and then restores the original color.

.PARAMETER Text
    The text to write to the console.

.PARAMETER Color
    The color to use for the text.

.EXAMPLE
    Write-ColoredOutput -Text "Success!" -Color "Green"
#>
function Write-ColoredOutput {
    param (
        [Parameter(Mandatory = $true)]
        [string]$Text,
        
        [Parameter(Mandatory = $true)]
        [string]$Color
    )
    
    $originalColor = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $Color
    Write-Host $Text
    $host.UI.RawUI.ForegroundColor = $originalColor
}

# Export the functions that should be available to other scripts
Export-ModuleMember -Function Generate-Report
Export-ModuleMember -Function Verify-CodeCoverage
Export-ModuleMember -Function Parse-CoberturaReport
Export-ModuleMember -Function Format-CoverageReport

# Main script execution when running directly (not imported as a module)
if ($MyInvocation.InvocationName -eq $MyInvocation.MyCommand.Name) {
    # Check parameters and execute appropriate functions based on script arguments
    param (
        [Parameter(Mandatory = $false)]
        [switch]$Verify,
        
        [Parameter(Mandatory = $false)]
        [string]$CoverageReportPath,
        
        [Parameter(Mandatory = $false)]
        [string]$OutputPath,
        
        [Parameter(Mandatory = $false)]
        [string]$TargetDirectory,
        
        [Parameter(Mandatory = $false)]
        [string]$ReportTypes = $DEFAULT_REPORT_TYPES,
        
        [Parameter(Mandatory = $false)]
        [double]$Threshold = $DEFAULT_COVERAGE_THRESHOLD
    )
    
    # Print script header
    Write-Host "`n=== Security Patrol Test Report Generator ===" -ForegroundColor Cyan
    Write-Host "Current directory: $(Get-Location)`n"
    
    if ($Verify) {
        # Verify code coverage if the -Verify switch is used
        if ([string]::IsNullOrEmpty($CoverageReportPath)) {
            Write-Error "Error: CoverageReportPath parameter is required when using the -Verify switch."
            exit 1
        }
        
        $result = Verify-CodeCoverage -CoverageReportPath $CoverageReportPath -Threshold $Threshold
        
        if (-not $result) {
            # Exit with error code if coverage threshold is not met
            exit 1
        }
    }
    else {
        # Generate report if -Verify switch is not used
        if ([string]::IsNullOrEmpty($CoverageReportPath) -or 
            [string]::IsNullOrEmpty($OutputPath) -or 
            [string]::IsNullOrEmpty($TargetDirectory)) {
            Write-Error "Error: CoverageReportPath, OutputPath, and TargetDirectory parameters are required."
            exit 1
        }
        
        $result = Generate-Report -CoverageReportPath $CoverageReportPath -OutputPath $OutputPath -TargetDirectory $TargetDirectory -ReportTypes $ReportTypes
        
        if (-not $result) {
            # Exit with error code if report generation fails
            exit 1
        }
    }
    
    # Script completed successfully
    Write-Host "`nScript completed successfully.`n" -ForegroundColor Green
    exit 0
}