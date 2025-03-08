using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;
using SecurityPatrol.IntegrationTests.Helpers;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.IntegrationTests.ApiTests
{
    /// <summary>
    /// Integration tests for the authentication API functionality in the Security Patrol application.
    /// </summary>
    public class AuthenticationApiTests : IDisposable
    {
        private readonly MockApiServer _mockApiServer;
        private readonly TestDatabaseInitializer _databaseInitializer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuthenticationApiTests> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthenticationApiTests class and sets up the test environment.
        /// </summary>
        public AuthenticationApiTests()
        {
            // Set up logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<AuthenticationApiTests>();
            
            // Initialize database and mock API server
            _databaseInitializer = new TestDatabaseInitializer(loggerFactory.CreateLogger<TestDatabaseInitializer>());
            _mockApiServer = new MockApiServer(loggerFactory.CreateLogger<MockApiServer>());
            
            // Set up service provider with required services
            _serviceProvider = SetupServices();
            
            // Start the mock API server
            _mockApiServer.Start();
        }

        /// <summary>
        /// Cleans up resources used by the tests.
        /// </summary>
        public void Dispose()
        {
            _mockApiServer.Stop();
            _mockApiServer.Dispose();
        }

        /// <summary>
        /// Sets up the service provider with required services for testing.
        /// </summary>
        /// <returns>The configured service provider.</returns>
        private IServiceProvider SetupServices()
        {
            var services = new ServiceCollection();
            
            // Add logging services
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            // Add mock server and database initializer
            services.AddSingleton(_mockApiServer);
            services.AddSingleton(_databaseInitializer);
            
            // Add mock authentication state provider
            var authStateProviderMock = new Mock<IAuthenticationStateProvider>();
            var authState = AuthState.CreateUnauthenticated();
            authStateProviderMock.Setup(p => p.GetCurrentState()).ReturnsAsync(authState);
            authStateProviderMock.Setup(p => p.UpdateState(It.IsAny<AuthState>())).Callback<AuthState>(newState => authState = newState);
            services.AddSingleton(authStateProviderMock.Object);
            
            // Add mock token manager
            var tokenManagerMock = new Mock<ITokenManager>();
            tokenManagerMock.Setup(t => t.StoreToken(It.IsAny<string>())).Returns(Task.CompletedTask);
            services.AddSingleton(tokenManagerMock.Object);
            
            // Add API service with mock server URL
            services.AddTransient<IApiService>(provider => 
            {
                var httpClient = new HttpClient { BaseAddress = new Uri(_mockApiServer.GetBaseUrl()) };
                return new ApiService(
                    httpClient,
                    provider.GetRequiredService<ITokenManager>(),
                    Mock.Of<INetworkService>(n => n.IsConnected == true && 
                        n.ShouldAttemptOperation(It.IsAny<NetworkOperationType>()) == true),
                    Mock.Of<ITelemetryService>()
                );
            });
            
            // Add authentication service
            services.AddTransient<AuthenticationService>();
            
            // Add test authentication handler
            services.AddTransient<TestAuthenticationHandler>();
            
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Initializes the test environment before each test.
        /// </summary>
        private async Task InitializeTestAsync()
        {
            await _databaseInitializer.ResetDatabaseAsync();
            _mockApiServer.ResetMappings();
        }

        [Fact]
        public async Task Test_RequestVerificationCode_Success()
        {
            // Arrange
            await InitializeTestAsync();
            
            // Setup mock API to return success for verification request
            var verificationResponse = new { verificationId = Guid.NewGuid().ToString() };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, verificationResponse);
            
            // Get the authentication service from the service provider
            var authService = _serviceProvider.GetRequiredService<AuthenticationService>();
            
            // Act
            bool result = await authService.RequestVerificationCode("+12345678901");
            
            // Assert
            Assert.True(result);
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.AuthVerify));
            
            // Check that the request body contains the correct phone number
            var requestBody = _mockApiServer.GetLastRequestBody(ApiEndpoints.AuthVerify);
            Assert.Contains("+12345678901", requestBody);
        }
        
        [Fact]
        public async Task Test_RequestVerificationCode_Failure()
        {
            // Arrange
            await InitializeTestAsync();
            
            // Setup mock API to return error for verification request
            _mockApiServer.SetupErrorResponse(ApiEndpoints.AuthVerify, 400, "Invalid phone number");
            
            // Get the authentication service from the service provider
            var authService = _serviceProvider.GetRequiredService<AuthenticationService>();
            
            // Act
            bool result = await authService.RequestVerificationCode("+12345678901");
            
            // Assert
            Assert.False(result);
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.AuthVerify));
        }
        
        [Fact]
        public async Task Test_VerifyCode_Success()
        {
            // Arrange
            await InitializeTestAsync();
            
            // Setup mock API to return success for both endpoints
            var verificationResponse = new { verificationId = Guid.NewGuid().ToString() };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, verificationResponse);
            
            var validateResponse = new { 
                token = $"mock_token_{Guid.NewGuid()}", 
                expiresAt = DateTime.UtcNow.AddHours(8).ToString("o") 
            };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthValidate, validateResponse);
            
            // Get the authentication service from the service provider
            var authService = _serviceProvider.GetRequiredService<AuthenticationService>();
            
            // Act
            // First request the verification code
            await authService.RequestVerificationCode("+12345678901");
            // Then verify the code
            bool result = await authService.VerifyCode("123456");
            
            // Assert
            Assert.True(result);
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.AuthVerify));
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.AuthValidate));
            
            // Check that the request body contains the correct data
            var requestBody = _mockApiServer.GetLastRequestBody(ApiEndpoints.AuthValidate);
            Assert.Contains("+12345678901", requestBody);
            Assert.Contains("123456", requestBody);
            
            // Check authentication state
            var authState = await _serviceProvider.GetRequiredService<IAuthenticationStateProvider>().GetCurrentState();
            Assert.True(authState.IsAuthenticated);
        }
        
        [Fact]
        public async Task Test_VerifyCode_Failure()
        {
            // Arrange
            await InitializeTestAsync();
            
            // Setup mock API to return success for verification but error for validation
            var verificationResponse = new { verificationId = Guid.NewGuid().ToString() };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, verificationResponse);
            
            _mockApiServer.SetupErrorResponse(ApiEndpoints.AuthValidate, 401, "Invalid verification code");
            
            // Get the authentication service from the service provider
            var authService = _serviceProvider.GetRequiredService<AuthenticationService>();
            
            // Act
            // First request the verification code
            await authService.RequestVerificationCode("+12345678901");
            // Then verify the code
            bool result = await authService.VerifyCode("123456");
            
            // Assert
            Assert.False(result);
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.AuthVerify));
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.AuthValidate));
            
            // Check authentication state
            var authState = await _serviceProvider.GetRequiredService<IAuthenticationStateProvider>().GetCurrentState();
            Assert.False(authState.IsAuthenticated);
        }
        
        [Fact]
        public async Task Test_VerifyCode_InvalidCode()
        {
            // Arrange
            await InitializeTestAsync();
            
            // Setup mock API to return success for verification
            var verificationResponse = new { verificationId = Guid.NewGuid().ToString() };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, verificationResponse);
            
            // Get the authentication service from the service provider
            var authService = _serviceProvider.GetRequiredService<AuthenticationService>();
            
            // Act
            // First request the verification code
            await authService.RequestVerificationCode("+12345678901");
            // Then verify with an invalid code
            bool result = await authService.VerifyCode("12345"); // Only 5 digits, not 6
            
            // Assert
            Assert.False(result);
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.AuthVerify));
            Assert.Equal(0, _mockApiServer.GetRequestCount(ApiEndpoints.AuthValidate)); // API should not be called with invalid code
            
            // Check authentication state
            var authState = await _serviceProvider.GetRequiredService<IAuthenticationStateProvider>().GetCurrentState();
            Assert.False(authState.IsAuthenticated);
        }
        
        [Fact]
        public async Task Test_VerifyCode_NoPhoneNumber()
        {
            // Arrange
            await InitializeTestAsync();
            
            // Get the authentication service from the service provider
            var authService = _serviceProvider.GetRequiredService<AuthenticationService>();
            
            // Act
            // Try to verify code without first requesting a verification code
            bool result = await authService.VerifyCode("123456");
            
            // Assert
            Assert.False(result);
            Assert.Equal(0, _mockApiServer.GetRequestCount(ApiEndpoints.AuthValidate)); // API should not be called
            
            // Check authentication state
            var authState = await _serviceProvider.GetRequiredService<IAuthenticationStateProvider>().GetCurrentState();
            Assert.False(authState.IsAuthenticated);
        }
        
        [Fact]
        public async Task Test_FullAuthenticationFlow()
        {
            // Arrange
            await InitializeTestAsync();
            
            // Setup mock API to return success for both endpoints
            var verificationResponse = new { verificationId = Guid.NewGuid().ToString() };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, verificationResponse);
            
            var validateResponse = new { 
                token = $"mock_token_{Guid.NewGuid()}", 
                expiresAt = DateTime.UtcNow.AddHours(8).ToString("o") 
            };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthValidate, validateResponse);
            
            // Get the authentication service from the service provider
            var authService = _serviceProvider.GetRequiredService<AuthenticationService>();
            const string phoneNumber = "+12345678901";
            
            // Act - Request verification code
            bool requestResult = await authService.RequestVerificationCode(phoneNumber);
            Assert.True(requestResult);
            
            // Act - Verify code
            bool verifyResult = await authService.VerifyCode("123456");
            Assert.True(verifyResult);
            
            // Assert - Check authentication state
            var authState = await authService.GetAuthenticationState();
            Assert.True(authState.IsAuthenticated);
            Assert.Equal(phoneNumber, authState.PhoneNumber);
            
            // Assert - Check API calls
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.AuthVerify));
            Assert.Equal(1, _mockApiServer.GetRequestCount(ApiEndpoints.AuthValidate));
        }
        
        [Fact]
        public async Task Test_AuthenticationWithTestHandler()
        {
            // Arrange
            await InitializeTestAsync();
            
            // Get the test authentication handler from the service provider
            var authHandler = _serviceProvider.GetRequiredService<TestAuthenticationHandler>();
            const string phoneNumber = "+12345678901";
            
            // Act - Request verification code
            bool requestResult = await authHandler.RequestVerificationCode(phoneNumber);
            Assert.True(requestResult);
            
            // Act - Verify code (using default verification code from handler which is "123456")
            bool verifyResult = await authHandler.VerifyCode("123456");
            Assert.True(verifyResult);
            
            // Assert - Check authentication state
            var authState = await authHandler.GetAuthenticationState();
            Assert.True(authState.IsAuthenticated);
            Assert.Equal(phoneNumber, authState.PhoneNumber);
        }
        
        [Fact]
        public async Task Test_AuthenticationFailureWithTestHandler()
        {
            // Arrange
            await InitializeTestAsync();
            
            // Get the test authentication handler from the service provider
            var authHandler = _serviceProvider.GetRequiredService<TestAuthenticationHandler>();
            const string phoneNumber = "+12345678901";
            
            // Act - Set handler to fail
            authHandler.SetShouldSucceed(false);
            
            // Act - Request verification code
            bool requestResult = await authHandler.RequestVerificationCode(phoneNumber);
            Assert.False(requestResult);
            
            // Reset handler to succeed for next test
            authHandler.SetShouldSucceed(true);
            requestResult = await authHandler.RequestVerificationCode(phoneNumber);
            Assert.True(requestResult);
            
            // Act - Set incorrect verification code
            authHandler.SetVerificationCode("654321");
            
            // Verify with a different code
            bool verifyResult = await authHandler.VerifyCode("123456");
            Assert.False(verifyResult);
            
            // Assert - Check authentication state
            var authState = await authHandler.GetAuthenticationState();
            Assert.False(authState.IsAuthenticated);
        }
    }
}