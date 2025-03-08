using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Models;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.API.IntegrationTests.Controllers
{
    /// <summary>
    /// Integration tests for the AuthController in the Security Patrol API, verifying the authentication flow using real HTTP requests.
    /// </summary>
    public class AuthControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the AuthControllerIntegrationTests class with the test factory.
        /// </summary>
        /// <param name="factory">The custom web application factory for creating the test server.</param>
        public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        /// <summary>
        /// Tests that the Verify endpoint returns a verification ID when provided with a valid phone number.
        /// </summary>
        [Fact]
        public async Task Verify_WithValidPhoneNumber_ReturnsVerificationId()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };

            // Act
            var result = await PostAsync<AuthenticationRequest, Result<string>>("/api/v1/auth/verify", request);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests that the Verify endpoint returns a bad request when provided with an invalid phone number.
        /// </summary>
        [Fact]
        public async Task Verify_WithInvalidPhoneNumber_ReturnsBadRequest()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = "invalid" };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/auth/verify", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Tests that the Validate endpoint returns an authentication token when provided with a valid verification code.
        /// </summary>
        [Fact]
        public async Task Validate_WithValidVerificationCode_ReturnsAuthToken()
        {
            // Arrange
            var request = new VerificationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };

            // Act
            var result = await PostAsync<VerificationRequest, Result<AuthenticationResponse>>("/api/v1/auth/validate", request);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Token.Should().NotBeNullOrEmpty();
            result.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        /// <summary>
        /// Tests that the Validate endpoint returns unauthorized when provided with an invalid verification code.
        /// </summary>
        [Fact]
        public async Task Validate_WithInvalidVerificationCode_ReturnsUnauthorized()
        {
            // Arrange
            var request = new VerificationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = "000000" // Invalid code
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/auth/validate", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests that the Refresh endpoint returns a new authentication token when provided with a valid token.
        /// </summary>
        [Fact]
        public async Task Refresh_WithValidToken_ReturnsNewToken()
        {
            // Arrange
            AuthenticateClient(TestConstants.TestAuthToken);

            // Act
            var result = await PostAsync<object, Result<AuthenticationResponse>>("/api/v1/auth/refresh", null);

            // Assert
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Token.Should().NotBeNullOrEmpty();
            result.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        /// <summary>
        /// Tests that the Refresh endpoint returns unauthorized when provided with an invalid token.
        /// </summary>
        [Fact]
        public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            AuthenticateClient("invalid-token");

            // Act
            var response = await Client.PostAsync("/api/v1/auth/refresh", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests that the Refresh endpoint returns unauthorized when no token is provided.
        /// </summary>
        [Fact]
        public async Task Refresh_WithNoToken_ReturnsUnauthorized()
        {
            // Arrange - no authorization header

            // Act
            var response = await Client.PostAsync("/api/v1/auth/refresh", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests the complete authentication flow from phone verification to token validation.
        /// </summary>
        [Fact]
        public async Task CompleteAuthenticationFlow_Success()
        {
            // Arrange
            var phoneRequest = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };

            // Act - Step 1: Request verification code
            var verifyResult = await PostAsync<AuthenticationRequest, Result<string>>("/api/v1/auth/verify", phoneRequest);
            verifyResult.Should().NotBeNull();
            verifyResult.Succeeded.Should().BeTrue();
            
            // Step 2: Submit verification code
            var verificationRequest = new VerificationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };
            var authResult = await PostAsync<VerificationRequest, Result<AuthenticationResponse>>("/api/v1/auth/validate", verificationRequest);
            authResult.Should().NotBeNull();
            authResult.Succeeded.Should().BeTrue();
            
            var token = authResult.Data.Token;
            
            // Step 3: Use token for refresh
            AuthenticateClient(token);
            var refreshResult = await PostAsync<object, Result<AuthenticationResponse>>("/api/v1/auth/refresh", null);
            
            // Assert
            refreshResult.Should().NotBeNull();
            refreshResult.Succeeded.Should().BeTrue();
            refreshResult.Data.Should().NotBeNull();
            refreshResult.Data.Token.Should().NotBeNullOrEmpty();
            refreshResult.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }
    }
}