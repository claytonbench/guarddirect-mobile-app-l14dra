using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityPatrol.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.UnitTests.Helpers;
using SecurityPatrol.UnitTests.Helpers.MockServices;
using Xunit;

namespace SecurityPatrol.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the AuthenticationService class to verify its behavior and functionality.
    /// </summary>
    public class AuthenticationServiceTests
    {
        private readonly MockApiService _mockApiService;
        private readonly Mock<ITokenManager> _mockTokenManager;
        private readonly Mock<IAuthenticationStateProvider> _mockAuthStateProvider;
        private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
        private readonly AuthenticationService _authService;
        private readonly string _validPhoneNumber;
        private readonly string _validVerificationCode;
        private readonly string _validToken;

        /// <summary>
        /// Initializes a new instance of the AuthenticationServiceTests class and sets up the test environment
        /// </summary>
        public AuthenticationServiceTests()
        {
            // Initialize mocks
            _mockApiService = new MockApiService();
            _mockTokenManager = new Mock<ITokenManager>();
            _mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            _mockLogger = new Mock<ILogger<AuthenticationService>>();

            // Initialize the service with the mocked dependencies
            _authService = new AuthenticationService(
                _mockApiService,
                _mockTokenManager.Object,
                _mockAuthStateProvider.Object,
                _mockLogger.Object);

            // Set up test data
            _validPhoneNumber = "+1234567890";
            _validVerificationCode = "123456";
            _validToken = "valid-jwt-token";
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        private void Setup()
        {
            // Reset mocks
            _mockApiService.Reset();
            _mockTokenManager.Reset();
            _mockAuthStateProvider.Reset();

            // Set up default behavior for _mockAuthStateProvider.GetCurrentState()
            _mockAuthStateProvider
                .Setup(p => p.GetCurrentState())
                .ReturnsAsync(TestDataGenerator.CreateAuthState(false));

            // Set up default behavior for _mockTokenManager methods
            _mockTokenManager
                .Setup(m => m.IsTokenValid())
                .ReturnsAsync(true);

            _mockTokenManager
                .Setup(m => m.IsTokenExpiringSoon())
                .ReturnsAsync(false);
        }

        [Fact]
        public async Task RequestVerificationCode_WithValidPhoneNumber_ReturnsTrue()
        {
            // Arrange
            Setup();
            _mockApiService.SetupPostResponse<object>(ApiEndpoints.AuthVerify, new object());

            // Act
            bool result = await _authService.RequestVerificationCode(_validPhoneNumber);

            // Assert
            Assert.True(result);
            Assert.True(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthVerify));
        }

        [Fact]
        public async Task RequestVerificationCode_WithInvalidPhoneNumber_ReturnsFalse()
        {
            // Arrange
            Setup();

            // Act
            bool result = await _authService.RequestVerificationCode("invalid");

            // Assert
            Assert.False(result);
            Assert.False(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthVerify));
        }

        [Fact]
        public async Task RequestVerificationCode_WhenApiCallFails_ReturnsFalse()
        {
            // Arrange
            Setup();
            _mockApiService.SetupPostException(ApiEndpoints.AuthVerify, new Exception("API Error"));

            // Act
            bool result = await _authService.RequestVerificationCode(_validPhoneNumber);

            // Assert
            Assert.False(result);
            Assert.True(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthVerify));
        }

        [Fact]
        public async Task VerifyCode_WithValidCode_ReturnsTrue()
        {
            // Arrange
            Setup();
            
            // Set up the service with a valid phone number by calling RequestVerificationCode
            await _authService.RequestVerificationCode(_validPhoneNumber);
            
            // Set up _mockApiService to return a successful authentication response
            var authResponse = new AuthenticationResponse
            {
                Token = _validToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            _mockApiService.SetupPostResponse<AuthenticationResponse>(ApiEndpoints.AuthValidate, authResponse);

            // Act
            bool result = await _authService.VerifyCode(_validVerificationCode);

            // Assert
            Assert.True(result);
            Assert.True(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthValidate));
            _mockTokenManager.Verify(m => m.StoreToken(_validToken), Times.Once);
            _mockAuthStateProvider.Verify(p => p.UpdateState(It.Is<AuthState>(s => 
                s.IsAuthenticated && s.PhoneNumber == _validPhoneNumber)), Times.Once);
        }

        [Fact]
        public async Task VerifyCode_WithInvalidCode_ReturnsFalse()
        {
            // Arrange
            Setup();
            
            // Set up the service with a valid phone number by calling RequestVerificationCode
            await _authService.RequestVerificationCode(_validPhoneNumber);

            // Act
            bool result = await _authService.VerifyCode("12345"); // Invalid code (too short)

            // Assert
            Assert.False(result);
            Assert.False(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthValidate));
            _mockTokenManager.Verify(m => m.StoreToken(It.IsAny<string>()), Times.Never);
            _mockAuthStateProvider.Verify(p => p.UpdateState(It.IsAny<AuthState>()), Times.Never);
        }

        [Fact]
        public async Task VerifyCode_WithNoPhoneNumber_ReturnsFalse()
        {
            // Arrange
            Setup();
            // Do not call RequestVerificationCode first

            // Act
            bool result = await _authService.VerifyCode(_validVerificationCode);

            // Assert
            Assert.False(result);
            Assert.False(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthValidate));
            _mockTokenManager.Verify(m => m.StoreToken(It.IsAny<string>()), Times.Never);
            _mockAuthStateProvider.Verify(p => p.UpdateState(It.IsAny<AuthState>()), Times.Never);
        }

        [Fact]
        public async Task VerifyCode_WhenApiCallFails_ReturnsFalse()
        {
            // Arrange
            Setup();
            
            // Set up the service with a valid phone number by calling RequestVerificationCode
            await _authService.RequestVerificationCode(_validPhoneNumber);
            
            // Set up _mockApiService to throw an exception for the validation endpoint
            _mockApiService.SetupPostException(ApiEndpoints.AuthValidate, new Exception("API Error"));

            // Act
            bool result = await _authService.VerifyCode(_validVerificationCode);

            // Assert
            Assert.False(result);
            Assert.True(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthValidate));
            _mockTokenManager.Verify(m => m.StoreToken(It.IsAny<string>()), Times.Never);
            _mockAuthStateProvider.Verify(p => p.UpdateState(It.IsAny<AuthState>()), Times.Never);
        }

        [Fact]
        public async Task GetAuthenticationState_ReturnsCurrentState()
        {
            // Arrange
            Setup();
            var expectedState = TestDataGenerator.CreateAuthState(true, _validPhoneNumber);
            _mockAuthStateProvider
                .Setup(p => p.GetCurrentState())
                .ReturnsAsync(expectedState);

            // Act
            var result = await _authService.GetAuthenticationState();

            // Assert
            Assert.Equal(expectedState.IsAuthenticated, result.IsAuthenticated);
            Assert.Equal(expectedState.PhoneNumber, result.PhoneNumber);
        }

        [Fact]
        public async Task Logout_ClearsTokenAndUpdatesState()
        {
            // Arrange
            Setup();

            // Act
            await _authService.Logout();

            // Assert
            _mockTokenManager.Verify(m => m.ClearToken(), Times.Once);
            _mockAuthStateProvider.Verify(p => p.UpdateState(It.Is<AuthState>(s => 
                !s.IsAuthenticated && s.PhoneNumber == null)), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_WhenTokenValid_AndNotExpiringSoon_ReturnsTrue()
        {
            // Arrange
            Setup();
            _mockTokenManager.Setup(m => m.IsTokenValid()).ReturnsAsync(true);
            _mockTokenManager.Setup(m => m.IsTokenExpiringSoon()).ReturnsAsync(false);

            // Act
            bool result = await _authService.RefreshToken();

            // Assert
            Assert.True(result);
            Assert.False(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthRefresh));
        }

        [Fact]
        public async Task RefreshToken_WhenTokenValid_AndExpiringSoon_RefreshesToken()
        {
            // Arrange
            Setup();
            _mockTokenManager.Setup(m => m.IsTokenValid()).ReturnsAsync(true);
            _mockTokenManager.Setup(m => m.IsTokenExpiringSoon()).ReturnsAsync(true);
            _mockTokenManager.Setup(m => m.RetrieveToken()).ReturnsAsync(_validToken);
            
            var refreshResponse = new AuthenticationResponse
            {
                Token = "new-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            
            _mockApiService.SetupPostResponse<AuthenticationResponse>(ApiEndpoints.AuthRefresh, refreshResponse);

            // Act
            bool result = await _authService.RefreshToken();

            // Assert
            Assert.True(result);
            Assert.True(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthRefresh));
            _mockTokenManager.Verify(m => m.StoreToken("new-token"), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_WhenTokenInvalid_ReturnsFalse()
        {
            // Arrange
            Setup();
            _mockTokenManager.Setup(m => m.IsTokenValid()).ReturnsAsync(false);

            // Act
            bool result = await _authService.RefreshToken();

            // Assert
            Assert.False(result);
            Assert.False(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthRefresh));
        }

        [Fact]
        public async Task RefreshToken_WhenApiCallFails_ReturnsFalse()
        {
            // Arrange
            Setup();
            _mockTokenManager.Setup(m => m.IsTokenValid()).ReturnsAsync(true);
            _mockTokenManager.Setup(m => m.IsTokenExpiringSoon()).ReturnsAsync(true);
            _mockTokenManager.Setup(m => m.RetrieveToken()).ReturnsAsync(_validToken);
            
            _mockApiService.SetupPostException(ApiEndpoints.AuthRefresh, new Exception("API Error"));

            // Act
            bool result = await _authService.RefreshToken();

            // Assert
            Assert.False(result);
            Assert.True(_mockApiService.VerifyPostCalled(ApiEndpoints.AuthRefresh));
            _mockTokenManager.Verify(m => m.StoreToken(It.IsAny<string>()), Times.Never);
        }
    }
}