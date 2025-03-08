using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Constants;

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the AuthenticationService class to verify its functionality for phone number verification, token management, and session state.
    /// </summary>
    public class AuthenticationServiceTests : TestBase
    {
        /// <summary>
        /// Initializes test dependencies before each test.
        /// </summary>
        public void Setup()
        {
            // Setup base mocks
            MockApiService.Reset();
            MockTokenManager.Reset();
        }

        /// <summary>
        /// Creates an instance of the AuthenticationService with mocked dependencies for testing.
        /// </summary>
        private AuthenticationService CreateAuthenticationService()
        {
            var mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            var mockLogger = new Mock<ILogger<AuthenticationService>>();

            return new AuthenticationService(
                MockApiService.Object,
                MockTokenManager.Object,
                mockAuthStateProvider.Object,
                mockLogger.Object);
        }

        [Fact]
        public async Task RequestVerificationCode_ValidPhoneNumber_ReturnsTrue()
        {
            // Arrange
            Setup();
            
            // Setup API service to return success
            MockApiService
                .Setup(x => x.PostAsync<object>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .ReturnsAsync(new object());
            
            var authService = CreateAuthenticationService();

            // Act
            var result = await authService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Assert
            result.Should().BeTrue();
            MockApiService.Verify(x => x.PostAsync<object>(
                It.Is<string>(url => url == ApiEndpoints.AuthVerify),
                It.IsAny<AuthenticationRequest>(), 
                It.Is<bool>(requiresAuth => requiresAuth == false)), 
                Times.Once);
        }

        [Fact]
        public async Task RequestVerificationCode_InvalidPhoneNumber_ReturnsFalse()
        {
            // Arrange
            Setup();
            var authService = CreateAuthenticationService();

            // Act
            var result = await authService.RequestVerificationCode(null);

            // Assert
            result.Should().BeFalse();
            MockApiService.Verify(x => x.PostAsync<object>(
                It.IsAny<string>(), 
                It.IsAny<object>(), 
                It.IsAny<bool>()), 
                Times.Never);
        }

        [Fact]
        public async Task RequestVerificationCode_ApiFailure_ReturnsFalse()
        {
            // Arrange
            Setup();
            
            // Setup API service to throw exception
            MockApiService
                .Setup(x => x.PostAsync<object>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Test API exception"));
            
            var authService = CreateAuthenticationService();

            // Act
            var result = await authService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task VerifyCode_ValidCode_ReturnsTrue()
        {
            // Arrange
            Setup();
            
            // Set up AuthenticationResponse
            var authResponse = new AuthenticationResponse
            {
                Token = TestConstants.TestAuthToken,
                ExpiresAt = DateTime.Now.AddHours(1)
            };
            
            // Set up API response
            MockApiService
                .Setup(x => x.PostAsync<AuthenticationResponse>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .ReturnsAsync(authResponse);
            
            // Set up mock authentication state provider
            var mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            mockAuthStateProvider
                .Setup(x => x.UpdateState(It.IsAny<AuthState>()))
                .Verifiable();
            
            // Create service with explicit mock auth state provider
            var mockLogger = new Mock<ILogger<AuthenticationService>>();
            var authService = new AuthenticationService(
                MockApiService.Object,
                MockTokenManager.Object,
                mockAuthStateProvider.Object,
                mockLogger.Object);
            
            // First call RequestVerificationCode to store phone number
            await authService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Act
            var result = await authService.VerifyCode(TestConstants.TestVerificationCode);

            // Assert
            result.Should().BeTrue();
            MockApiService.Verify(x => x.PostAsync<AuthenticationResponse>(
                It.Is<string>(url => url == ApiEndpoints.AuthValidate),
                It.IsAny<VerificationRequest>(),
                It.Is<bool>(requiresAuth => requiresAuth == false)),
                Times.Once);
            MockTokenManager.Verify(x => x.StoreToken(It.Is<string>(token => token == TestConstants.TestAuthToken)), Times.Once);
            mockAuthStateProvider.Verify(x => x.UpdateState(It.Is<AuthState>(state => 
                state.IsAuthenticated && state.PhoneNumber == TestConstants.TestPhoneNumber)), Times.Once);
        }

        [Fact]
        public async Task VerifyCode_InvalidCode_ReturnsFalse()
        {
            // Arrange
            Setup();
            var authService = CreateAuthenticationService();
            
            // First call RequestVerificationCode to store phone number
            await authService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Act
            var result = await authService.VerifyCode(null);

            // Assert
            result.Should().BeFalse();
            MockApiService.Verify(x => x.PostAsync<AuthenticationResponse>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task VerifyCode_NoPhoneNumber_ReturnsFalse()
        {
            // Arrange
            Setup();
            var authService = CreateAuthenticationService();
            
            // No call to RequestVerificationCode, so no phone number is stored

            // Act
            var result = await authService.VerifyCode(TestConstants.TestVerificationCode);

            // Assert
            result.Should().BeFalse();
            MockApiService.Verify(x => x.PostAsync<AuthenticationResponse>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task VerifyCode_ApiFailure_ReturnsFalse()
        {
            // Arrange
            Setup();
            
            // Setup API service to throw exception
            MockApiService
                .Setup(x => x.PostAsync<AuthenticationResponse>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Test API exception"));
            
            var authService = CreateAuthenticationService();
            
            // First call RequestVerificationCode to store phone number
            await authService.RequestVerificationCode(TestConstants.TestPhoneNumber);

            // Act
            var result = await authService.VerifyCode(TestConstants.TestVerificationCode);

            // Assert
            result.Should().BeFalse();
            MockTokenManager.Verify(x => x.StoreToken(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAuthenticationState_ReturnsCurrentState()
        {
            // Arrange
            Setup();
            
            // Create expected state
            var expectedState = AuthState.CreateAuthenticated(TestConstants.TestPhoneNumber);
            
            // Setup auth state provider to return the expected state
            var mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            mockAuthStateProvider
                .Setup(x => x.GetCurrentState())
                .ReturnsAsync(expectedState);
            
            // Create service with explicit mock auth state provider
            var mockLogger = new Mock<ILogger<AuthenticationService>>();
            var authService = new AuthenticationService(
                MockApiService.Object,
                MockTokenManager.Object,
                mockAuthStateProvider.Object,
                mockLogger.Object);

            // Act
            var result = await authService.GetAuthenticationState();

            // Assert
            result.Should().Be(expectedState);
            result.IsAuthenticated.Should().BeTrue();
            result.PhoneNumber.Should().Be(TestConstants.TestPhoneNumber);
        }

        [Fact]
        public async Task Logout_ClearsTokenAndUpdatesState()
        {
            // Arrange
            Setup();
            
            // Setup auth state provider
            var mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            mockAuthStateProvider
                .Setup(x => x.UpdateState(It.IsAny<AuthState>()))
                .Verifiable();
            
            // Create service with explicit mock auth state provider
            var mockLogger = new Mock<ILogger<AuthenticationService>>();
            var authService = new AuthenticationService(
                MockApiService.Object,
                MockTokenManager.Object,
                mockAuthStateProvider.Object,
                mockLogger.Object);

            // Act
            await authService.Logout();

            // Assert
            MockTokenManager.Verify(x => x.ClearToken(), Times.Once);
            mockAuthStateProvider.Verify(x => x.UpdateState(It.Is<AuthState>(state => !state.IsAuthenticated)), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_ValidToken_ReturnsTrue()
        {
            // Arrange
            Setup();
            
            // Setup token manager
            MockTokenManager
                .Setup(x => x.IsTokenValid())
                .ReturnsAsync(true);
            
            MockTokenManager
                .Setup(x => x.IsTokenExpiringSoon())
                .ReturnsAsync(false);
            
            var authService = CreateAuthenticationService();

            // Act
            var result = await authService.RefreshToken();

            // Assert
            result.Should().BeTrue();
            MockApiService.Verify(x => x.PostAsync<AuthenticationResponse>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<bool>()),
                Times.Never); // No refresh should be performed since token is not expiring
        }

        [Fact]
        public async Task RefreshToken_ExpiringToken_RefreshesSuccessfully()
        {
            // Arrange
            Setup();
            
            // Setup token manager
            MockTokenManager
                .Setup(x => x.IsTokenValid())
                .ReturnsAsync(true);
            
            MockTokenManager
                .Setup(x => x.IsTokenExpiringSoon())
                .ReturnsAsync(true);
            
            MockTokenManager
                .Setup(x => x.RetrieveToken())
                .ReturnsAsync(TestConstants.TestAuthToken);
            
            // Setup API response
            var authResponse = new AuthenticationResponse
            {
                Token = "new-token",
                ExpiresAt = DateTime.Now.AddHours(1)
            };
            
            MockApiService
                .Setup(x => x.PostAsync<AuthenticationResponse>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .ReturnsAsync(authResponse);
            
            var authService = CreateAuthenticationService();

            // Act
            var result = await authService.RefreshToken();

            // Assert
            result.Should().BeTrue();
            MockApiService.Verify(x => x.PostAsync<AuthenticationResponse>(
                It.Is<string>(url => url == ApiEndpoints.AuthRefresh),
                It.IsAny<object>(),
                It.Is<bool>(requiresAuth => requiresAuth == false)),
                Times.Once);
            MockTokenManager.Verify(x => x.StoreToken(It.Is<string>(token => token == "new-token")), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_InvalidToken_ReturnsFalse()
        {
            // Arrange
            Setup();
            
            // Setup token manager to indicate invalid token
            MockTokenManager
                .Setup(x => x.IsTokenValid())
                .ReturnsAsync(false);
            
            var authService = CreateAuthenticationService();

            // Act
            var result = await authService.RefreshToken();

            // Assert
            result.Should().BeFalse();
            MockApiService.Verify(x => x.PostAsync<AuthenticationResponse>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task RefreshToken_ApiFailure_ReturnsFalse()
        {
            // Arrange
            Setup();
            
            // Setup token manager
            MockTokenManager
                .Setup(x => x.IsTokenValid())
                .ReturnsAsync(true);
            
            MockTokenManager
                .Setup(x => x.IsTokenExpiringSoon())
                .ReturnsAsync(true);
            
            MockTokenManager
                .Setup(x => x.RetrieveToken())
                .ReturnsAsync(TestConstants.TestAuthToken);
            
            // Setup API to throw exception
            MockApiService
                .Setup(x => x.PostAsync<AuthenticationResponse>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Test API exception"));
            
            var authService = CreateAuthenticationService();

            // Act
            var result = await authService.RefreshToken();

            // Assert
            result.Should().BeFalse();
            MockTokenManager.Verify(x => x.StoreToken(It.IsAny<string>()), Times.Never);
        }
    }
}