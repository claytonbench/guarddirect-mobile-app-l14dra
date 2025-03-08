# Security Patrol Application - Test Suite

This directory contains the comprehensive test suite for the Security Patrol application, including unit tests, integration tests, UI tests, performance tests, security tests, and end-to-end tests for both the mobile application and backend API components.

## Test Structure

The test suite is organized into the following main directories:

- **Common**: Shared test utilities, mock implementations, test data generators, and fixtures used across all test projects
- **MAUI**: Tests specific to the .NET MAUI mobile application
- **API**: Tests specific to the backend API services
- **EndToEnd**: End-to-end tests that validate complete user flows across mobile and backend components
- **SecurityScanning**: Security tests and vulnerability scanning
- **Automation**: CI/CD pipeline configurations and automation scripts

## Test Projects

### Common
- **SecurityPatrol.TestCommon**: Common test utilities, mocks, test data generators, and fixtures

### MAUI
- **SecurityPatrol.MAUI.UnitTests**: Unit tests for mobile app components
- **SecurityPatrol.MAUI.IntegrationTests**: Integration tests for mobile app components
- **SecurityPatrol.MAUI.UITests**: UI automation tests for mobile app
- **SecurityPatrol.MAUI.PerformanceTests**: Performance tests for mobile app

### API
- **SecurityPatrol.API.UnitTests**: Unit tests for backend API components
- **SecurityPatrol.API.IntegrationTests**: Integration tests for backend API components
- **SecurityPatrol.API.PerformanceTests**: Performance tests for backend API components

### EndToEnd
- **SecurityPatrol.E2ETests**: End-to-end tests for complete user flows

### SecurityScanning
- **SecurityPatrol.SecurityTests**: Security tests for API and mobile components
- **SecurityPatrol.VulnerabilityScan**: Vulnerability scanning for dependencies and code analysis

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or later (recommended)
- .NET MAUI workload installed
- Android SDK for mobile app testing

### Building the Test Suite

```bash
dotnet build SecurityPatrol.Tests.sln
```

### Running Tests

#### Running All Tests
```bash
dotnet test SecurityPatrol.Tests.sln
```

#### Running Specific Test Projects
```bash
dotnet test src/test/MAUI/SecurityPatrol.MAUI.UnitTests/SecurityPatrol.MAUI.UnitTests.csproj
```

#### Running Tests with Coverage
```bash
dotnet test SecurityPatrol.Tests.sln --collect:"XPlat Code Coverage"
```

## Test Automation

The test suite is integrated with Azure DevOps for continuous integration and automated test execution. The pipeline configuration is defined in `Automation/azure-pipelines-tests.yml`.

### CI/CD Pipeline

The pipeline includes the following stages:

1. **Build**: Builds the mobile app and backend API components
2. **Test**: Runs unit tests, integration tests, and specialized tests
3. **E2E_Tests**: Runs end-to-end tests on the deployed components

### Quality Gates

The pipeline enforces the following quality gates:

- All unit tests must pass
- Code coverage must meet the minimum threshold (80%)
- No critical or high security vulnerabilities
- No critical or high code quality issues

## Test Environment

### Local Development Environment

- Uses in-memory databases for integration tests
- Mock implementations for external services
- Local file system for storage operations

### CI Environment

- Dedicated test databases
- Mock external services
- Isolated storage containers

### End-to-End Environment

- Fully deployed backend services
- Android emulators for mobile app testing
- Controlled test data and state

## Test Data Management

Test data is managed through the following approaches:

- **Mock Data Generators**: Located in `Common/SecurityPatrol.TestCommon/Data`
- **Test Constants**: Shared values and configuration in `Common/SecurityPatrol.TestCommon/Constants`
- **Fixtures**: Reusable test environment setup in `Common/SecurityPatrol.TestCommon/Fixtures`
- **AutoFixture**: Used for generating random test data in unit tests

Test data is isolated between test runs to ensure test independence and repeatability.

## Mocking Strategy

The test suite uses Moq as the primary mocking framework. Mock implementations for common services are provided in `Common/SecurityPatrol.TestCommon/Mocks`.

Key mock implementations include:

- Authentication services
- Location services
- API clients
- Storage services
- Device capabilities (camera, GPS)

Mock implementations follow the same interfaces as the real implementations to ensure compatibility and realistic behavior.

## Code Coverage

Code coverage is measured using coverlet.collector and reported in Cobertura format. The minimum coverage threshold is 80% for all projects.

Coverage reports are generated during CI/CD pipeline execution and published as build artifacts. Coverage exclusions are defined in `Directory.Build.props` to exclude generated code, test projects, and other non-relevant code.

## Best Practices

### Test Organization

- Group tests by feature or component
- Use descriptive test names that explain the scenario and expected outcome
- Follow the Arrange-Act-Assert pattern

### Test Independence

- Tests should be independent and not rely on other tests
- Clean up test data after each test
- Use fresh fixtures for each test

### Test Reliability

- Avoid timing-dependent tests
- Use deterministic test data
- Handle asynchronous operations properly

### Test Maintainability

- Use helper methods for common test operations
- Avoid duplication in test code
- Keep tests focused on a single behavior

## Contributing

When adding new tests, please follow these guidelines:

1. Place tests in the appropriate project based on test type
2. Follow the existing naming conventions
3. Ensure tests are independent and reliable
4. Include appropriate assertions to verify behavior
5. Document any special setup or requirements

All tests should pass locally before submitting a pull request.

## Troubleshooting

### Common Issues

- **Test Discovery Issues**: Ensure test classes and methods are public
- **Mobile App Test Failures**: Check Android SDK and emulator configuration
- **Database Connection Issues**: Verify connection strings and database availability
- **Mock Configuration Problems**: Ensure mocks are properly set up and verified

### Getting Help

If you encounter issues with the test suite, please contact the development team or create an issue in the project repository.