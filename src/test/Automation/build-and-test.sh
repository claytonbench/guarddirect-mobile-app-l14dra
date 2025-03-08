#!/bin/bash
#
# build-and-test.sh - Build and test automation script for the Security Patrol application
#
# This script automates the build process, runs various test types, collects code coverage,
# and generates test reports for both the MAUI mobile application and backend API components.
#
# It's designed to work in both local development environments and CI/CD pipelines.

# Exit on error, undefined variable, and pipe failures
set -euo pipefail

# Global variables
BUILD_CONFIGURATION="Debug"
TEST_CONFIGURATION="Debug"
COVERAGE_THRESHOLD=80
RESULTS_DIRECTORY="TestResults"
COVERAGE_DIRECTORY="CodeCoverage"
API_SOLUTION_PATH="../../src/SecurityPatrol.API.sln"
MAUI_SOLUTION_PATH="../../src/SecurityPatrol.MAUI.sln"
API_TEST_PROJECT_PATH="../../src/test/API/SecurityPatrol.API.Tests.csproj"
API_INTEGRATION_TEST_PROJECT_PATH="../../src/test/API/SecurityPatrol.API.IntegrationTests.csproj"
MAUI_TEST_PROJECT_PATH="../../src/test/MAUI/SecurityPatrol.MAUI.Tests.csproj"
MAUI_INTEGRATION_TEST_PROJECT_PATH="../../src/test/MAUI/SecurityPatrol.MAUI.IntegrationTests.csproj"
API_SETTINGS_PATH="../API/SecurityPatrol.API.Tests.runsettings"
MAUI_SETTINGS_PATH="../MAUI/SecurityPatrol.MAUI.Tests.runsettings"

# Flags for what to build and test
BUILD_API=false
BUILD_MAUI=false
RUN_SPECIALIZED_TESTS=false
RUN_E2E_TESTS=false

# Initialize the environment for building and testing
initialize_environment() {
    echo "Initializing build and test environment..."
    
    # Create necessary directories
    mkdir -p "${RESULTS_DIRECTORY}"
    mkdir -p "${COVERAGE_DIRECTORY}"
    
    # Verify .NET SDK installation
    if ! command -v dotnet &> /dev/null; then
        echo "Error: .NET SDK not found. Please install the .NET SDK."
        return 1
    fi
    
    # Verify bc command for floating point comparison
    if ! command -v bc &> /dev/null; then
        echo "Warning: 'bc' command not found. Code coverage threshold verification may not work correctly."
        echo "         Please install 'bc' package for accurate coverage verification."
    fi
    
    # Install required .NET tools if not already installed
    if ! dotnet tool list -g | grep -q "dotnet-reportgenerator-globaltool"; then
        echo "Installing ReportGenerator tool..."
        dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.1.19
    fi
    
    # Set environment variables for testing
    export DOTNET_CLI_UI_LANGUAGE=en
    export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
    export DOTNET_NOLOGO=true
    
    echo "Environment initialized successfully."
    return 0
}

# Build the specified solution
build_solution() {
    local solution_path=$1
    local configuration=$2
    
    echo "Building solution: ${solution_path} (${configuration})"
    
    # Restore NuGet packages
    dotnet restore "${solution_path}" || return $?
    
    # Build the solution
    dotnet build "${solution_path}" --configuration "${configuration}" --no-restore || return $?
    
    echo "Build successful."
    return 0
}

# Run tests for the specified project
run_tests() {
    local project_path=$1
    local configuration=$2
    local settings_path=$3
    local results_path=$4
    local collect_coverage=$5
    
    echo "Running tests for: ${project_path}"
    
    # Build command arguments
    local test_args=("test" "${project_path}" "--configuration" "${configuration}" "--no-build")
    
    # Add settings file if provided
    if [[ -n "${settings_path}" ]]; then
        test_args+=("--settings" "${settings_path}")
    fi
    
    # Add results directory if provided
    if [[ -n "${results_path}" ]]; then
        test_args+=("--results-directory" "${results_path}")
    fi
    
    # Add code coverage collection if requested
    if [[ "${collect_coverage}" == true ]]; then
        test_args+=("--collect" "XPlat Code Coverage")
    fi
    
    # Run the tests
    dotnet "${test_args[@]}"
    local result=$?
    
    if [[ ${result} -eq 0 ]]; then
        echo "Tests completed successfully."
    else
        echo "Tests failed with exit code: ${result}"
    fi
    
    return ${result}
}

# Generate code coverage reports
generate_coverage_report() {
    local coverage_report_path=$1
    local output_path=$2
    local target_directory=$3
    
    echo "Generating code coverage report..."
    
    # Run ReportGenerator
    dotnet reportgenerator \
        "-reports:${coverage_report_path}" \
        "-targetdir:${output_path}" \
        "-reporttypes:Html;Cobertura;Badges" \
        "-sourcedirs:${target_directory}" \
        "-verbosity:Warning"
    
    local result=$?
    
    if [[ ${result} -eq 0 ]]; then
        echo "Coverage report generated successfully."
    else
        echo "Failed to generate coverage report with exit code: ${result}"
    fi
    
    return ${result}
}

# Verify code coverage meets threshold
verify_code_coverage() {
    local coverage_report_path=$1
    local threshold=$2
    
    echo "Verifying code coverage meets threshold of ${threshold}%..."
    
    # Check if the coverage report exists
    if [[ ! -f "${coverage_report_path}" ]]; then
        echo "Error: Coverage report not found at ${coverage_report_path}"
        return 1
    fi
    
    # Extract coverage percentage from the Cobertura XML file
    local line_rate=$(grep -o 'line-rate="[0-9.]*"' "${coverage_report_path}" | head -1 | grep -o '[0-9.]*')
    
    if [[ -z "${line_rate}" ]]; then
        echo "Error: Could not extract coverage from ${coverage_report_path}"
        return 1
    fi
    
    # Multiply by 100 to get percentage
    # Use awk if available, otherwise use simple math
    local coverage_percentage=0
    if command -v awk &> /dev/null; then
        coverage_percentage=$(awk "BEGIN {printf \"%.2f\", ${line_rate} * 100}")
    else
        # Basic calculation - less precise but more portable
        coverage_percentage=$(echo "${line_rate} * 100" | bc -l | cut -d. -f1)
    fi
    
    echo "Current code coverage: ${coverage_percentage}%"
    
    # Compare with threshold
    if command -v bc &> /dev/null; then
        if (( $(echo "${coverage_percentage} >= ${threshold}" | bc -l) )); then
            echo "Code coverage meets or exceeds threshold."
            return 0
        else
            echo "Error: Code coverage ${coverage_percentage}% is below threshold ${threshold}%"
            return 1
        fi
    else
        # Fallback to integer comparison if bc is not available
        # This is less accurate but more portable
        if (( ${coverage_percentage%.*} >= ${threshold} )); then
            echo "Code coverage appears to meet or exceed threshold."
            return 0
        else
            echo "Error: Code coverage ${coverage_percentage}% appears to be below threshold ${threshold}%"
            return 1
        fi
    fi
}

# Build and test API components
build_and_test_api() {
    echo "====== Building and testing API components ======"
    local exit_code=0
    
    # Build API solution
    build_solution "${API_SOLUTION_PATH}" "${BUILD_CONFIGURATION}"
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "API build failed with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Run API unit tests with coverage
    echo "Running API unit tests..."
    run_tests "${API_TEST_PROJECT_PATH}" "${TEST_CONFIGURATION}" "${API_SETTINGS_PATH}" "${RESULTS_DIRECTORY}/API" true
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "API unit tests failed with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Run API integration tests with coverage
    echo "Running API integration tests..."
    run_tests "${API_INTEGRATION_TEST_PROJECT_PATH}" "${TEST_CONFIGURATION}" "${API_SETTINGS_PATH}" "${RESULTS_DIRECTORY}/API_Integration" true
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "API integration tests failed with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Find the latest coverage file
    local coverage_file=$(find "${RESULTS_DIRECTORY}/API" -name "coverage.cobertura.xml" | sort | tail -1)
    
    if [[ -z "${coverage_file}" ]]; then
        echo "Error: No coverage file found for API tests"
        return 1
    fi
    
    # Generate coverage report
    generate_coverage_report "${coverage_file}" "${COVERAGE_DIRECTORY}/API" "../../src"
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "Failed to generate API coverage report with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Verify code coverage
    verify_code_coverage "${coverage_file}" "${COVERAGE_THRESHOLD}"
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "API code coverage verification failed"
        return ${exit_code}
    fi
    
    echo "API build and tests completed successfully."
    return 0
}

# Build and test MAUI mobile application
build_and_test_maui() {
    echo "====== Building and testing MAUI mobile application ======"
    local exit_code=0
    
    # Check if MAUI workload is installed and install if needed
    if ! dotnet workload list | grep -q "maui"; then
        echo "Installing MAUI workload..."
        dotnet workload install maui
        exit_code=$?
        
        if [[ ${exit_code} -ne 0 ]]; then
            echo "Failed to install MAUI workload with exit code: ${exit_code}"
            return ${exit_code}
        fi
    fi
    
    # Build MAUI solution
    build_solution "${MAUI_SOLUTION_PATH}" "${BUILD_CONFIGURATION}"
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "MAUI build failed with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Run MAUI unit tests with coverage
    echo "Running MAUI unit tests..."
    run_tests "${MAUI_TEST_PROJECT_PATH}" "${TEST_CONFIGURATION}" "${MAUI_SETTINGS_PATH}" "${RESULTS_DIRECTORY}/MAUI" true
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "MAUI unit tests failed with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Run MAUI integration tests with coverage
    echo "Running MAUI integration tests..."
    run_tests "${MAUI_INTEGRATION_TEST_PROJECT_PATH}" "${TEST_CONFIGURATION}" "${MAUI_SETTINGS_PATH}" "${RESULTS_DIRECTORY}/MAUI_Integration" true
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "MAUI integration tests failed with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Find the latest coverage file
    local coverage_file=$(find "${RESULTS_DIRECTORY}/MAUI" -name "coverage.cobertura.xml" | sort | tail -1)
    
    if [[ -z "${coverage_file}" ]]; then
        echo "Error: No coverage file found for MAUI tests"
        return 1
    fi
    
    # Generate coverage report
    generate_coverage_report "${coverage_file}" "${COVERAGE_DIRECTORY}/MAUI" "../../src"
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "Failed to generate MAUI coverage report with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Verify code coverage
    verify_code_coverage "${coverage_file}" "${COVERAGE_THRESHOLD}"
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "MAUI code coverage verification failed"
        return ${exit_code}
    fi
    
    echo "MAUI build and tests completed successfully."
    return 0
}

# Run specialized tests like security and performance tests
run_specialized_tests() {
    echo "====== Running specialized tests ======"
    local exit_code=0
    
    # Run security tests
    echo "Running security tests..."
    # This would typically call a specialized security testing tool or project
    dotnet test ../../src/test/SecurityPatrol.SecurityTests/SecurityPatrol.SecurityTests.csproj \
        --configuration "${TEST_CONFIGURATION}" \
        --results-directory "${RESULTS_DIRECTORY}/Security" \
        || exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "Security tests failed with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Run vulnerability scan
    echo "Running vulnerability scan..."
    # Execute vulnerability scanning tool if available, otherwise simulate
    if command -v dotnet-security-scan &> /dev/null; then
        dotnet-security-scan ../../src/SecurityPatrol.sln \
            --output-format="json" \
            --output-file="${RESULTS_DIRECTORY}/vulnerability-scan.json" \
            || exit_code=$?
            
        if [[ ${exit_code} -ne 0 ]]; then
            echo "Vulnerability scan failed with exit code: ${exit_code}"
            return ${exit_code}
        fi
    else
        echo "Vulnerability scan skipped (dotnet-security-scan tool not available)"
    fi
    
    # Run API performance tests
    echo "Running API performance tests..."
    dotnet test ../../src/test/SecurityPatrol.PerformanceTests/SecurityPatrol.API.PerformanceTests.csproj \
        --configuration "${TEST_CONFIGURATION}" \
        --results-directory "${RESULTS_DIRECTORY}/Performance" \
        || exit_code=$?
        
    if [[ ${exit_code} -ne 0 ]]; then
        echo "API performance tests failed with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Run MAUI performance tests
    echo "Running MAUI performance tests..."
    dotnet test ../../src/test/SecurityPatrol.PerformanceTests/SecurityPatrol.MAUI.PerformanceTests.csproj \
        --configuration "${TEST_CONFIGURATION}" \
        --results-directory "${RESULTS_DIRECTORY}/Performance" \
        || exit_code=$?
        
    if [[ ${exit_code} -ne 0 ]]; then
        echo "MAUI performance tests failed with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    echo "Specialized tests completed successfully."
    return 0
}

# Run end-to-end tests
run_e2e_tests() {
    echo "====== Running end-to-end tests ======"
    local exit_code=0
    
    # Set up test environment for E2E tests
    echo "Setting up E2E test environment..."
    
    # Start API service for E2E tests if it's not already running
    if ! curl -s http://localhost:5001/api/health > /dev/null; then
        echo "Starting API service for E2E tests..."
        dotnet run --project ../../src/SecurityPatrol.API/SecurityPatrol.API.csproj \
            --configuration "${TEST_CONFIGURATION}" \
            --no-build \
            --urls "http://localhost:5001" > "${RESULTS_DIRECTORY}/api-server.log" 2>&1 &
        API_PID=$!
        
        # Give the API service time to start
        echo "Waiting for API service to start..."
        for i in {1..30}; do
            if curl -s http://localhost:5001/api/health > /dev/null; then
                echo "API service started successfully."
                break
            fi
            
            if [[ $i -eq 30 ]]; then
                echo "Error: API service failed to start within the timeout period."
                kill $API_PID 2>/dev/null || true
                return 1
            fi
            
            sleep 1
        done
    else
        echo "API service is already running."
        API_PID=""
    fi
    
    # Run E2E test suite
    echo "Running E2E tests..."
    dotnet test ../../src/test/SecurityPatrol.E2ETests/SecurityPatrol.E2ETests.csproj \
        --configuration "${TEST_CONFIGURATION}" \
        --results-directory "${RESULTS_DIRECTORY}/E2E" \
        || exit_code=$?
    
    # Clean up test environment
    echo "Cleaning up E2E test environment..."
    if [[ -n "$API_PID" ]]; then
        echo "Stopping API service..."
        kill $API_PID 2>/dev/null || true
    fi
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "E2E tests failed with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    echo "E2E tests completed successfully."
    return 0
}

# Show usage information
show_usage() {
    echo "Usage: $(basename "$0") [options]"
    echo
    echo "Build and test automation script for the Security Patrol application"
    echo
    echo "Options:"
    echo "  -a, --build-api           Build and test API components"
    echo "  -m, --build-maui          Build and test MAUI components"
    echo "  -s, --specialized-tests   Run specialized tests"
    echo "  -e, --e2e-tests           Run end-to-end tests"
    echo "  -c, --configuration       Build configuration (Debug/Release)"
    echo "                            Default: ${BUILD_CONFIGURATION}"
    echo "  -t, --coverage-threshold  Minimum code coverage percentage required"
    echo "                            Default: ${COVERAGE_THRESHOLD}"
    echo "  -o, --output-path         Path for test results and coverage reports"
    echo "                            Default: ${RESULTS_DIRECTORY}"
    echo "  -h, --help                Display this help information"
    echo
    echo "Examples:"
    echo "  $(basename "$0") --build-api --build-maui"
    echo "  $(basename "$0") -a -m -c Release -t 85"
    echo "  $(basename "$0") --build-api --specialized-tests"
}

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            -a|--build-api)
                BUILD_API=true
                shift
                ;;
            -m|--build-maui)
                BUILD_MAUI=true
                shift
                ;;
            -s|--specialized-tests)
                RUN_SPECIALIZED_TESTS=true
                shift
                ;;
            -c|--configuration)
                if [[ -z "$2" || "$2" == -* ]]; then
                    echo "Error: --configuration requires an argument"
                    show_usage
                    return 1
                fi
                BUILD_CONFIGURATION="$2"
                TEST_CONFIGURATION="$2"
                shift 2
                ;;
            -t|--coverage-threshold)
                if [[ -z "$2" || "$2" == -* ]]; then
                    echo "Error: --coverage-threshold requires an argument"
                    show_usage
                    return 1
                fi
                COVERAGE_THRESHOLD="$2"
                shift 2
                ;;
            -o|--output-path)
                if [[ -z "$2" || "$2" == -* ]]; then
                    echo "Error: --output-path requires an argument"
                    show_usage
                    return 1
                fi
                RESULTS_DIRECTORY="$2"
                COVERAGE_DIRECTORY="${RESULTS_DIRECTORY}/coverage"
                shift 2
                ;;
            -e|--e2e-tests)
                RUN_E2E_TESTS=true
                shift
                ;;
            -h|--help)
                show_usage
                exit 0
                ;;
            *)
                echo "Error: Unknown option: $1"
                show_usage
                return 1
                ;;
        esac
    done
    
    # Validate arguments
    if [[ "${BUILD_API}" == false && "${BUILD_MAUI}" == false && "${RUN_SPECIALIZED_TESTS}" == false && "${RUN_E2E_TESTS}" == false ]]; then
        echo "Error: At least one of --build-api, --build-maui, --specialized-tests, or --e2e-tests must be specified."
        show_usage
        return 1
    fi
    
    return 0
}

# Main function
main() {
    local args=("$@")
    local exit_code=0
    local overall_status=0
    
    # Start time for total execution
    local start_time=$(date +%s)
    
    # Parse command line arguments
    parse_arguments "${args[@]}"
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        return ${exit_code}
    fi
    
    # Initialize the environment
    initialize_environment
    exit_code=$?
    
    if [[ ${exit_code} -ne 0 ]]; then
        echo "Failed to initialize environment with exit code: ${exit_code}"
        return ${exit_code}
    fi
    
    # Build and test API components if specified
    if [[ "${BUILD_API}" == true ]]; then
        build_and_test_api
        exit_code=$?
        
        if [[ ${exit_code} -ne 0 ]]; then
            echo "API build and test failed with exit code: ${exit_code}"
            overall_status=1
        fi
    fi
    
    # Build and test MAUI application if specified
    if [[ "${BUILD_MAUI}" == true ]]; then
        build_and_test_maui
        exit_code=$?
        
        if [[ ${exit_code} -ne 0 ]]; then
            echo "MAUI build and test failed with exit code: ${exit_code}"
            overall_status=1
        fi
    fi
    
    # Run specialized tests if specified
    if [[ "${RUN_SPECIALIZED_TESTS}" == true ]]; then
        run_specialized_tests
        exit_code=$?
        
        if [[ ${exit_code} -ne 0 ]]; then
            echo "Specialized tests failed with exit code: ${exit_code}"
            overall_status=1
        fi
    fi
    
    # Run E2E tests if specified
    if [[ "${RUN_E2E_TESTS}" == true ]]; then
        run_e2e_tests
        exit_code=$?
        
        if [[ ${exit_code} -ne 0 ]]; then
            echo "E2E tests failed with exit code: ${exit_code}"
            overall_status=1
        fi
    fi
    
    # End time and calculate duration
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    local minutes=$((duration / 60))
    local seconds=$((duration % 60))
    
    # Output summary
    echo
    echo "====== Build and Test Summary ======"
    echo "Total execution time: ${minutes}m ${seconds}s"
    echo
    
    if [[ "${BUILD_API}" == true ]]; then
        if [[ -d "${COVERAGE_DIRECTORY}/API" ]]; then
            echo "API code coverage report: ${COVERAGE_DIRECTORY}/API/index.html"
        else
            echo "API code coverage report: Not generated"
        fi
    fi
    
    if [[ "${BUILD_MAUI}" == true ]]; then
        if [[ -d "${COVERAGE_DIRECTORY}/MAUI" ]]; then
            echo "MAUI code coverage report: ${COVERAGE_DIRECTORY}/MAUI/index.html"
        else
            echo "MAUI code coverage report: Not generated"
        fi
    fi
    
    echo
    
    if [[ ${overall_status} -eq 0 ]]; then
        echo "Overall status: SUCCESS"
    else
        echo "Overall status: FAILURE"
    fi
    
    return ${overall_status}
}

# Execute main function with all arguments
main "$@"