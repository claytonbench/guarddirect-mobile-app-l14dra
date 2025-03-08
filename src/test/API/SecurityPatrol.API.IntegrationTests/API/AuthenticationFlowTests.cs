using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SecurityPatrol.API.IntegrationTests.Setup;
using SecurityPatrol.Core.Models;
using SecurityPatrol.TestCommon.Constants;
using Xunit;

namespace SecurityPatrol.API.IntegrationTests.API
{
    /// <summary>
    /// Integration tests for the authentication flow in the Security Patrol API, testing the complete
    /// two-step authentication process including phone number verification and code validation.
    /// </summary>
    public class AuthenticationFlowTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the AuthenticationFlowTests class with the test factory.
        /// </summary>
        /// <param name="factory">The factory that creates a test server with in-memory database.</param>
        public AuthenticationFlowTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        /// <summary>
        /// Tests that the /auth/verify endpoint successfully accepts a phone number and returns a verification ID.
        /// </summary>
        [Fact]
        public async Task Should_Request_Verification_Code_Successfully()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/auth/verify", request);
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadFromJsonAsync<Result<string>>();
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests that the /auth/validate endpoint successfully validates a verification code and returns an authentication token.
        /// </summary>
        [Fact]
        public async Task Should_Validate_Verification_Code_Successfully()
        {
            // Arrange
            var request = new VerificationRequest 
            { 
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/auth/validate", request);
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var result = await response.Content.ReadFromJsonAsync<Result<AuthenticationResponse>>();
            result.Should().NotBeNull();
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Token.Should().NotBeNullOrEmpty();
            result.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        /// <summary>
        /// Tests that the /auth/validate endpoint rejects an invalid verification code with an appropriate error.
        /// </summary>
        [Fact]
        public async Task Should_Reject_Invalid_Verification_Code()
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
            var result = await response.Content.ReadFromJsonAsync<Result>();
            result.Should().NotBeNull();
            result.Succeeded.Should().BeFalse();
            result.Message.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests the complete authentication flow from verification request through code validation to receiving a valid token.
        /// </summary>
        [Fact]
        public async Task Should_Complete_Full_Authentication_Flow()
        {
            // Arrange - Step 1: Request verification code
            var authRequest = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };
            
            // Act - Step 1
            var authResponse = await Client.PostAsJsonAsync("/api/v1/auth/verify", authRequest);
            
            // Assert - Step 1
            authResponse.IsSuccessStatusCode.Should().BeTrue();
            var authResult = await authResponse.Content.ReadFromJsonAsync<Result<string>>();
            authResult.Succeeded.Should().BeTrue();
            var verificationId = authResult.Data;
            
            // Arrange - Step 2: Validate verification code
            var verifyRequest = new VerificationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };
            
            // Act - Step 2
            var verifyResponse = await Client.PostAsJsonAsync("/api/v1/auth/validate", verifyRequest);
            
            // Assert - Step 2
            verifyResponse.IsSuccessStatusCode.Should().BeTrue();
            var tokenResult = await verifyResponse.Content.ReadFromJsonAsync<Result<AuthenticationResponse>>();
            tokenResult.Succeeded.Should().BeTrue();
            tokenResult.Data.Should().NotBeNull();
            tokenResult.Data.Token.Should().NotBeNullOrEmpty();
            tokenResult.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            
            // Act - Step 3: Try to use the token for an authenticated request
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.Data.Token);
            var protectedResponse = await Client.GetAsync("/api/v1/users/current");
            
            // Assert - Step 3
            protectedResponse.IsSuccessStatusCode.Should().BeTrue();
        }

        /// <summary>
        /// Tests that the /auth/refresh endpoint successfully refreshes an authentication token.
        /// </summary>
        [Fact]
        public async Task Should_Refresh_Token_Successfully()
        {
            // Arrange - Complete the authentication flow first
            var authRequest = new AuthenticationRequest { PhoneNumber = TestConstants.TestPhoneNumber };
            var authResponse = await Client.PostAsJsonAsync("/api/v1/auth/verify", authRequest);
            authResponse.EnsureSuccessStatusCode();
            
            var verifyRequest = new VerificationRequest
            {
                PhoneNumber = TestConstants.TestPhoneNumber,
                Code = TestConstants.TestVerificationCode
            };
            var verifyResponse = await Client.PostAsJsonAsync("/api/v1/auth/validate", verifyRequest);
            verifyResponse.EnsureSuccessStatusCode();
            
            var tokenResult = await verifyResponse.Content.ReadFromJsonAsync<Result<AuthenticationResponse>>();
            var originalToken = tokenResult.Data.Token;
            
            // Set the authorization header with the token
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", originalToken);

            // Act - Refresh the token
            var refreshResponse = await Client.PostAsync("/api/v1/auth/refresh", null);
            
            // Assert
            refreshResponse.IsSuccessStatusCode.Should().BeTrue();
            var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<Result<AuthenticationResponse>>();
            refreshResult.Should().NotBeNull();
            refreshResult.Succeeded.Should().BeTrue();
            refreshResult.Data.Should().NotBeNull();
            refreshResult.Data.Token.Should().NotBeNullOrEmpty();
            refreshResult.Data.Token.Should().NotBe(originalToken); // Should be a new token
            refreshResult.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }
    }
}