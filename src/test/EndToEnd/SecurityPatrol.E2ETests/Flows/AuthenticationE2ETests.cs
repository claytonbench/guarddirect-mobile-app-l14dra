using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Constants; // TestConstants

namespace SecurityPatrol.E2ETests.Flows
{
    /// <summary>
    /// End-to-end tests for the authentication flow in the Security Patrol application
    /// </summary>
    public class AuthenticationE2ETests : E2ETestBase
    {
        /// <summary>
        /// Initializes a new instance of the AuthenticationE2ETests class
        /// </summary>
        public AuthenticationE2ETests()
        {
            // Call base constructor to initialize test environment
        }

        /// <summary>
        /// Tests the complete authentication flow with valid credentials
        /// </summary>
        [Fact]
        public async Task TestSuccessfulAuthentication()
        {
            // Set up authentication success responses
            SetupAuthenticationSuccessResponse();

            // Request verification code with TestPhoneNumber
            bool requestSuccess = await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Assert that verification code request was successful
            requestSuccess.Should().BeTrue();

            // Verify code with TestVerificationCode
            bool verifySuccess = await AuthenticationService.VerifyCode(TestConstants.TestVerificationCode);

            // Assert that code verification was successful
            verifySuccess.Should().BeTrue();

            // Get authentication state
            AuthState authState = await AuthenticationService.GetAuthenticationState();

            // Assert that IsAuthenticated is true
            authState.IsAuthenticated.Should().BeTrue();

            // Assert that PhoneNumber matches TestPhoneNumber
            authState.PhoneNumber.Should().Be(TestConstants.TestPhoneNumber);
        }

        /// <summary>
        /// Tests authentication failure with an invalid phone number
        /// </summary>
        [Fact]
        public async Task TestInvalidPhoneNumber()
        {
            // Set up API error response for /auth/verify endpoint with 400 status code
            SetupApiErrorResponse("/auth/verify", 400, "Invalid phone number");

            // Request verification code with invalid phone number
            bool requestSuccess = await AuthenticationService.RequestVerificationCode("invalid_phone_number");

            // Assert that verification code request failed
            requestSuccess.Should().BeFalse();

            // Get authentication state
            AuthState authState = await AuthenticationService.GetAuthenticationState();

            // Assert that IsAuthenticated is false
            authState.IsAuthenticated.Should().BeFalse();
        }

        /// <summary>
        /// Tests authentication failure with an invalid verification code
        /// </summary>
        [Fact]
        public async Task TestInvalidVerificationCode()
        {
            // Set up authentication success response for /auth/verify endpoint
            SetupAuthenticationSuccessResponse();

            // Set up API error response for /auth/validate endpoint with 400 status code
            SetupApiErrorResponse("/auth/validate", 400, "Invalid verification code");

            // Request verification code with TestPhoneNumber
            bool requestSuccess = await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Assert that verification code request was successful
            requestSuccess.Should().BeTrue();

            // Verify code with invalid verification code
            bool verifySuccess = await AuthenticationService.VerifyCode("invalid_code");

            // Assert that code verification failed
            verifySuccess.Should().BeFalse();

            // Get authentication state
            AuthState authState = await AuthenticationService.GetAuthenticationState();

            // Assert that IsAuthenticated is false
            authState.IsAuthenticated.Should().BeFalse();
        }

        /// <summary>
        /// Tests the logout functionality after successful authentication
        /// </summary>
        [Fact]
        public async Task TestLogout()
        {
            // Set up authentication success responses
            SetupAuthenticationSuccessResponse();

            // Request verification code with TestPhoneNumber
            bool requestSuccess = await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Verify code with TestVerificationCode
            bool verifySuccess = await AuthenticationService.VerifyCode(TestConstants.TestVerificationCode);

            // Assert that authentication was successful
            verifySuccess.Should().BeTrue();

            // Call Logout method
            await AuthenticationService.Logout();

            // Get authentication state
            AuthState authState = await AuthenticationService.GetAuthenticationState();

            // Assert that IsAuthenticated is false
            authState.IsAuthenticated.Should().BeFalse();

            // Assert that PhoneNumber is null or empty
            string phoneNumber = authState.PhoneNumber;
            Assert.True(string.IsNullOrEmpty(phoneNumber));
        }

        /// <summary>
        /// Tests the token refresh functionality
        /// </summary>
        [Fact]
        public async Task TestTokenRefresh()
        {
            // Set up authentication success responses including refresh endpoint
            SetupAuthenticationSuccessResponse();

            // Authenticate user with TestPhoneNumber and TestVerificationCode
            bool authSuccess = await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);
            authSuccess = await AuthenticationService.VerifyCode(TestConstants.TestVerificationCode);

            // Assert that authentication was successful
            authSuccess.Should().BeTrue();

            // Call RefreshToken method
            bool refreshSuccess = await AuthenticationService.RefreshToken();

            // Assert that token refresh was successful
            refreshSuccess.Should().BeTrue();

            // Get authentication state
            AuthState authState = await AuthenticationService.GetAuthenticationState();

            // Assert that IsAuthenticated is still true
            authState.IsAuthenticated.Should().BeTrue();
        }

        /// <summary>
        /// Tests handling of authentication timeout scenario
        /// </summary>
        [Fact]
        public async Task TestAuthenticationTimeout()
        {
            // Set up API error response for /auth/verify endpoint with 408 status code (timeout)
            SetupApiErrorResponse("/auth/verify", 408, "Request timeout");

            // Request verification code with TestPhoneNumber
            bool requestSuccess = await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Assert that verification code request failed
            requestSuccess.Should().BeFalse();

            // Get authentication state
            AuthState authState = await AuthenticationService.GetAuthenticationState();

            // Assert that IsAuthenticated is false
            authState.IsAuthenticated.Should().BeFalse();
        }

        /// <summary>
        /// Tests handling of server unavailability during authentication
        /// </summary>
        [Fact]
        public async Task TestServerUnavailable()
        {
            // Set up API error response for /auth/verify endpoint with 503 status code (service unavailable)
            SetupApiErrorResponse("/auth/verify", 503, "Service unavailable");

            // Request verification code with TestPhoneNumber
            bool requestSuccess = await AuthenticationService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Assert that verification code request failed
            requestSuccess.Should().BeFalse();

            // Get authentication state
            AuthState authState = await AuthenticationService.GetAuthenticationState();

            // Assert that IsAuthenticated is false
            authState.IsAuthenticated.Should().BeFalse();
        }
    }
}