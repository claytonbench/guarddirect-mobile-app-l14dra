using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.Constants;

namespace SecurityPatrol.IntegrationTests.Helpers
{
    /// <summary>
    /// A test implementation of the IAuthenticationService interface that provides controlled authentication behavior for integration tests.
    /// </summary>
    public class TestAuthenticationHandler : IAuthenticationService
    {
        private string _currentPhoneNumber;
        private string _verificationCode;
        private bool _shouldSucceed;
        private bool _shouldThrowException;
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly MockApiServer _mockApiServer;
        private readonly ILogger<TestAuthenticationHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the TestAuthenticationHandler class with required dependencies.
        /// </summary>
        /// <param name="authStateProvider">The authentication state provider to update authentication state.</param>
        /// <param name="mockApiServer">The mock API server to set up responses.</param>
        /// <param name="logger">The logger to use for test logging.</param>
        public TestAuthenticationHandler(
            IAuthenticationStateProvider authStateProvider,
            MockApiServer mockApiServer,
            ILogger<TestAuthenticationHandler> logger)
        {
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _mockApiServer = mockApiServer ?? throw new ArgumentNullException(nameof(mockApiServer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _currentPhoneNumber = string.Empty;
            _verificationCode = "123456"; // Default verification code
            _shouldSucceed = true; // Default to success
            _shouldThrowException = false;
            
            SetupMockResponses();
        }

        /// <summary>
        /// Simulates requesting a verification code for the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to send the verification code to.</param>
        /// <returns>True if the verification code was successfully requested, otherwise false.</returns>
        public async Task<bool> RequestVerificationCode(string phoneNumber)
        {
            _logger.LogInformation("Test: Requesting verification code for phone number: {PhoneNumber}", phoneNumber);
            
            if (_shouldThrowException)
            {
                throw new Exception("Test exception during verification code request");
            }
            
            _currentPhoneNumber = phoneNumber;
            
            if (!_shouldSucceed)
            {
                return false;
            }
            
            // Setup mock response for verification endpoint
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, new { verificationId = Guid.NewGuid().ToString() });
            
            return true;
        }

        /// <summary>
        /// Simulates verifying the code sent to the user's phone number.
        /// </summary>
        /// <param name="code">The verification code received by the user.</param>
        /// <returns>True if the verification was successful, otherwise false.</returns>
        public async Task<bool> VerifyCode(string code)
        {
            _logger.LogInformation("Test: Verifying code: {Code}", code);
            
            if (_shouldThrowException)
            {
                throw new Exception("Test exception during code verification");
            }
            
            if (string.IsNullOrEmpty(_currentPhoneNumber))
            {
                _logger.LogError("Cannot verify code without a phone number. Call RequestVerificationCode first.");
                return false;
            }
            
            if (code != _verificationCode)
            {
                _logger.LogError("Invalid verification code. Expected: {Expected}, Actual: {Actual}", _verificationCode, code);
                return false;
            }
            
            if (!_shouldSucceed)
            {
                return false;
            }
            
            // Setup mock response for validate endpoint
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthValidate, new 
            { 
                token = $"mock_token_{Guid.NewGuid()}", 
                expiresAt = DateTime.UtcNow.AddHours(8).ToString("o") 
            });
            
            // Create authenticated state
            var authState = AuthState.CreateAuthenticated(_currentPhoneNumber);
            _authStateProvider.UpdateState(authState);
            
            _logger.LogInformation("User successfully authenticated with phone number: {PhoneNumber}", _currentPhoneNumber);
            
            return true;
        }

        /// <summary>
        /// Retrieves the current authentication state of the user.
        /// </summary>
        /// <returns>The current authentication state.</returns>
        public async Task<AuthState> GetAuthenticationState()
        {
            return await _authStateProvider.GetCurrentState();
        }

        /// <summary>
        /// Simulates logging out the current user by clearing authentication state.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Logout()
        {
            _logger.LogInformation("Test: Logging out user");
            
            if (_shouldThrowException)
            {
                throw new Exception("Test exception during logout");
            }
            
            // Update to unauthenticated state
            var authState = AuthState.CreateUnauthenticated();
            _authStateProvider.UpdateState(authState);
            
            // Clear current phone number
            _currentPhoneNumber = string.Empty;
            
            _logger.LogInformation("User successfully logged out");
        }

        /// <summary>
        /// Simulates refreshing the authentication token.
        /// </summary>
        /// <returns>True if the token was successfully refreshed, otherwise false.</returns>
        public async Task<bool> RefreshToken()
        {
            _logger.LogInformation("Test: Refreshing authentication token");
            
            if (_shouldThrowException)
            {
                throw new Exception("Test exception during token refresh");
            }
            
            if (!_shouldSucceed)
            {
                return false;
            }
            
            // Setup mock response for refresh endpoint
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthRefresh, new 
            { 
                token = $"mock_refreshed_token_{Guid.NewGuid()}", 
                expiresAt = DateTime.UtcNow.AddHours(8).ToString("o") 
            });
            
            return true;
        }

        /// <summary>
        /// Sets the verification code that will be considered valid.
        /// </summary>
        /// <param name="code">The verification code to set as valid.</param>
        public void SetVerificationCode(string code)
        {
            _verificationCode = code;
        }

        /// <summary>
        /// Sets whether authentication operations should succeed or fail.
        /// </summary>
        /// <param name="shouldSucceed">True if operations should succeed, false to simulate failures.</param>
        public void SetShouldSucceed(bool shouldSucceed)
        {
            _shouldSucceed = shouldSucceed;
        }

        /// <summary>
        /// Sets whether authentication operations should throw exceptions.
        /// </summary>
        /// <param name="shouldThrow">True if operations should throw exceptions, false otherwise.</param>
        public void SetShouldThrowException(bool shouldThrow)
        {
            _shouldThrowException = shouldThrow;
        }

        /// <summary>
        /// Sets up mock API responses for authentication endpoints.
        /// </summary>
        private void SetupMockResponses()
        {
            // Setup default mock responses for auth endpoints
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, new { verificationId = Guid.NewGuid().ToString() });
            
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthValidate, new 
            { 
                token = $"mock_token_{Guid.NewGuid()}", 
                expiresAt = DateTime.UtcNow.AddHours(8).ToString("o") 
            });
            
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.AuthRefresh, new 
            { 
                token = $"mock_refreshed_token_{Guid.NewGuid()}", 
                expiresAt = DateTime.UtcNow.AddHours(8).ToString("o") 
            });
        }
    }
}