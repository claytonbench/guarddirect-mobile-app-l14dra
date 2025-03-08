using System;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Moq;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Fixtures;
using SecurityPatrol.SecurityTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Helpers;
using SecurityPatrol.Models;

namespace SecurityPatrol.SecurityTests.Mobile
{
    /// <summary>
    /// Security-focused test class that verifies the security aspects of authentication token handling in the mobile application.
    /// </summary>
    public class AuthTokenSecurityTests : SecurityTestBase
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly ApiServerFixture _apiServer;
        private readonly TokenManager _tokenManager;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        /// <summary>
        /// Initializes a new instance of the AuthTokenSecurityTests class with required dependencies
        /// </summary>
        /// <param name="outputHelper">Test output helper for logging</param>
        /// <param name="apiServer">API server fixture for mock API responses</param>
        public AuthTokenSecurityTests(ITestOutputHelper outputHelper, ApiServerFixture apiServer)
            : base(outputHelper, apiServer)
        {
            _outputHelper = outputHelper;
            _apiServer = apiServer;
            _tokenManager = new TokenManager(Logger);
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        /// <summary>
        /// Verifies that authentication tokens are stored securely using platform secure storage
        /// </summary>
        [Fact]
        public async Task TestTokenSecureStorage()
        {
            // Arrange
            string testToken = TestConstants.TestAuthToken;
            
            // Act
            await _tokenManager.StoreToken(testToken);
            
            // Try to retrieve the token directly from secure storage to verify it's not stored in plain text
            string rawStoredValue = await SecurityHelper.GetFromSecureStorage("auth_token");
            
            // Retrieve the token using the token manager
            string retrievedToken = await _tokenManager.RetrieveToken();
            
            // Assert
            Assert.NotNull(rawStoredValue);
            Assert.NotEqual(testToken, rawStoredValue); // Should not be stored as plain text
            Assert.Equal(testToken, retrievedToken); // But should be retrievable via the token manager
            
            // Verify that the token storage is secure
            bool isSecurelyStored = VerifyTokenStorageSecurity(testToken, rawStoredValue);
            Assert.True(isSecurelyStored, "Token should be securely stored");
        }

        /// <summary>
        /// Verifies that authentication tokens are properly encrypted before storage
        /// </summary>
        [Fact]
        public async Task TestTokenEncryption()
        {
            // Arrange
            string testToken = TestConstants.TestAuthToken;
            
            // Act
            await _tokenManager.StoreToken(testToken);
            
            // Retrieve the raw stored value
            string rawStoredValue = await SecurityHelper.GetFromSecureStorage("auth_token");
            
            // Assert
            Assert.NotNull(rawStoredValue);
            Assert.NotEqual(testToken, rawStoredValue); // Different value indicates encryption
            
            // Verify encryption strength using the base class method
            bool isProperlyEncrypted = ValidateEncryption(testToken, System.Text.Encoding.UTF8.GetBytes(rawStoredValue));
            
            // Assert that proper encryption is used (AES-256)
            Assert.True(isProperlyEncrypted, "Token should be properly encrypted");
        }

        /// <summary>
        /// Verifies that token validation properly checks for token integrity and expiration
        /// </summary>
        [Fact]
        public async Task TestTokenValidation()
        {
            // Arrange - Create a valid token
            string validToken = CreateTestToken(DateTime.UtcNow.AddHours(1)); // Valid for 1 hour
            
            // Act - Store the token
            await _tokenManager.StoreToken(validToken);
            
            // Assert - Token should be valid
            bool isValid = await _tokenManager.IsTokenValid();
            Assert.True(isValid, "Valid token should be recognized as valid");
            
            // Arrange - Create an expired token
            string expiredToken = CreateTestToken(DateTime.UtcNow.AddHours(-1)); // Expired 1 hour ago
            
            // Act - Store the expired token
            await _tokenManager.StoreToken(expiredToken);
            
            // Assert - Token should be invalid
            isValid = await _tokenManager.IsTokenValid();
            Assert.False(isValid, "Expired token should be recognized as invalid");
            
            // Arrange - Create a malformed token
            string malformedToken = "not.a.valid.jwt.token";
            
            // Act - Store the malformed token
            await _tokenManager.StoreToken(malformedToken);
            
            // Assert - Token should be invalid
            isValid = await _tokenManager.IsTokenValid();
            Assert.False(isValid, "Malformed token should be recognized as invalid");
        }

        /// <summary>
        /// Verifies that the JWT token structure follows security best practices
        /// </summary>
        [Fact]
        public async Task TestTokenStructureSecurity()
        {
            // Arrange
            string testToken = TestConstants.TestAuthToken;
            
            // Act
            var jwtToken = _tokenHandler.ReadJwtToken(testToken);
            
            // Assert
            // Verify algorithm
            Assert.Contains(jwtToken.Header.Alg, new[] { "HS256", "RS256" }, "Token should use a secure algorithm");
            
            // Verify essential claims
            Assert.Contains(jwtToken.Claims, c => c.Type == "sub");
            Assert.Contains(jwtToken.Claims, c => c.Type == "iat");
            Assert.Contains(jwtToken.Claims, c => c.Type == "exp");
            
            // Verify the token doesn't contain sensitive information
            Assert.DoesNotContain(jwtToken.Claims, c => c.Type == "password");
            Assert.DoesNotContain(jwtToken.Claims, c => c.Type == "secret");
            
            // Verify the token structure is secure using the base class method
            bool isSecure = ValidateTokenSecurity(testToken);
            Assert.True(isSecure, "Token structure should follow security best practices");
        }

        /// <summary>
        /// Verifies that token expiration is properly handled
        /// </summary>
        [Fact]
        public async Task TestTokenExpiryHandling()
        {
            // Arrange - Create a token that expires in 5 seconds
            string tokenWithShortExpiry = CreateTestToken(DateTime.UtcNow.AddSeconds(5));
            
            // Act - Store the token
            await _tokenManager.StoreToken(tokenWithShortExpiry);
            
            // Assert - Token should initially be valid
            bool isValid = await _tokenManager.IsTokenValid();
            Assert.True(isValid, "Token should be valid initially");
            
            // Act - Wait for the token to expire
            await Task.Delay(6000); // Wait 6 seconds to ensure expiry
            
            // Assert - Token should now be expired
            isValid = await _tokenManager.IsTokenValid();
            Assert.False(isValid, "Token should be invalid after expiration");
            
            // Arrange - Create a token that expires in 15 minutes
            string tokenExpiringIn15Min = CreateTestToken(DateTime.UtcNow.AddMinutes(15));
            
            // Act - Store the token
            await _tokenManager.StoreToken(tokenExpiringIn15Min);
            
            // Assert - Token should be expiring soon (within 30 minutes)
            bool isExpiringSoon = await _tokenManager.IsTokenExpiringSoon();
            Assert.True(isExpiringSoon, "Token expiring in 15 minutes should be detected as expiring soon");
        }

        /// <summary>
        /// Verifies that tokens are properly cleared from secure storage on logout
        /// </summary>
        [Fact]
        public async Task TestTokenClearingOnLogout()
        {
            // Arrange - Store a token
            string testToken = TestConstants.TestAuthToken;
            await _tokenManager.StoreToken(testToken);
            
            // Verify token is stored and retrievable
            string retrievedToken = await _tokenManager.RetrieveToken();
            Assert.Equal(testToken, retrievedToken);
            
            // Act - Clear the token (simulate logout)
            await _tokenManager.ClearToken();
            
            // Assert - Token should no longer be retrievable
            retrievedToken = await _tokenManager.RetrieveToken();
            Assert.Null(retrievedToken);
            
            // Verify no token data remains in secure storage
            string rawStoredValue = await SecurityHelper.GetFromSecureStorage("auth_token");
            Assert.Null(rawStoredValue);
            
            // Verify token validation fails
            bool isValid = await _tokenManager.IsTokenValid();
            Assert.False(isValid, "Token should not be valid after clearing");
        }

        /// <summary>
        /// Verifies that token refresh operations maintain security
        /// </summary>
        [Fact]
        public async Task TestTokenRefreshSecurity()
        {
            // Arrange - Set up mock API response for token refresh
            var refreshResponse = new AuthenticationResponse
            {
                Token = "new.refreshed.token",
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };
            
            _apiServer.SetupSuccessResponse("/auth/refresh", refreshResponse);
            
            // Store an initial token
            string initialToken = TestConstants.TestAuthToken;
            await _tokenManager.StoreToken(initialToken);
            
            // Act - Simulate token refresh (manually store the new token from the response)
            await _tokenManager.StoreToken(refreshResponse.Token);
            
            // Assert
            // Verify that the new token is stored
            string storedToken = await _tokenManager.RetrieveToken();
            Assert.Equal(refreshResponse.Token, storedToken);
            
            // Verify that the token is valid
            bool isValid = await _tokenManager.IsTokenValid();
            Assert.True(isValid, "Refreshed token should be valid");
            
            // Verify that the refresh maintains security properties
            bool isSecure = ValidateTokenSecurity(storedToken);
            Assert.True(isSecure, "Refreshed token should maintain security properties");
        }

        /// <summary>
        /// Verifies that token storage is isolated and not accessible to other applications
        /// </summary>
        [Fact]
        public async Task TestTokenStorageIsolation()
        {
            // Arrange - Store a token
            string testToken = TestConstants.TestAuthToken;
            await _tokenManager.StoreToken(testToken);
            
            // Act/Assert - Attempt to access the token through non-standard means
            // This is more of a theoretical test as we can't actually attempt to access
            // the secure storage from another app in a unit test
            
            // Verify that the token is stored using platform-specific secure storage
            string rawStoredValue = await SecurityHelper.GetFromSecureStorage("auth_token");
            Assert.NotNull(rawStoredValue);
            Assert.NotEqual(testToken, rawStoredValue); // Not stored as plain text
            
            // Log an information message about this limitation
            LogSecurityIssue("TokenStorageIsolation", 
                "Full isolation testing requires device-level testing. " +
                "Verified that token is stored using platform secure storage.", 
                LogLevel.Information);
            
            // Indirectly verify isolation by ensuring proper secure storage mechanisms are used
            bool isProperlyStored = await SecurityHelper.IsSecureStorageAvailable();
            Assert.True(isProperlyStored, "Secure storage should be available for token isolation");
        }

        /// <summary>
        /// Helper method to create a test JWT token with specified expiry
        /// </summary>
        /// <param name="expiryTime">The expiration time for the token</param>
        /// <returns>A JWT token string</returns>
        private string CreateTestToken(DateTime expiryTime)
        {
            // Create a simple JWT token with the necessary claims
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = new JwtSecurityToken(
                issuer: "SecurityPatrol.Tests",
                audience: "SecurityPatrol.App",
                claims: new[]
                {
                    new System.Security.Claims.Claim("sub", TestConstants.TestUserId),
                    new System.Security.Claims.Claim("name", "Test User"),
                    new System.Security.Claims.Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), System.Security.Claims.ClaimValueTypes.Integer64)
                },
                expires: expiryTime,
                signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("testsecuritykeywith32characters!!")),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)
            );

            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Helper method to verify the security of token storage
        /// </summary>
        /// <param name="originalToken">The original token</param>
        /// <param name="storedValue">The stored value from secure storage</param>
        /// <returns>True if storage is secure, false otherwise</returns>
        private bool VerifyTokenStorageSecurity(string originalToken, string storedValue)
        {
            // Basic verification that the token is not stored in plain text
            if (storedValue == originalToken)
            {
                LogSecurityIssue("PlainTextStorage", "Token is stored in plain text", LogLevel.Critical);
                return false;
            }

            // Verify that the token is not stored using weak encoding
            string base64Token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalToken));
            if (storedValue == base64Token)
            {
                LogSecurityIssue("WeakEncoding", "Token is stored using Base64 encoding only", LogLevel.Critical);
                return false;
            }

            // Verify that the storage mechanism is secure
            // This is a theoretical check as we can't fully test platform security in a unit test
            
            // For a more comprehensive check, we would need to:
            // 1. Verify encryption algorithm strength
            // 2. Verify key management
            // 3. Verify storage isolation
            
            // For now, we'll assume secure storage is properly implemented if the value differs from the original
            return true;
        }
    }
}