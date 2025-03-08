using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SecurityPatrol.Core.Models;
using Xunit;

namespace SecurityPatrol.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the AuthController in the Security Patrol API, verifying the authentication flow.
    /// </summary>
    public class AuthControllerTests : TestBase
    {
        private const string BaseUrl = "api/v1/auth";

        /// <summary>
        /// Initializes a new instance of the AuthControllerTests class with the test factory.
        /// </summary>
        /// <param name="factory">Factory for creating the test server with in-memory database.</param>
        public AuthControllerTests(CustomWebApplicationFactory factory) 
            : base(factory)
        {
        }

        /// <summary>
        /// Tests that the verify endpoint returns a verification ID when provided with a valid phone number.
        /// </summary>
        [Fact]
        public async Task VerifyEndpoint_WithValidPhoneNumber_ReturnsVerificationId()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = TestPhoneNumber };
            
            // Act & Assert - Using PostAsync helper that expects a success status code
            var result = await PostAsync<AuthenticationRequest, Result<string>>($"{BaseUrl}/verify", request);
            
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
        }

        /// <summary>
        /// Tests that the verify endpoint returns a bad request when provided with an invalid phone number.
        /// </summary>
        [Fact]
        public async Task VerifyEndpoint_WithInvalidPhoneNumber_ReturnsBadRequest()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = "invalid" };
            
            // Act - Using HttpClient directly since we expect an error
            var response = await Client.PostAsync($"{BaseUrl}/verify", CreateJsonContent(request));
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that the validate endpoint returns an authentication token when provided with a valid verification code.
        /// </summary>
        [Fact]
        public async Task ValidateEndpoint_WithValidCode_ReturnsAuthToken()
        {
            // Arrange - First get a verification ID
            var authRequest = new AuthenticationRequest { PhoneNumber = TestPhoneNumber };
            await PostAsync<AuthenticationRequest, Result<string>>($"{BaseUrl}/verify", authRequest);
            
            // Now we verify with a valid code (mocked in test environment)
            var verifyRequest = new VerificationRequest 
            { 
                PhoneNumber = TestPhoneNumber,
                Code = "123456" // Valid code in test environment
            };
            
            // Act & Assert - Using PostAsync helper that expects a success status code
            var validateResult = await PostAsync<VerificationRequest, Result<AuthenticationResponse>>($"{BaseUrl}/validate", verifyRequest);
            
            validateResult.Should().NotBeNull();
            validateResult.Succeeded.Should().BeTrue();
            validateResult.Data.Token.Should().NotBeEmpty();
            validateResult.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        /// <summary>
        /// Tests that the validate endpoint returns unauthorized when provided with an invalid verification code.
        /// </summary>
        [Fact]
        public async Task ValidateEndpoint_WithInvalidCode_ReturnsUnauthorized()
        {
            // Arrange - First get a verification ID
            var authRequest = new AuthenticationRequest { PhoneNumber = TestPhoneNumber };
            await PostAsync<AuthenticationRequest, Result<string>>($"{BaseUrl}/verify", authRequest);
            
            // Now we verify with an invalid code
            var verifyRequest = new VerificationRequest 
            { 
                PhoneNumber = TestPhoneNumber,
                Code = "999999" // Invalid code
            };
            
            // Act - Using HttpClient directly since we expect an error
            var validateResponse = await Client.PostAsync($"{BaseUrl}/validate", CreateJsonContent(verifyRequest));
            
            // Assert
            validateResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests that the refresh endpoint returns a new token when provided with a valid token.
        /// </summary>
        [Fact]
        public async Task RefreshEndpoint_WithValidToken_ReturnsNewToken()
        {
            // Arrange - First get a valid token
            var token = await GetValidTokenAsync();
            
            // Set the token in the authorization header
            SetAuthToken(token);
            
            // Act - Using HttpClient directly since we need to handle the null content
            var refreshResponse = await Client.PostAsync($"{BaseUrl}/refresh", null);
            
            // Assert
            refreshResponse.IsSuccessStatusCode.Should().BeTrue();
            
            var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<Result<AuthenticationResponse>>(JsonOptions);
            refreshResult.Should().NotBeNull();
            refreshResult.Succeeded.Should().BeTrue();
            refreshResult.Data.Token.Should().NotBeEmpty();
            refreshResult.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            refreshResult.Data.Token.Should().NotBe(token); // New token should be different
        }

        /// <summary>
        /// Tests that the refresh endpoint returns unauthorized when provided with an invalid token.
        /// </summary>
        [Fact]
        public async Task RefreshEndpoint_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange - Set an invalid token
            SetAuthToken("invalid.token.value");
            
            // Act - Using HttpClient directly since we expect an error
            var refreshResponse = await Client.PostAsync($"{BaseUrl}/refresh", null);
            
            // Assert
            refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests that the refresh endpoint returns unauthorized when no token is provided.
        /// </summary>
        [Fact]
        public async Task RefreshEndpoint_WithNoToken_ReturnsUnauthorized()
        {
            // Arrange - Clear any existing token
            Client.DefaultRequestHeaders.Authorization = null;
            
            // Act - Using HttpClient directly since we expect an error
            var refreshResponse = await Client.PostAsync($"{BaseUrl}/refresh", null);
            
            // Assert
            refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests the complete authentication flow from phone verification to token refresh.
        /// </summary>
        [Fact]
        public async Task CompleteAuthenticationFlow_Success()
        {
            // Arrange
            var authRequest = new AuthenticationRequest { PhoneNumber = TestPhoneNumber };
            
            // Act - Step 1: Request verification code
            var authResult = await PostAsync<AuthenticationRequest, Result<string>>($"{BaseUrl}/verify", authRequest);
            
            // Assert - Step 1
            authResult.Should().NotBeNull();
            authResult.Succeeded.Should().BeTrue();
            authResult.Data.Should().NotBeEmpty();
            
            // Act - Step 2: Validate verification code
            var verifyRequest = new VerificationRequest 
            { 
                PhoneNumber = TestPhoneNumber,
                Code = "123456" // Valid code in test environment
            };
            var validateResult = await PostAsync<VerificationRequest, Result<AuthenticationResponse>>($"{BaseUrl}/validate", verifyRequest);
            
            // Assert - Step 2
            validateResult.Should().NotBeNull();
            validateResult.Succeeded.Should().BeTrue();
            validateResult.Data.Token.Should().NotBeEmpty();
            
            // Act - Step 3: Refresh token
            SetAuthToken(validateResult.Data.Token);
            var refreshResponse = await Client.PostAsync($"{BaseUrl}/refresh", null);
            
            // Assert - Step 3
            refreshResponse.IsSuccessStatusCode.Should().BeTrue();
            var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<Result<AuthenticationResponse>>(JsonOptions);
            
            refreshResult.Should().NotBeNull();
            refreshResult.Succeeded.Should().BeTrue();
            refreshResult.Data.Token.Should().NotBeEmpty();
            refreshResult.Data.Token.Should().NotBe(validateResult.Data.Token); // New token should be different
        }

        private async Task<string> GetValidTokenAsync()
        {
            // Request verification code
            var authRequest = new AuthenticationRequest { PhoneNumber = TestPhoneNumber };
            var authResult = await PostAsync<AuthenticationRequest, Result<string>>($"{BaseUrl}/verify", authRequest);
                
            // Validate code to get token
            var verifyRequest = new VerificationRequest 
            { 
                PhoneNumber = TestPhoneNumber,
                Code = "123456" // Valid code in test environment
            };
            var validateResult = await PostAsync<VerificationRequest, Result<AuthenticationResponse>>($"{BaseUrl}/validate", verifyRequest);
            
            return validateResult.Data.Token;
        }
    }
}