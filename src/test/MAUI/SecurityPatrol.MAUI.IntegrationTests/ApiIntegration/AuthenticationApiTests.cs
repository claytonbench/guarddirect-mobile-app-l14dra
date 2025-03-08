using System;
using System.Threading.Tasks;
using System.Text.Json; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Constants;

namespace SecurityPatrol.MAUI.IntegrationTests.ApiIntegration
{
    /// <summary>
    /// Integration tests for the authentication API functionality in the Security Patrol application.
    /// </summary>
    [public]
    public class AuthenticationApiTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the AuthenticationApiTests class
        /// </summary>
        public AuthenticationApiTests()
        {
            // Call base constructor to initialize the IntegrationTestBase
        }

        /// <summary>
        /// Tests that requesting a verification code with a valid phone number returns true
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task RequestVerificationCode_WithValidPhoneNumber_ShouldReturnTrue()
        {
            // Set up a success response for the auth/verify endpoint with a verification ID
            ApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, new { VerificationId = Guid.NewGuid() });

            // Call AuthenticationService.RequestVerificationCode with TestPhoneNumber
            bool result = await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Assert that the result is true
            result.Should().BeTrue();

            // Verify that the API server received a request to the auth/verify endpoint
            ApiServer.GetRequestCount(ApiEndpoints.AuthVerify).Should().Be(1);

            // Verify that the request body contained the correct phone number
            string requestBody = ApiServer.GetLastRequestBody(ApiEndpoints.AuthVerify);
            var request = JsonSerializer.Deserialize<AuthenticationRequest>(requestBody);
            request.PhoneNumber.Should().Be(TestConstants.TestPhoneNumber);
        }

        /// <summary>
        /// Tests that requesting a verification code with an invalid phone number returns false
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task RequestVerificationCode_WithInvalidPhoneNumber_ShouldReturnFalse()
        {
            // Set up an error response for the auth/verify endpoint with a 400 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.AuthVerify, 400, "Invalid phone number");

            // Call AuthenticationService.RequestVerificationCode with an invalid phone number
            bool result = await AuthenticationService.RequestVerificationCode("invalid-phone-number");

            // Assert that the result is false
            result.Should().BeFalse();

            // Verify that the API server received a request to the auth/verify endpoint
            ApiServer.GetRequestCount(ApiEndpoints.AuthVerify).Should().Be(1);
        }

        /// <summary>
        /// Tests that verifying a valid code returns true and stores the authentication token
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task VerifyCode_WithValidCode_ShouldReturnTrue()
        {
            // Set up a success response for the auth/verify endpoint with a verification ID
            ApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, new { VerificationId = Guid.NewGuid() });

            // Call AuthenticationService.RequestVerificationCode with TestPhoneNumber to set up the phone number
            await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Set up a success response for the auth/validate endpoint with a token and expiry
            ApiServer.SetupSuccessResponse(ApiEndpoints.AuthValidate, new AuthenticationResponse
            {
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            // Call AuthenticationService.VerifyCode with TestVerificationCode
            bool result = await AuthenticationService.VerifyCode(TestConstants.TestVerificationCode);

            // Assert that the result is true
            result.Should().BeTrue();

            // Verify that the API server received a request to the auth/validate endpoint
            ApiServer.GetRequestCount(ApiEndpoints.AuthValidate).Should().Be(1);

            // Verify that the request body contained the correct phone number and verification code
            string requestBody = ApiServer.GetLastRequestBody(ApiEndpoints.AuthValidate);
            var request = JsonSerializer.Deserialize<VerificationRequest>(requestBody);
            request.PhoneNumber.Should().Be(TestConstants.TestPhoneNumber);
            request.VerificationCode.Should().Be(TestConstants.TestVerificationCode);

            // Get the authentication state and verify that IsAuthenticated is true
            var authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeTrue();

            // Verify that the authentication state contains the correct phone number
            authState.PhoneNumber.Should().Be(TestConstants.TestPhoneNumber);
        }

        /// <summary>
        /// Tests that verifying an invalid code returns false
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task VerifyCode_WithInvalidCode_ShouldReturnFalse()
        {
            // Set up a success response for the auth/verify endpoint with a verification ID
            ApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, new { VerificationId = Guid.NewGuid() });

            // Call AuthenticationService.RequestVerificationCode with TestPhoneNumber to set up the phone number
            await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Set up an error response for the auth/validate endpoint with a 400 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.AuthValidate, 400, "Invalid verification code");

            // Call AuthenticationService.VerifyCode with an invalid verification code
            bool result = await AuthenticationService.VerifyCode("invalid-code");

            // Assert that the result is false
            result.Should().BeFalse();

            // Verify that the API server received a request to the auth/validate endpoint
            ApiServer.GetRequestCount(ApiEndpoints.AuthValidate).Should().Be(1);

            // Get the authentication state and verify that IsAuthenticated is false
            var authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeFalse();
        }

        /// <summary>
        /// Tests that verifying a code without first requesting a verification code returns false
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task VerifyCode_WithoutRequestingCode_ShouldReturnFalse()
        {
            // Call AuthenticationService.VerifyCode with TestVerificationCode without first calling RequestVerificationCode
            bool result = await AuthenticationService.VerifyCode(TestConstants.TestVerificationCode);

            // Assert that the result is false
            result.Should().BeFalse();

            // Verify that no request was made to the auth/validate endpoint
            ApiServer.GetRequestCount(ApiEndpoints.AuthValidate).Should().Be(0);

            // Get the authentication state and verify that IsAuthenticated is false
            var authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeFalse();
        }

        /// <summary>
        /// Tests that refreshing a valid token returns true and updates the token
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task RefreshToken_WithValidToken_ShouldReturnTrue()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Set up a success response for the auth/refresh endpoint with a new token and expiry
            ApiServer.SetupSuccessResponse(ApiEndpoints.AuthRefresh, new AuthenticationResponse
            {
                Token = "new-auth-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            // Call AuthenticationService.RefreshToken
            bool result = await AuthenticationService.RefreshToken();

            // Assert that the result is true
            result.Should().BeTrue();

            // Verify that the API server received a request to the auth/refresh endpoint
            ApiServer.GetRequestCount(ApiEndpoints.AuthRefresh).Should().Be(1);

            // Get the authentication state and verify that IsAuthenticated is still true
            var authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeTrue();
        }

        /// <summary>
        /// Tests that refreshing an invalid token returns false
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task RefreshToken_WithInvalidToken_ShouldReturnFalse()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Set up an error response for the auth/refresh endpoint with a 401 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.AuthRefresh, 401, "Invalid token");

            // Call AuthenticationService.RefreshToken
            bool result = await AuthenticationService.RefreshToken();

            // Assert that the result is false
            result.Should().BeFalse();

            // Verify that the API server received a request to the auth/refresh endpoint
            ApiServer.GetRequestCount(ApiEndpoints.AuthRefresh).Should().Be(1);
        }

        /// <summary>
        /// Tests that attempting to refresh a token when not authenticated returns false
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task RefreshToken_WhenNotAuthenticated_ShouldReturnFalse()
        {
            // Call AuthenticationService.RefreshToken without first authenticating
            bool result = await AuthenticationService.RefreshToken();

            // Assert that the result is false
            result.Should().BeFalse();

            // Verify that no request was made to the auth/refresh endpoint
            ApiServer.GetRequestCount(ApiEndpoints.AuthRefresh).Should().Be(0);
        }

        /// <summary>
        /// Tests that logging out when authenticated clears the authentication state
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task Logout_WhenAuthenticated_ShouldClearAuthenticationState()
        {
            // Call AuthenticateAsync to authenticate the user
            await AuthenticateAsync();

            // Get the authentication state and verify that IsAuthenticated is true
            var authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeTrue();

            // Call AuthenticationService.Logout
            await AuthenticationService.Logout();

            // Get the authentication state and verify that IsAuthenticated is now false
            authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeFalse();
        }

        /// <summary>
        /// Tests that logging out when not authenticated does not change the authentication state
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task Logout_WhenNotAuthenticated_ShouldNotChangeState()
        {
            // Get the authentication state and verify that IsAuthenticated is false
            var authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeFalse();

            // Call AuthenticationService.Logout
            await AuthenticationService.Logout();

            // Get the authentication state and verify that IsAuthenticated is still false
            authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeFalse();
        }

        /// <summary>
        /// Tests the complete authentication flow from requesting a code to verification to logout
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task CompleteAuthenticationFlow_ShouldSucceed()
        {
            // Set up a success response for the auth/verify endpoint with a verification ID
            ApiServer.SetupSuccessResponse(ApiEndpoints.AuthVerify, new { VerificationId = Guid.NewGuid() });

            // Call AuthenticationService.RequestVerificationCode with TestPhoneNumber
            bool requestResult = await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Assert that the result is true
            requestResult.Should().BeTrue();

            // Set up a success response for the auth/validate endpoint with a token and expiry
            ApiServer.SetupSuccessResponse(ApiEndpoints.AuthValidate, new AuthenticationResponse
            {
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            // Call AuthenticationService.VerifyCode with TestVerificationCode
            bool verifyResult = await AuthenticationService.VerifyCode(TestConstants.TestVerificationCode);

            // Assert that the result is true
            verifyResult.Should().BeTrue();

            // Get the authentication state and verify that IsAuthenticated is true
            var authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeTrue();

            // Set up a success response for the auth/refresh endpoint with a new token and expiry
            ApiServer.SetupSuccessResponse(ApiEndpoints.AuthRefresh, new AuthenticationResponse
            {
                Token = "new-auth-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            // Call AuthenticationService.RefreshToken
            bool refreshResult = await AuthenticationService.RefreshToken();

            // Assert that the result is true
            refreshResult.Should().BeTrue();

            // Call AuthenticationService.Logout
            await AuthenticationService.Logout();

            // Get the authentication state and verify that IsAuthenticated is now false
            authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeFalse();
        }

        /// <summary>
        /// Tests that the authentication service handles server errors gracefully
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task AuthenticationService_WithServerError_ShouldHandleGracefully()
        {
            // Set up an error response for the auth/verify endpoint with a 500 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.AuthVerify, 500, "Internal Server Error");

            // Call AuthenticationService.RequestVerificationCode with TestPhoneNumber
            bool requestResult = await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Assert that the result is false
            requestResult.Should().BeFalse();

            // Verify that the API server received a request to the auth/verify endpoint
            ApiServer.GetRequestCount(ApiEndpoints.AuthVerify).Should().Be(1);

            // Set up an error response for the auth/validate endpoint with a 500 status code
            ApiServer.SetupErrorResponse(ApiEndpoints.AuthValidate, 500, "Internal Server Error");

            // Call AuthenticationService.VerifyCode with TestVerificationCode
            bool verifyResult = await AuthenticationService.VerifyCode(TestConstants.TestVerificationCode);

            // Assert that the result is false
            verifyResult.Should().BeFalse();

            // Get the authentication state and verify that IsAuthenticated is false
            var authState = await AuthenticationService.GetAuthenticationState();
            authState.IsAuthenticated.Should().BeFalse();
        }
    }
}