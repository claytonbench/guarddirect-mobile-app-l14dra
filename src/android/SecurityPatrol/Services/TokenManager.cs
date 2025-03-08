using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Helpers;
using SecurityPatrol.Constants;
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of ITokenManager that handles secure storage, retrieval, validation, 
    /// and lifecycle management of authentication tokens.
    /// </summary>
    public class TokenManager : ITokenManager
    {
        private readonly ILogger<TokenManager> _logger;
        private readonly string TOKEN_KEY = "auth_token";
        private readonly string TOKEN_EXPIRY_KEY = "auth_token_expiry";

        /// <summary>
        /// Initializes a new instance of the TokenManager class with required dependencies
        /// </summary>
        /// <param name="logger">Logger for logging operations</param>
        public TokenManager(ILogger<TokenManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Securely stores the authentication token in the device's secure storage
        /// </summary>
        /// <param name="token">The authentication token to store</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StoreToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token cannot be null or empty", nameof(token));
            }

            try
            {
                _logger.LogInformation("Storing authentication token");
                
                // Try to parse the token to extract expiry information if it's a JWT
                DateTime expiryTime = DateTime.UtcNow.AddMinutes(AppConstants.AuthTokenExpiryMinutes);
                try
                {
                    // Simple JWT parsing - split by dots and check second part (payload)
                    string[] tokenParts = token.Split('.');
                    if (tokenParts.Length >= 2)
                    {
                        // Add padding if needed for base64 decoding
                        string payload = tokenParts[1];
                        int padding = payload.Length % 4;
                        if (padding > 0)
                            payload += new string('=', 4 - padding);

                        // Decode payload
                        byte[] payloadBytes = Convert.FromBase64String(payload);
                        string payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);

                        // Parse as JSON and extract expiry
                        using (JsonDocument doc = JsonDocument.Parse(payloadJson))
                        {
                            if (doc.RootElement.TryGetProperty("exp", out JsonElement expElement) && 
                                expElement.ValueKind == JsonValueKind.Number)
                            {
                                // exp in JWT is Unix timestamp (seconds since epoch)
                                long expUnixTime = expElement.GetInt64();
                                expiryTime = DateTimeOffset.FromUnixTimeSeconds(expUnixTime).UtcDateTime;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // If token parsing fails, fall back to default expiry time
                    _logger.LogWarning(ex, "Failed to parse token expiry time, using default expiry");
                }
                
                // Store the token
                await SecurityHelper.SaveToSecureStorage(TOKEN_KEY, token);
                
                // Store the expiry time as ISO 8601 string
                await SecurityHelper.SaveToSecureStorage(TOKEN_EXPIRY_KEY, expiryTime.ToString("o"));
                
                _logger.LogInformation("Authentication token stored successfully, expires at: {ExpiryTime}", expiryTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing authentication token");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the stored authentication token from secure storage
        /// </summary>
        /// <returns>The authentication token if available, otherwise null</returns>
        public async Task<string> RetrieveToken()
        {
            try
            {
                _logger.LogInformation("Retrieving authentication token");
                
                string token = await SecurityHelper.GetFromSecureStorage(TOKEN_KEY);
                
                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("Authentication token retrieved successfully");
                }
                else
                {
                    _logger.LogInformation("No authentication token found in secure storage");
                }
                
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authentication token");
                return null;
            }
        }

        /// <summary>
        /// Removes the stored authentication token from secure storage
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ClearToken()
        {
            try
            {
                _logger.LogInformation("Clearing authentication token");
                
                // Remove both token and expiry time
                await SecurityHelper.RemoveFromSecureStorage(TOKEN_KEY);
                await SecurityHelper.RemoveFromSecureStorage(TOKEN_EXPIRY_KEY);
                
                _logger.LogInformation("Authentication token cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing authentication token");
                throw;
            }
        }

        /// <summary>
        /// Checks if the stored token exists and is not expired
        /// </summary>
        /// <returns>True if a valid token exists, otherwise false</returns>
        public async Task<bool> IsTokenValid()
        {
            try
            {
                _logger.LogInformation("Checking if authentication token is valid");
                
                // First, check if the token exists
                string token = await RetrieveToken();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("No authentication token found");
                    return false;
                }
                
                // Then, check if it's expired
                DateTime? expiryTime = await GetTokenExpiryTime();
                if (!expiryTime.HasValue)
                {
                    _logger.LogWarning("Token exists but no expiry time found");
                    return false;
                }
                
                bool isValid = DateTime.UtcNow < expiryTime.Value;
                
                if (isValid)
                {
                    _logger.LogInformation("Authentication token is valid");
                }
                else
                {
                    _logger.LogInformation("Authentication token is expired");
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating authentication token");
                return false;
            }
        }

        /// <summary>
        /// Retrieves the expiration time of the stored token
        /// </summary>
        /// <returns>The token expiration time if available, otherwise null</returns>
        public async Task<DateTime?> GetTokenExpiryTime()
        {
            try
            {
                _logger.LogInformation("Retrieving token expiry time");
                
                string expiryTimeStr = await SecurityHelper.GetFromSecureStorage(TOKEN_EXPIRY_KEY);
                
                if (string.IsNullOrEmpty(expiryTimeStr))
                {
                    _logger.LogInformation("No token expiry time found");
                    return null;
                }
                
                DateTime expiryTime = DateTime.Parse(expiryTimeStr);
                
                _logger.LogInformation("Token expiry time retrieved: {ExpiryTime}", expiryTime);
                
                return expiryTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving token expiry time");
                return null;
            }
        }

        /// <summary>
        /// Checks if the token is approaching its expiration time and should be refreshed
        /// </summary>
        /// <returns>True if the token is expiring soon, otherwise false</returns>
        public async Task<bool> IsTokenExpiringSoon()
        {
            try
            {
                _logger.LogInformation("Checking if token is expiring soon");
                
                DateTime? expiryTime = await GetTokenExpiryTime();
                
                if (!expiryTime.HasValue)
                {
                    _logger.LogInformation("No token expiry time found");
                    return false;
                }
                
                // Define "soon" as within 30 minutes of expiration
                DateTime thresholdTime = DateTime.UtcNow.AddMinutes(30);
                
                bool isExpiringSoon = expiryTime.Value <= thresholdTime;
                
                if (isExpiringSoon)
                {
                    _logger.LogInformation("Token is expiring soon and should be refreshed");
                }
                else
                {
                    _logger.LogInformation("Token is not expiring soon");
                }
                
                return isExpiringSoon;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if token is expiring soon");
                return false;
            }
        }
    }
}