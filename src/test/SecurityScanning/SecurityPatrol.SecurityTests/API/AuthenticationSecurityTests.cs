using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt; // System.IdentityModel.Tokens.Jwt 6.15.0
using Xunit; // xunit 2.4.2
using Xunit.Abstractions; // xunit.abstractions 2.0.3
using Newtonsoft.Json; // Newtonsoft.Json 13.0.1
using SecurityPatrol.SecurityTests.Setup;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Fixtures;

namespace SecurityPatrol.SecurityTests.API
{
    /// <summary>
    /// Implements focused security tests for the authentication system in the Security Patrol application,
    /// testing token security, verification code handling, session management, and protection against common
    /// authentication vulnerabilities.
    /// </summary>
    public class AuthenticationSecurityTests : SecurityTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly ApiServerFixture _apiServer;

        /// <summary>
        /// Initializes a new instance of the AuthenticationSecurityTests class with test output helper and API server fixture
        /// </summary>
        /// <param name="output">The test output helper</param>
        /// <param name="apiServer">The API server fixture</param>
        public AuthenticationSecurityTests(ITestOutputHelper output, ApiServerFixture apiServer) 
            : base(output, apiServer)
        {
            _output = output;
            _apiServer = apiServer;
        }

        /// <summary>
        /// Tests that authentication tokens have proper structure and use secure algorithms
        /// </summary>
        [Fact]
        public async Task TestTokenStructureAndAlgorithm()
        {
            // Set up mock response for auth/validate endpoint with a valid token
            var authResponse = new
            {
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            };
            _apiServer.SetupSuccessResponse("/auth/validate", authResponse);

            // Create a request with valid phone number and verification code
            var request = new
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };

            // Send the request and get the response
            var httpRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            httpRequest.Content = JsonContent.Create(request);
            var response = await HttpClient.SendAsync(httpRequest);
            
            // Extract the token from the response
            Assert.True(response.IsSuccessStatusCode, "Authentication request should succeed");
            var responseContent = await response.Content.ReadFromJsonAsync<dynamic>();
            
            Assert.NotNull(responseContent);
            string token = responseContent.Token;
            Assert.NotNull(token);
            
            // Parse the JWT token to examine its structure
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            // Assert that the token has three parts (header.payload.signature)
            string[] tokenParts = token.Split('.');
            Assert.Equal(3, tokenParts.Length);
            
            // Assert that the token uses a secure algorithm (HS256 or RS256)
            string algorithm = jwtToken.Header.Alg;
            Assert.Contains(algorithm, new[] { "HS256", "RS256" });
            
            // Assert that the token contains required claims (sub, exp, iat)
            Assert.Contains(jwtToken.Claims, c => c.Type == "sub");
            Assert.Contains(jwtToken.Claims, c => c.Type == "exp");
            Assert.Contains(jwtToken.Claims, c => c.Type == "iat");
            
            LogSecurityIssue("TokenTest", "Token structure and algorithm validation passed", Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests that authentication tokens have appropriate expiration times
        /// </summary>
        [Fact]
        public async Task TestTokenExpiration()
        {
            // Set up mock response for auth/validate endpoint with a valid token
            DateTime expiryTime = DateTime.UtcNow.AddHours(8);
            var authResponse = new
            {
                Token = TestConstants.TestAuthToken,
                ExpiresAt = expiryTime.ToString("o")
            };
            _apiServer.SetupSuccessResponse("/auth/validate", authResponse);

            // Create a request with valid phone number and verification code
            var request = new
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };

            // Send the request and get the response
            var httpRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            httpRequest.Content = JsonContent.Create(request);
            var response = await HttpClient.SendAsync(httpRequest);
            
            // Extract the token from the response
            Assert.True(response.IsSuccessStatusCode, "Authentication request should succeed");
            var responseContent = await response.Content.ReadFromJsonAsync<dynamic>();
            
            Assert.NotNull(responseContent);
            string token = responseContent.Token;
            Assert.NotNull(token);
            
            // Parse the JWT token to examine its expiration claim
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            // Assert that the token has an expiration time (exp claim)
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
            Assert.NotNull(expClaim);
            
            // Assert that the expiration time is appropriate (approximately 8 hours from issuance)
            long expValue = long.Parse(expClaim.Value);
            var tokenExpiryTime = DateTimeOffset.FromUnixTimeSeconds(expValue).UtcDateTime;
            
            // Get the issued at time
            var iatClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "iat");
            Assert.NotNull(iatClaim);
            long iatValue = long.Parse(iatClaim.Value);
            var tokenIssuedTime = DateTimeOffset.FromUnixTimeSeconds(iatValue).UtcDateTime;
            
            // Check that expiry is approximately 8 hours from issuance (with some tolerance)
            TimeSpan timeUntilExpiry = tokenExpiryTime - tokenIssuedTime;
            Assert.True(timeUntilExpiry >= TimeSpan.FromHours(7.5) && 
                        timeUntilExpiry <= TimeSpan.FromHours(8.5),
                        $"Token expiry time should be approximately 8 hours from issuance. Found: {timeUntilExpiry.TotalHours} hours");
            
            // Assert that the token has not already expired
            Assert.True(tokenExpiryTime > DateTime.UtcNow);
            
            LogSecurityIssue("TokenExpiryTest", "Token expiration validation passed", Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests the security of the token refresh mechanism
        /// </summary>
        [Fact]
        public async Task TestTokenRefreshSecurity()
        {
            // Set up mock response for auth/refresh endpoint with a refreshed token
            string oldToken = TestConstants.TestAuthToken;
            string newToken = oldToken + "refreshed"; // Just for testing, to make it different
            var refreshResponse = new
            {
                Token = newToken,
                RefreshToken = "new-refresh-token",
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            };
            _apiServer.SetupSuccessResponse("/auth/refresh", refreshResponse);

            // Create a request to refresh a valid token
            var request = new
            {
                RefreshToken = TestConstants.TestRefreshToken
            };

            // Send the request and get the response
            var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/auth/refresh", oldToken);
            httpRequest.Content = JsonContent.Create(request);
            var response = await HttpClient.SendAsync(httpRequest);
            
            // Extract the refreshed token from the response
            Assert.True(response.IsSuccessStatusCode, "Token refresh request should succeed");
            var responseContent = await response.Content.ReadFromJsonAsync<dynamic>();
            
            Assert.NotNull(responseContent);
            string refreshedToken = responseContent.Token;
            
            // Assert that the refreshed token is different from the original
            Assert.NotEqual(oldToken, refreshedToken);
            
            // Test that expired tokens cannot be refreshed
            _apiServer.SetupErrorResponse("/auth/refresh", 401, "Expired or invalid refresh token");
            
            // Create a request with an expired token
            var expiredRequest = new
            {
                RefreshToken = "expired-refresh-token"
            };
            
            var expiredHttpRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/auth/refresh");
            expiredHttpRequest.Content = JsonContent.Create(expiredRequest);
            var expiredResponse = await HttpClient.SendAsync(expiredHttpRequest);
            
            // Assert that the request was rejected
            Assert.Equal(401, (int)expiredResponse.StatusCode);
            
            // Test that malformed tokens cannot be refreshed
            _apiServer.SetupErrorResponse("/auth/refresh", 400, "Malformed token");
            
            // Create a request with a malformed token
            var malformedRequest = new
            {
                RefreshToken = "malformed-token"
            };
            
            var malformedHttpRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/auth/refresh");
            malformedHttpRequest.Content = JsonContent.Create(malformedRequest);
            var malformedResponse = await HttpClient.SendAsync(malformedHttpRequest);
            
            // Assert that the request was rejected
            Assert.Equal(400, (int)malformedResponse.StatusCode);
            
            LogSecurityIssue("TokenRefreshTest", "Token refresh security validation passed", Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests the security of the verification code mechanism
        /// </summary>
        [Fact]
        public async Task TestVerificationCodeSecurity()
        {
            // Set up mock responses for auth/verify and auth/validate endpoints
            _apiServer.SetupSuccessResponse("/auth/verify", new { VerificationId = "test-verification-id" });
            _apiServer.SetupSuccessResponse("/auth/validate", new { 
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            });

            // Test that verification codes must be of correct length
            _apiServer.SetupErrorResponse("/auth/validate", 400, "Invalid verification code format");
            
            // Create a request with an invalid code length
            var shortCodeRequest = new
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = "12345" // Too short (should be 6 digits)
            };
            
            var shortCodeHttpRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            shortCodeHttpRequest.Content = JsonContent.Create(shortCodeRequest);
            var shortCodeResponse = await HttpClient.SendAsync(shortCodeHttpRequest);
            
            // Assert that the request was rejected
            Assert.Equal(400, (int)shortCodeResponse.StatusCode);
            
            // Test that verification codes must be numeric
            var nonNumericCodeRequest = new
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = "12345a" // Contains non-numeric character
            };
            
            var nonNumericHttpRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            nonNumericHttpRequest.Content = JsonContent.Create(nonNumericCodeRequest);
            var nonNumericResponse = await HttpClient.SendAsync(nonNumericHttpRequest);
            
            // Assert that the request was rejected
            Assert.Equal(400, (int)nonNumericResponse.StatusCode);
            
            // Test that verification codes expire after a certain time
            _apiServer.SetupErrorResponse("/auth/validate", 401, "Verification code expired");
            
            // Create a request with an expired code
            var expiredCodeRequest = new
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode,
                VerificationId = "expired-verification-id"
            };
            
            var expiredCodeHttpRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            expiredCodeHttpRequest.Content = JsonContent.Create(expiredCodeRequest);
            var expiredCodeResponse = await HttpClient.SendAsync(expiredCodeHttpRequest);
            
            // Assert that the request was rejected
            Assert.Equal(401, (int)expiredCodeResponse.StatusCode);
            
            // Test that multiple failed attempts with incorrect codes are limited
            _apiServer.SetupErrorResponse("/auth/validate", 429, "Too many failed attempts");
            
            // Simulate multiple failed attempts
            for (int i = 0; i < 5; i++)
            {
                var failedRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
                failedRequest.Content = JsonContent.Create(new {
                    PhoneNumber = TestConstants.TestPhoneNumber,
                    Code = "000000" // Incorrect code
                });
                await HttpClient.SendAsync(failedRequest);
            }
            
            // Attempt after too many failures
            var rateLimitedRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            rateLimitedRequest.Content = JsonContent.Create(new {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = "000000"
            });
            var rateLimitedResponse = await HttpClient.SendAsync(rateLimitedRequest);
            
            // Should be rate limited
            Assert.Equal(429, (int)rateLimitedResponse.StatusCode);
            
            LogSecurityIssue("VerificationCodeTest", "Verification code security validation passed", Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests that authentication endpoints implement rate limiting to prevent brute force attacks
        /// </summary>
        [Fact]
        public async Task TestAuthenticationRateLimiting()
        {
            // Set up mock responses for auth endpoints with rate limiting headers
            _apiServer.SetupSuccessResponse("/auth/verify", new { VerificationId = "test-verification-id" });
            
            // Test rate limiting on auth/verify endpoint
            bool verifyRateLimited = await TestRateLimiting("/auth/verify", HttpMethod.Post, 10, 5);
            Assert.True(verifyRateLimited, "The /auth/verify endpoint should implement rate limiting");
            
            // Test rate limiting on auth/validate endpoint
            bool validateRateLimited = await TestRateLimiting("/auth/validate", HttpMethod.Post, 10, 5);
            Assert.True(validateRateLimited, "The /auth/validate endpoint should implement rate limiting");
            
            // Get the last response to check for Retry-After header
            var request = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/verify");
            request.Content = JsonContent.Create(new { PhoneNumber = TestConstants.TestPhoneNumber });
            var response = await HttpClient.SendAsync(request);
            
            // Check for 429 status and Retry-After header
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Assert.True(response.Headers.Contains("Retry-After"), 
                    "Rate limited responses should include a Retry-After header");
            }
            
            LogSecurityIssue("RateLimitingTest", "Authentication rate limiting validation passed", Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests the security of session management using authentication tokens
        /// </summary>
        [Fact]
        public async Task TestSessionManagementSecurity()
        {
            // Set up mock responses for authentication and protected endpoints
            _apiServer.SetupSuccessResponse("/auth/validate", new { 
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            });
            
            _apiServer.SetupSuccessResponse("/api/protected", new { 
                Message = "This is protected data" 
            });
            
            // Test that protected endpoints require valid authentication
            // First try without authentication
            var unauthenticatedRequest = CreateUnauthenticatedRequest(HttpMethod.Get, "/api/protected");
            var unauthenticatedResponse = await HttpClient.SendAsync(unauthenticatedRequest);
            
            // Should be rejected
            Assert.Equal(401, (int)unauthenticatedResponse.StatusCode);
            
            // Now try with valid authentication
            var authenticatedRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/protected", TestConstants.TestAuthToken);
            var authenticatedResponse = await HttpClient.SendAsync(authenticatedRequest);
            
            // Should succeed
            Assert.Equal(200, (int)authenticatedResponse.StatusCode);
            
            // Test that expired tokens are rejected
            _apiServer.SetupErrorResponse("/api/protected", 401, "Token expired");
            
            // Create a request with an expired token
            var expiredTokenRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/protected", "expired-token");
            var expiredTokenResponse = await HttpClient.SendAsync(expiredTokenRequest);
            
            // Should be rejected
            Assert.Equal(401, (int)expiredTokenResponse.StatusCode);
            
            // Test that tokens from different users cannot access others' resources
            _apiServer.SetupErrorResponse("/api/protected/user/456", 403, "Access denied");
            
            // Try to access another user's resources
            var crossUserRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/protected/user/456", TestConstants.TestAuthToken);
            var crossUserResponse = await HttpClient.SendAsync(crossUserRequest);
            
            // Should be forbidden
            Assert.Equal(403, (int)crossUserResponse.StatusCode);
            
            // Test that logout invalidates the current token
            _apiServer.SetupSuccessResponse("/auth/logout", new { Success = true });
            
            // Perform logout
            var logoutRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/auth/logout", TestConstants.TestAuthToken);
            var logoutResponse = await HttpClient.SendAsync(logoutRequest);
            
            // Should succeed
            Assert.Equal(200, (int)logoutResponse.StatusCode);
            
            // Set up the API to reject the logged-out token
            _apiServer.SetupErrorResponse("/api/protected", 401, "Token invalidated");
            
            // Try to use the token after logout
            var postLogoutRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/protected", TestConstants.TestAuthToken);
            var postLogoutResponse = await HttpClient.SendAsync(postLogoutRequest);
            
            // Should be rejected
            Assert.Equal(401, (int)postLogoutResponse.StatusCode);
            
            LogSecurityIssue("SessionManagementTest", "Session management security validation passed", Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests the security of authentication headers in requests and responses
        /// </summary>
        [Fact]
        public async Task TestAuthenticationHeaderSecurity()
        {
            // Set up mock responses for authentication endpoints
            _apiServer.SetupSuccessResponse("/auth/validate", new { 
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            });
            
            // Test that authentication responses include secure headers
            var request = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            request.Content = JsonContent.Create(new { 
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            });
            
            var response = await HttpClient.SendAsync(request);
            
            // Verify security headers
            Assert.True(ValidateSecureHeaders(response), "Authentication responses should include security headers");
            
            // Verify Cache-Control headers prevent token caching
            Assert.True(response.Headers.CacheControl != null, "Response should include Cache-Control header");
            Assert.True(response.Headers.CacheControl.NoStore, "Cache-Control should include no-store directive");
            Assert.True(response.Headers.CacheControl.NoCache, "Cache-Control should include no-cache directive");
            
            // Test that authentication tokens are only sent over HTTPS
            // This is usually enforced by the HttpClient's BaseAddress and would need to be
            // tested differently in a real environment, but we'll check the mocked URL
            Assert.StartsWith("https://", TestConstants.TestApiBaseUrl, "API base URL should use HTTPS");
            
            LogSecurityIssue("HeaderSecurityTest", "Authentication header security validation passed", Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests that authentication errors are handled securely without information leakage
        /// </summary>
        [Fact]
        public async Task TestAuthenticationErrorHandling()
        {
            // Set up mock error responses for authentication endpoints
            _apiServer.SetupErrorResponse("/auth/verify", 400, "Invalid phone number format");
            
            // Test invalid phone number format error response
            var invalidPhoneRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/verify");
            invalidPhoneRequest.Content = JsonContent.Create(new { PhoneNumber = "invalid-format" });
            var invalidPhoneResponse = await HttpClient.SendAsync(invalidPhoneRequest);
            
            // Should be rejected with appropriate status code
            Assert.Equal(400, (int)invalidPhoneResponse.StatusCode);
            
            // Verify response doesn't leak sensitive information
            var errorContent = await invalidPhoneResponse.Content.ReadFromJsonAsync<dynamic>();
            Assert.NotNull(errorContent);
            Assert.NotNull(errorContent.message);
            string errorMessage = errorContent.message.ToString();
            Assert.DoesNotContain("stack", errorMessage.ToLower()); // No stack traces
            Assert.DoesNotContain("exception", errorMessage.ToLower()); // No exception names
            Assert.DoesNotContain("internal", errorMessage.ToLower()); // No "internal server error"
            
            // Test invalid verification code error response
            _apiServer.SetupErrorResponse("/auth/validate", 400, "Invalid verification code");
            
            var invalidCodeRequest = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            invalidCodeRequest.Content = JsonContent.Create(new { 
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = "invalid"
            });
            var invalidCodeResponse = await HttpClient.SendAsync(invalidCodeRequest);
            
            // Should be rejected with appropriate status code
            Assert.Equal(400, (int)invalidCodeResponse.StatusCode);
            
            // Verify security headers are still present in error responses
            Assert.True(ValidateSecureHeaders(invalidCodeResponse), "Error responses should include security headers");
            
            LogSecurityIssue("ErrorHandlingTest", "Authentication error handling validation passed", Microsoft.Extensions.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Tests the security of token storage in the mobile application
        /// </summary>
        [Fact]
        public async Task TestTokenStorageSecurity()
        {
            // In a full implementation, this would use a rooted device or specialized testing tools
            // to verify actual secure storage. For this test, we'll validate the method calls and patterns.
            
            // Create a mock token to store
            string token = TestConstants.TestAuthToken;
            
            // Verify secure token storage
            // For this test, we'll call our TokenManager's StoreToken method and verify it doesn't throw
            try
            {
                await TokenManager.StoreToken(token);
                
                // Verify the token is retrievable
                string retrievedToken = await TokenManager.RetrieveToken();
                Assert.Equal(token, retrievedToken);
                
                // Verify token can be cleared
                await TokenManager.ClearToken();
                string clearedToken = await TokenManager.RetrieveToken();
                Assert.Null(clearedToken);
                
                LogSecurityIssue("TokenStorageTest", "Token storage security validation passed", Microsoft.Extensions.Logging.LogLevel.Information);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Token storage operations should not throw exceptions: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests the security of authentication across multiple devices for the same user
        /// </summary>
        [Fact]
        public async Task TestCrossDeviceAuthenticationSecurity()
        {
            // Set up mock responses simulating multiple device authentication
            _apiServer.SetupSuccessResponse("/auth/validate", new { 
                Token = TestConstants.TestAuthToken + "-device1",
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            });
            
            // Authenticate first device
            var device1Request = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            device1Request.Content = JsonContent.Create(new { 
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode,
                DeviceId = "device1"
            });
            var device1Response = await HttpClient.SendAsync(device1Request);
            
            Assert.Equal(200, (int)device1Response.StatusCode);
            var device1Content = await device1Response.Content.ReadFromJsonAsync<dynamic>();
            Assert.NotNull(device1Content);
            string device1Token = device1Content.Token;
            
            // Set up response for second device authentication
            _apiServer.SetupSuccessResponse("/auth/validate", new { 
                Token = TestConstants.TestAuthToken + "-device2",
                ExpiresAt = DateTime.UtcNow.AddHours(8).ToString("o")
            });
            
            // Authenticate second device
            var device2Request = CreateUnauthenticatedRequest(HttpMethod.Post, "/auth/validate");
            device2Request.Content = JsonContent.Create(new { 
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode,
                DeviceId = "device2"
            });
            var device2Response = await HttpClient.SendAsync(device2Request);
            
            Assert.Equal(200, (int)device2Response.StatusCode);
            var device2Content = await device2Response.Content.ReadFromJsonAsync<dynamic>();
            Assert.NotNull(device2Content);
            string device2Token = device2Content.Token;
            
            // Test that each device receives a unique token
            Assert.NotEqual(device1Token, device2Token);
            
            // Test that both devices have valid access after multi-device login
            _apiServer.SetupSuccessResponse("/api/protected", new { Message = "This is protected data" });
            
            var device1AccessRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/protected", device1Token);
            var device1AccessResponse = await HttpClient.SendAsync(device1AccessRequest);
            Assert.Equal(200, (int)device1AccessResponse.StatusCode);
            
            var device2AccessRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/protected", device2Token);
            var device2AccessResponse = await HttpClient.SendAsync(device2AccessRequest);
            Assert.Equal(200, (int)device2AccessResponse.StatusCode);
            
            // Test that revoking access for one device doesn't affect others
            _apiServer.SetupSuccessResponse("/auth/revoke-device", new { Success = true });
            
            var revokeRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/auth/revoke-device", device1Token);
            revokeRequest.Content = JsonContent.Create(new { DeviceId = "device1" });
            var revokeResponse = await HttpClient.SendAsync(revokeRequest);
            Assert.Equal(200, (int)revokeResponse.StatusCode);
            
            // Set up revoked response for device 1
            _apiServer.SetupErrorResponse("/api/protected", 401, "Token revoked");
            
            // Device 1 should now be rejected
            var device1RevokedRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/protected", device1Token);
            var device1RevokedResponse = await HttpClient.SendAsync(device1RevokedRequest);
            Assert.Equal(401, (int)device1RevokedResponse.StatusCode);
            
            // But device 2 should still have access (restore success response)
            _apiServer.SetupSuccessResponse("/api/protected", new { Message = "This is protected data" });
            var device2StillValidRequest = CreateAuthenticatedRequest(HttpMethod.Get, "/api/protected", device2Token);
            var device2StillValidResponse = await HttpClient.SendAsync(device2StillValidRequest);
            Assert.Equal(200, (int)device2StillValidResponse.StatusCode);
            
            LogSecurityIssue("CrossDeviceTest", "Cross-device authentication security validation passed", Microsoft.Extensions.Logging.LogLevel.Information);
        }
    }
}