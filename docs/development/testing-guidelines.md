# Security Patrol Application - Testing Guidelines

This document provides comprehensive guidelines for testing the Security Patrol application. It covers test organization, best practices, and implementation details for different test types including unit, integration, UI, performance, security, and end-to-end testing.

## 1. Test Organization

The test suite is organized into the following main directories:

- **Common**: Shared test utilities, mock implementations, test data generators, and fixtures used across all test projects
- **MAUI**: Tests specific to the .NET MAUI mobile application
- **API**: Tests specific to the backend API services
- **EndToEnd**: End-to-end tests that validate complete user flows across mobile and backend components
- **SecurityScanning**: Security tests and vulnerability scanning
- **Automation**: CI/CD pipeline configurations and automation scripts

### Test Projects

#### Common
- **SecurityPatrol.TestCommon**: Common test utilities, mocks, test data generators, and fixtures

#### MAUI
- **SecurityPatrol.MAUI.UnitTests**: Unit tests for mobile app components
- **SecurityPatrol.MAUI.IntegrationTests**: Integration tests for mobile app components
- **SecurityPatrol.MAUI.UITests**: UI automation tests for mobile app
- **SecurityPatrol.MAUI.PerformanceTests**: Performance tests for mobile app

#### API
- **SecurityPatrol.API.UnitTests**: Unit tests for backend API components
- **SecurityPatrol.API.IntegrationTests**: Integration tests for backend API components
- **SecurityPatrol.API.PerformanceTests**: Performance tests for backend API components

#### EndToEnd
- **SecurityPatrol.E2ETests**: End-to-end tests for complete user flows

#### SecurityScanning
- **SecurityPatrol.SecurityTests**: Security tests for API and mobile components
- **SecurityPatrol.VulnerabilityScan**: Vulnerability scanning for dependencies and code analysis

## 2. Test Types and Approaches

### 2.1 Unit Testing

Unit tests focus on testing individual components in isolation, using mocks for dependencies.

**Key Characteristics:**
- Fast execution
- No external dependencies
- Tests a single unit of functionality
- Uses mocking for dependencies

**Base Class:** `TestBase` in `SecurityPatrol.MAUI.UnitTests.Setup`

**Example:**
```csharp
[Fact]
public async Task AuthenticationService_VerifyCode_WithValidCode_ReturnsTrue()
{
    // Arrange
    MockApiService.Setup(x => x.PostAsync<AuthenticationResponse>(
        It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
        .ReturnsAsync(new AuthenticationResponse { Token = "valid-token", ExpiresAt = DateTime.UtcNow.AddHours(1) });
    
    var service = new AuthenticationService(MockApiService.Object, MockTokenManager.Object);
    
    // Act
    var result = await service.VerifyCode(TestConstants.TestVerificationCode);
    
    // Assert
    result.Should().BeTrue();
}
```

### 2.2 Integration Testing

Integration tests verify that components work together correctly, using real implementations with controlled test environments.

**Key Characteristics:**
- Tests interaction between multiple components
- Uses real implementations with test doubles for external systems
- Requires more setup than unit tests
- May use in-memory databases or mock API servers

**Base Class:** `IntegrationTestBase` in `SecurityPatrol.MAUI.IntegrationTests.Setup`

**Example:**
```csharp
[Fact]
public async Task AuthenticationService_VerifyCode_IntegratesWithApiService()
{
    // Arrange
    ApiServer.SetupSuccessResponse("/auth/validate", new AuthenticationResponse
    {
        Token = "valid-token",
        ExpiresAt = DateTime.UtcNow.AddHours(1)
    });
    
    // Act
    var result = await AuthenticationService.VerifyCode(TestConstants.TestVerificationCode);
    
    // Assert
    result.Should().BeTrue();
}
```

### 2.3 UI Testing

UI tests automate user interactions with the application interface to verify correct behavior.

**Key Characteristics:**
- Tests the application from a user's perspective
- Interacts with UI elements
- Verifies visual elements and workflows
- Slower execution than unit or integration tests

**Base Class:** `UITestBase` in `SecurityPatrol.MAUI.UITests.Setup`

**Example:**
```csharp
[Test]
public void Login_WithValidCredentials_NavigatesToMainPage()
{
    // Arrange - App is started in SetUp method
    
    // Act
    EnterText("PhoneEntryField", TestConstants.TestPhoneNumber);
    TapElement("RequestCodeButton");
    WaitForElement("VerificationCodeField");
    EnterText("VerificationCodeField", TestConstants.TestVerificationCode);
    TapElement("VerifyButton");
    
    // Assert
    AssertElementExists("MainPageTitle");
}
```

### 2.4 Performance Testing

Performance tests measure and validate the application's performance characteristics.

**Key Characteristics:**
- Measures execution time, memory usage, and other performance metrics
- Validates against defined performance thresholds
- May simulate different device conditions
- Requires stable test environment for consistent results

**Base Class:** `PerformanceTestBase` in `SecurityPatrol.MAUI.PerformanceTests.Setup`

**Example:**
```csharp
[Fact]
public async Task PhotoService_CapturePhoto_MeetsPerformanceRequirements()
{
    // Arrange
    const int expectedMaxDuration = 1500; // 1.5 seconds
    const double expectedMaxMemoryMB = 10; // 10 MB
    
    // Act
    var (averageTime, averageMemory) = await RunBenchmarkAsync(
        async () => await PhotoService.CapturePhoto(),
        "PhotoCapture",
        iterations: 5);
    
    // Assert
    AssertPerformanceThreshold(averageTime, expectedMaxDuration, "Photo capture time");
    AssertMemoryThreshold(averageMemory, expectedMaxMemoryMB, "Photo capture memory");
}
```

### 2.5 Security Testing

Security tests verify that the application properly implements security controls and is resistant to common vulnerabilities.

**Key Characteristics:**
- Tests authentication and authorization mechanisms
- Validates data protection measures
- Checks for common vulnerabilities (XSS, SQL injection, etc.)
- May use automated scanning tools

**Base Class:** `SecurityTestBase` in `SecurityPatrol.SecurityTests.Setup`

**Example:**
```csharp
[Fact]
public async Task AuthToken_HasSecureConfiguration()
{
    // Arrange
    var token = await GetAuthenticationToken();
    
    // Act
    var isSecure = ValidateTokenSecurity(token);
    
    // Assert
    isSecure.Should().BeTrue();
}

[Fact]
public async Task Api_Endpoints_UseSecureHeaders()
{
    // Arrange
    var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/patrol/locations", TestConstants.TestAuthToken);
    
    // Act
    var response = await HttpClient.SendAsync(request);
    var hasSecureHeaders = ValidateSecureHeaders(response);
    
    // Assert
    hasSecureHeaders.Should().BeTrue();
}
```

### 2.6 End-to-End Testing

End-to-end tests validate complete user flows across the entire application stack.

**Key Characteristics:**
- Tests complete user workflows
- Exercises all layers of the application
- Validates integration with external systems
- Closest to real-world usage scenarios

**Base Class:** `E2ETestBase` in `SecurityPatrol.E2ETests.Setup`

**Example:**
```csharp
[Fact]
public async Task CompletePatrolFlow_SuccessfullyCompletesAllSteps()
{
    // Arrange
    await AuthenticateAsync();
    await ClockInAsync();
    
    // Act
    var result = await ExecuteCompletePatrolFlowAsync();
    
    // Assert
    result.Should().BeTrue();
    
    // Verify data was properly synchronized
    var syncResult = await SyncDataAsync();
    syncResult.Should().BeTrue();
}
```

## 3. Test Environment Setup

### 3.1 Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or later (recommended)
- .NET MAUI workload installed
- Android SDK for mobile app testing
- xUnit and NUnit test runners

### 3.2 Local Development Environment

The local development environment uses:

- In-memory databases for integration tests
- Mock API servers for service testing
- Local file system for storage operations
- Emulators for UI testing

### 3.3 CI Environment

The CI environment includes:

- Dedicated test databases
- Mock external services
- Isolated storage containers
- Automated test runners
- Code coverage reporting

### 3.4 End-to-End Environment

The end-to-end test environment consists of:

- Fully deployed backend services
- Android emulators for mobile app testing
- Controlled test data and state
- Network simulation capabilities

## 4. Test Data Management

### 4.1 Test Data Sources

Test data is managed through the following approaches:

- **Mock Data Generators**: Located in `Common/SecurityPatrol.TestCommon/Data`
- **Test Constants**: Shared values and configuration in `Common/SecurityPatrol.TestCommon/Constants`
- **Fixtures**: Reusable test environment setup in `Common/SecurityPatrol.TestCommon/Fixtures`
- **AutoFixture**: Used for generating random test data in unit tests

### 4.2 Test Data Isolation

Test data is isolated between test runs to ensure test independence and repeatability:

- Each test should create its own data or use fixtures that reset between tests
- Database tests should use transactions that are rolled back after test completion
- Tests should not depend on data created by other tests
- Shared test data should be created through fixtures with proper cleanup

### 4.3 Sensitive Test Data

For security testing, sensitive test data should be:

- Never committed to source control
- Generated dynamically when possible
- Stored securely when static data is required
- Properly sanitized in test reports and logs

## 5. Mocking Strategy

### 5.1 Mocking Framework

The test suite uses Moq as the primary mocking framework. Mock implementations for common services are provided in `Common/SecurityPatrol.TestCommon/Mocks`.

### 5.2 Key Mock Implementations

Key mock implementations include:

- **MockAuthService**: Authentication service mock
- **MockApiService**: API client mock
- **MockLocationService**: Location service mock
- **MockPhotoService**: Photo service mock
- **MockTimeTrackingService**: Time tracking service mock
- **MockPatrolService**: Patrol service mock
- **MockSyncService**: Synchronization service mock
- **MockApiServer**: HTTP server mock for API testing

### 5.3 Mocking Best Practices

- Mock only the direct dependencies of the system under test
- Avoid excessive stubbing - only stub methods that will be called
- Use strict mocks to catch unexpected calls
- Verify important interactions with mocks
- Keep mock setup simple and readable

### 5.4 Example Mock Setup

```csharp
// Setup a mock API service for authentication testing
MockApiService.Setup(x => x.PostAsync<AuthenticationResponse>(
    It.Is<string>(url => url.Contains("/auth/validate")),
    It.IsAny<object>(),
    It.IsAny<bool>()))
    .ReturnsAsync(new AuthenticationResponse 
    { 
        Token = "test-token", 
        ExpiresAt = DateTime.UtcNow.AddHours(1) 
    });

// Verify the mock was called with expected parameters
MockApiService.Verify(x => x.PostAsync<AuthenticationResponse>(
    It.Is<string>(url => url.Contains("/auth/validate")),
    It.Is<object>(obj => obj is VerificationRequest),
    It.IsAny<bool>()),
    Times.Once);
```

## 6. Test Automation

### 6.1 CI/CD Integration

The test suite is integrated with Azure DevOps for continuous integration and automated test execution. The pipeline configuration is defined in `Automation/azure-pipelines-tests.yml`.

#### Pipeline Stages

1. **Build**: Builds the mobile app and backend API components
2. **Test**: Runs unit tests, integration tests, and specialized tests
3. **E2E_Tests**: Runs end-to-end tests on the deployed components

### 6.2 Automated Test Triggers

| Trigger | Test Types | Environment |
|---------|------------|-------------|
| Pull Request | Unit, Integration | Build Agent |
| Merge to Main | Unit, Integration, UI Automation | Test Environment |
| Nightly Build | All Tests + Performance | Test Environment |
| Release Candidate | Full Test Suite | Staging Environment |

### 6.3 Quality Gates

The pipeline enforces the following quality gates:

- All unit tests must pass
- Code coverage must meet the minimum threshold (80%)
- No critical or high security vulnerabilities
- No critical or high code quality issues
- Performance tests must meet defined thresholds

### 6.4 Test Reporting

Test results are reported in multiple formats:

- JUnit XML for test results
- Cobertura XML for code coverage
- HTML reports for human-readable results
- Integration with Azure DevOps Test Plans

### 6.5 Failed Test Handling

- Immediate notification to development team
- Automatic retry for potentially flaky tests (max 3 attempts)
- Detailed failure logs with context information
- Screenshot capture for UI test failures
- Video recording of test execution when possible

## 7. Code Coverage

### 7.1 Coverage Requirements

Code coverage is measured using coverlet.collector and reported in Cobertura format. The minimum coverage requirements are:

| Component | Minimum Coverage |
|-----------|------------------|
| Services | 90% |
| ViewModels | 85% |
| Repositories | 80% |
| Helpers | 80% |
| Overall | 80% |

### 7.2 Coverage Exclusions

Coverage exclusions are defined in `Directory.Build.props` to exclude:

- Generated code
- Test projects
- Third-party libraries
- Platform-specific implementation details
- UI markup (XAML)

### 7.3 Coverage Reporting

Coverage reports are generated during CI/CD pipeline execution and published as build artifacts. Local coverage reports can be generated using:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

and converted to HTML format using ReportGenerator.

## 8. Best Practices

### 8.1 Test Organization

- Group tests by feature or component
- Use descriptive test names that explain the scenario and expected outcome
- Follow the Arrange-Act-Assert pattern
- Keep test files organized and focused

### 8.2 Test Independence

- Tests should be independent and not rely on other tests
- Clean up test data after each test
- Use fresh fixtures for each test
- Avoid shared state between tests

### 8.3 Test Reliability

- Avoid timing-dependent tests
- Use deterministic test data
- Handle asynchronous operations properly
- Implement proper waiting and timeout mechanisms
- Avoid testing implementation details

### 8.4 Test Maintainability

- Use helper methods for common test operations
- Avoid duplication in test code
- Keep tests focused on a single behavior
- Update tests when requirements change
- Document complex test scenarios

### 8.5 Test Naming Conventions

Use the following naming convention for test methods:

```
[ClassUnderTest]_[MethodUnderTest]_[Scenario]_[ExpectedResult]
```

Examples:
- `AuthenticationService_VerifyCode_WithValidCode_ReturnsTrue`
- `TimeTrackingViewModel_ClockIn_WhenAlreadyClockedIn_ShowsError`
- `PatrolService_VerifyCheckpoint_WithInvalidId_ThrowsException`

## 9. Specialized Testing Guidelines

### 9.1 Performance Testing

#### Key Metrics to Measure

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| App Startup | < 2 seconds | Cold start timing |
| Screen Load | < 1 second | UI rendering timing |
| API Response | < 1 second | Network request timing |
| Photo Capture | < 1.5 seconds | Operation timing |
| Memory Usage | < 150MB | Process memory monitoring |
| Battery Impact | < 5% per hour | Battery usage monitoring |

#### Performance Test Environment

- Test on both high-end and low-end device profiles
- Use consistent test data sets
- Run multiple iterations (minimum 5) and use average results
- Include warmup iterations to eliminate JIT compilation effects
- Test under various network conditions

### 9.2 Security Testing

#### Security Test Areas

| Area | Focus | Tools |
|------|-------|-------|
| Authentication | Token security, session management | Manual testing, JWT analyzers |
| Data Protection | Encryption, secure storage | Manual testing, encryption validators |
| API Security | Input validation, secure headers | OWASP ZAP, manual testing |
| Dependency Security | Vulnerable packages | OWASP Dependency Check |
| Code Security | Secure coding practices | Static analysis tools |

#### Security Testing Frequency

- Basic security tests: Every build
- Dependency scanning: Daily
- OWASP ZAP scanning: Weekly
- Comprehensive security review: Monthly
- Penetration testing: Quarterly

### 9.3 Accessibility Testing

#### Accessibility Requirements

| Requirement | Standard | Testing Method |
|-------------|----------|----------------|
| Screen Reader Support | WCAG 2.1 | Manual testing with TalkBack |
| Color Contrast | WCAG AA | Automated + manual testing |
| Touch Target Size | Android guidelines | Manual verification |
| Keyboard Navigation | Android accessibility | Manual verification |

#### Accessibility Testing Tools

- Android Accessibility Scanner
- Contrast Analyzer
- Manual testing with TalkBack screen reader
- Accessibility checklist verification

## 10. Troubleshooting

### 10.1 Common Issues

#### Test Discovery Issues

- Ensure test classes and methods are public
- Verify test methods have appropriate attributes ([Fact], [Theory], [Test])
- Check that test project references are correct
- Verify test SDK packages are installed

#### Mobile App Test Failures

- Check Android SDK and emulator configuration
- Verify app permissions are properly set
- Check for UI changes that might break selectors
- Ensure test environment has necessary resources

#### Database Connection Issues

- Verify connection strings and database availability
- Check that database migrations are applied
- Ensure database user has appropriate permissions
- Check for locked database files

#### Mock Configuration Problems

- Ensure mocks are properly set up and verified
- Check for mismatched parameter types in mock setup
- Verify mock behavior matches expected usage
- Look for missing or incorrect mock setups

### 10.2 Debugging Tests

- Use Visual Studio's test explorer for interactive debugging
- Add logging to tests for better visibility
- Use conditional breakpoints for specific scenarios
- Isolate failing tests by running them individually
- Check test output and logs for error messages

### 10.3 Getting Help

If you encounter issues with the test suite, please:

1. Check existing documentation and troubleshooting guides
2. Review recent changes that might affect the tests
3. Contact the development team via Teams or email
4. Create an issue in the project repository with detailed information

## 11. Contributing to the Test Suite

When adding new tests, please follow these guidelines:

1. Place tests in the appropriate project based on test type
2. Follow the existing naming conventions
3. Ensure tests are independent and reliable
4. Include appropriate assertions to verify behavior
5. Document any special setup or requirements
6. Maintain or improve code coverage
7. Run tests locally before submitting a pull request

All tests should pass locally before submitting a pull request. If you need to modify existing tests, ensure that your changes don't break other tests and maintain the original intent of the test.

## 12. Resources

### 12.1 Documentation

- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [NUnit Documentation](https://docs.nunit.org/)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions Documentation](https://fluentassertions.com/introduction)
- [Xamarin.UITest Documentation](https://docs.microsoft.com/en-us/appcenter/test-cloud/uitest/)

### 12.2 Tools

- [ReportGenerator](https://github.com/danielpalme/ReportGenerator) - For converting coverage reports to HTML
- [BenchmarkDotNet](https://benchmarkdotnet.org/) - For performance benchmarking
- [OWASP ZAP](https://www.zaproxy.org/) - For security testing
- [Accessibility Scanner](https://play.google.com/store/apps/details?id=com.google.android.apps.accessibility.auditor) - For accessibility testing

### 12.3 Internal Resources

- Test Plan: `docs/test-plan.md`
- CI/CD Pipeline Configuration: `Automation/azure-pipelines-tests.yml`
- Test Environment Setup: `docs/test-environment-setup.md`
- Security Testing Guidelines: `docs/security-testing.md`