using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of the IAuthenticationService interface that handles phone number verification,
    /// authentication token management, and user session state for the Security Patrol application.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private string _currentPhoneNumber;
        private readonly IApiService _apiService;
        private readonly ITokenManager _tokenManager;
        private readonly IAuthenticationStateProvider _authStateProvider;
        private readonly ILogger<AuthenticationService> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthenticationService class with required dependencies.
        /// </summary>
        /// <param name="apiService">Service for making API requests</param>
        /// <param name="tokenManager">Service for managing authentication tokens</param>
        /// <param name="authStateProvider">Service for managing authentication state</param>
        /// <param name="logger">Logger for recording authentication events</param>
        public AuthenticationService(
            IApiService apiService,
            ITokenManager tokenManager,
            IAuthenticationStateProvider authStateProvider,
            ILogger<AuthenticationService> logger)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentPhoneNumber = string.Empty;
        }

        /// <summary>
        /// Requests a verification code to be sent to the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to send the verification code to</param>
        /// <returns>True if the verification code was successfully requested, otherwise false</returns>
        public async Task<bool> RequestVerificationCode(string phoneNumber)
        {
            _logger.LogInformation("Requesting verification code for phone number: {PhoneNumber}", 
                phoneNumber?.Substring(0, Math.Min(4, phoneNumber?.Length ?? 0)) + "******");
            
            try
            {
                // Validate phone number format
                var (isValid, errorMessage) = ValidationHelper.ValidatePhoneNumber(phoneNumber);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid phone number format: {ErrorMessage}", errorMessage);
                    return false;
                }

                // Store phone number for the verification step
                _currentPhoneNumber = phoneNumber;

                // Create the request payload
                var request = new AuthenticationRequest
                {
                    PhoneNumber = phoneNumber
                };

                // Send request to the API
                await _apiService.PostAsync<object>(ApiEndpoints.AuthVerify, request, false);
                
                _logger.LogInformation("Verification code requested successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting verification code: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Verifies the code sent to the user's phone number and completes the authentication process.
        /// </summary>
        /// <param name="code">The verification code received by the user</param>
        /// <returns>True if the verification was successful, otherwise false</returns>
        public async Task<bool> VerifyCode(string code)
        {
            _logger.LogInformation("Verifying code for authentication");
            
            try
            {
                // Validate verification code format
                var (isValid, errorMessage) = ValidationHelper.ValidateVerificationCode(code);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid verification code: {ErrorMessage}", errorMessage);
                    return false;
                }

                // Verify we have a phone number from previous step
                if (string.IsNullOrEmpty(_currentPhoneNumber))
                {
                    _logger.LogError("No phone number found for verification. Request verification code first.");
                    return false;
                }

                // Create the request payload
                var request = new VerificationRequest
                {
                    PhoneNumber = _currentPhoneNumber,
                    VerificationCode = code
                };

                // Send request to the API
                var response = await _apiService.PostAsync<AuthenticationResponse>(
                    ApiEndpoints.AuthValidate, request, false);

                // Store the token from the response
                await _tokenManager.StoreToken(response.Token);

                // Update authentication state
                var authState = AuthState.CreateAuthenticated(_currentPhoneNumber);
                _authStateProvider.UpdateState(authState);

                _logger.LogInformation("User authenticated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying code: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the current authentication state of the user.
        /// </summary>
        /// <returns>The current authentication state</returns>
        public async Task<AuthState> GetAuthenticationState()
        {
            return await _authStateProvider.GetCurrentState();
        }

        /// <summary>
        /// Logs out the current user by clearing authentication tokens and state.
        /// </summary>
        public async Task Logout()
        {
            _logger.LogInformation("User logout requested");
            
            try
            {
                // Clear authentication token
                await _tokenManager.ClearToken();

                // Update authentication state
                var authState = AuthState.CreateUnauthenticated();
                _authStateProvider.UpdateState(authState);

                // Clear current phone number
                _currentPhoneNumber = string.Empty;

                _logger.LogInformation("User logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Attempts to refresh the authentication token when it's approaching expiration.
        /// </summary>
        /// <returns>True if the token was successfully refreshed, otherwise false</returns>
        public async Task<bool> RefreshToken()
        {
            _logger.LogInformation("Attempting to refresh authentication token");
            
            try
            {
                // Check if token exists and is valid
                if (!await _tokenManager.IsTokenValid())
                {
                    _logger.LogWarning("Cannot refresh token: No valid token exists");
                    return false;
                }

                // Check if token is expiring soon
                if (!await _tokenManager.IsTokenExpiringSoon())
                {
                    _logger.LogInformation("Token refresh not needed yet");
                    return true;
                }

                // Get current token
                var currentToken = await _tokenManager.RetrieveToken();

                // Create refresh request
                var refreshRequest = new { Token = currentToken };

                // Send request to the API
                var response = await _apiService.PostAsync<AuthenticationResponse>(
                    ApiEndpoints.AuthRefresh, refreshRequest, false);

                // Store the new token
                await _tokenManager.StoreToken(response.Token);

                _logger.LogInformation("Token refreshed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token: {Message}", ex.Message);
                return false;
            }
        }
    }
}