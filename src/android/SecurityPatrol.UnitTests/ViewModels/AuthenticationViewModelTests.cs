using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using SecurityPatrol.Constants;
using SecurityPatrol.Services;
using SecurityPatrol.UnitTests.Helpers.MockServices;
using SecurityPatrol.ViewModels;
using Xunit;

namespace SecurityPatrol.UnitTests.ViewModels
{
    /// <summary>
    /// Contains unit tests for the AuthenticationViewModel class
    /// </summary>
    public class AuthenticationViewModelTests
    {
        private Mock<INavigationService> _mockNavigationService;
        private Mock<IAuthenticationStateProvider> _mockAuthStateProvider;
        private MockAuthenticationService _mockAuthService;
        private AuthenticationViewModel _viewModel;
        private readonly string _testPhoneNumber = "+1234567890";

        /// <summary>
        /// Initializes a new instance of the AuthenticationViewModelTests class and sets up common test dependencies
        /// </summary>
        public AuthenticationViewModelTests()
        {
            Setup();
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        private void Setup()
        {
            _mockNavigationService = new Mock<INavigationService>();
            _mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            _mockAuthService = new MockAuthenticationService();
            
            _mockAuthStateProvider.Setup(p => p.IsAuthenticated()).ReturnsAsync(false);
            _mockNavigationService.Setup(n => n.GetRouteParameter(NavigationConstants.ParamPhoneNumber))
                .Returns(_testPhoneNumber);
            
            _viewModel = new AuthenticationViewModel(
                _mockNavigationService.Object,
                _mockAuthStateProvider.Object,
                _mockAuthService);

            _viewModel.InitializeAsync().Wait();
        }

        /// <summary>
        /// Tests that InitializeAsync navigates to MainPage when user is already authenticated
        /// </summary>
        [Fact]
        public async Task InitializeAsync_WhenUserIsAuthenticated_NavigatesToMainPage()
        {
            // Arrange
            _mockAuthStateProvider.Setup(p => p.IsAuthenticated()).ReturnsAsync(true);
            var viewModel = new AuthenticationViewModel(
                _mockNavigationService.Object,
                _mockAuthStateProvider.Object,
                _mockAuthService);

            // Act
            await viewModel.InitializeAsync();

            // Assert
            _mockNavigationService.Verify(n => n.NavigateToAsync(NavigationConstants.MainPage, null), Times.Once());
        }

        /// <summary>
        /// Tests that InitializeAsync navigates back when phone number parameter is null
        /// </summary>
        [Fact]
        public async Task InitializeAsync_WhenPhoneNumberIsNull_NavigatesBack()
        {
            // Arrange
            _mockNavigationService.Setup(n => n.GetRouteParameter(NavigationConstants.ParamPhoneNumber))
                .Returns(null);
            var viewModel = new AuthenticationViewModel(
                _mockNavigationService.Object,
                _mockAuthStateProvider.Object,
                _mockAuthService);

            // Act
            await viewModel.InitializeAsync();

            // Assert
            _mockNavigationService.Verify(n => n.NavigateBackAsync(), Times.Once());
        }

        /// <summary>
        /// Tests that ValidateVerificationCode returns true for a valid verification code
        /// </summary>
        [Fact]
        public void ValidateVerificationCode_WithValidCode_ReturnsTrue()
        {
            // Arrange
            _viewModel.VerificationCode = "123456";

            // Act
            _viewModel.ValidateVerificationCode.Execute(null);

            // Assert
            Assert.True(_viewModel.IsVerificationCodeValid);
            Assert.False(_viewModel.HasError);
        }

        /// <summary>
        /// Tests that ValidateVerificationCode returns false for an invalid verification code
        /// </summary>
        [Fact]
        public void ValidateVerificationCode_WithInvalidCode_ReturnsFalse()
        {
            // Arrange
            _viewModel.VerificationCode = "12345"; // Too short

            // Act
            _viewModel.ValidateVerificationCode.Execute(null);

            // Assert
            Assert.False(_viewModel.IsVerificationCodeValid);
            Assert.True(_viewModel.HasError);
            Assert.Equal(ErrorMessages.InvalidVerificationCode, _viewModel.ErrorMessage);
        }

        /// <summary>
        /// Tests that ValidateVerificationCode returns false for an empty verification code
        /// </summary>
        [Fact]
        public void ValidateVerificationCode_WithEmptyCode_ReturnsFalse()
        {
            // Arrange
            _viewModel.VerificationCode = string.Empty;

            // Act
            _viewModel.ValidateVerificationCode.Execute(null);

            // Assert
            Assert.False(_viewModel.IsVerificationCodeValid);
            Assert.True(_viewModel.HasError);
            Assert.Equal(ErrorMessages.InvalidVerificationCode, _viewModel.ErrorMessage);
        }

        /// <summary>
        /// Tests that ValidateVerificationCode returns false for a code containing non-digit characters
        /// </summary>
        [Fact]
        public void ValidateVerificationCode_WithNonDigitCode_ReturnsFalse()
        {
            // Arrange
            _viewModel.VerificationCode = "12345A";

            // Act
            _viewModel.ValidateVerificationCode.Execute(null);

            // Assert
            Assert.False(_viewModel.IsVerificationCodeValid);
            Assert.True(_viewModel.HasError);
            Assert.Equal(ErrorMessages.InvalidVerificationCode, _viewModel.ErrorMessage);
        }

        /// <summary>
        /// Tests that VerifyCodeAsync does not call the authentication service when the code is invalid
        /// </summary>
        [Fact]
        public async Task VerifyCodeAsync_WithInvalidCode_DoesNotCallService()
        {
            // Arrange
            _viewModel.VerificationCode = "12345"; // Invalid

            // Act
            await _viewModel.VerifyCodeAsync.ExecuteAsync(null);

            // Assert
            Assert.False(_mockAuthService.VerifyVerifyCodeCalled("12345"));
        }

        /// <summary>
        /// Tests that VerifyCodeAsync calls the authentication service when the code is valid
        /// </summary>
        [Fact]
        public async Task VerifyCodeAsync_WithValidCode_CallsService()
        {
            // Arrange
            _viewModel.VerificationCode = "123456"; // Valid

            // Act
            await _viewModel.VerifyCodeAsync.ExecuteAsync(null);

            // Assert
            Assert.True(_mockAuthService.VerifyVerifyCodeCalled("123456"));
        }

        /// <summary>
        /// Tests that VerifyCodeAsync navigates to MainPage when verification is successful
        /// </summary>
        [Fact]
        public async Task VerifyCodeAsync_WhenSuccessful_NavigatesToMainPage()
        {
            // Arrange
            _mockAuthService.SetupVerifyCodeResult(true);
            _viewModel.VerificationCode = "123456";

            // Act
            await _viewModel.VerifyCodeAsync.ExecuteAsync(null);

            // Assert
            _mockNavigationService.Verify(n => n.NavigateToAsync(NavigationConstants.MainPage, null), Times.Once());
        }

        /// <summary>
        /// Tests that VerifyCodeAsync sets error message when verification fails
        /// </summary>
        [Fact]
        public async Task VerifyCodeAsync_WhenFailed_SetsErrorMessage()
        {
            // Arrange
            _mockAuthService.SetupVerifyCodeResult(false);
            _viewModel.VerificationCode = "123456";

            // Act
            await _viewModel.VerifyCodeAsync.ExecuteAsync(null);

            // Assert
            Assert.True(_viewModel.HasError);
            Assert.Equal(ErrorMessages.AuthenticationFailed, _viewModel.ErrorMessage);
            _mockNavigationService.Verify(n => n.NavigateToAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Never());
        }

        /// <summary>
        /// Tests that VerifyCodeAsync sets network error message when a network exception occurs
        /// </summary>
        [Fact]
        public async Task VerifyCodeAsync_WhenNetworkError_SetsNetworkErrorMessage()
        {
            // Arrange
            _mockAuthService.SetupException(new HttpRequestException());
            _viewModel.VerificationCode = "123456";

            // Act
            await _viewModel.VerifyCodeAsync.ExecuteAsync(null);

            // Assert
            Assert.True(_viewModel.HasError);
            Assert.Equal(ErrorMessages.NetworkError, _viewModel.ErrorMessage);
            _mockNavigationService.Verify(n => n.NavigateToAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Never());
        }

        /// <summary>
        /// Tests that ResendVerificationCodeAsync calls the authentication service with the correct phone number
        /// </summary>
        [Fact]
        public async Task ResendVerificationCodeAsync_CallsService()
        {
            // Act
            await _viewModel.ResendVerificationCodeAsync.ExecuteAsync(null);

            // Assert
            Assert.True(_mockAuthService.VerifyRequestVerificationCodeCalled(_testPhoneNumber));
        }

        /// <summary>
        /// Tests that ResendVerificationCodeAsync clears error message when successful
        /// </summary>
        [Fact]
        public async Task ResendVerificationCodeAsync_WhenSuccessful_ClearsError()
        {
            // Arrange
            _mockAuthService.SetupRequestVerificationCodeResult(true);
            _viewModel.ErrorMessage = "Some error";

            // Act
            await _viewModel.ResendVerificationCodeAsync.ExecuteAsync(null);

            // Assert
            Assert.False(_viewModel.HasError);
            Assert.True(string.IsNullOrEmpty(_viewModel.ErrorMessage));
        }

        /// <summary>
        /// Tests that ResendVerificationCodeAsync sets error message when request fails
        /// </summary>
        [Fact]
        public async Task ResendVerificationCodeAsync_WhenFailed_SetsErrorMessage()
        {
            // Arrange
            _mockAuthService.SetupRequestVerificationCodeResult(false);

            // Act
            await _viewModel.ResendVerificationCodeAsync.ExecuteAsync(null);

            // Assert
            Assert.True(_viewModel.HasError);
            Assert.Equal(ErrorMessages.AuthenticationFailed, _viewModel.ErrorMessage);
        }

        /// <summary>
        /// Tests that ResendVerificationCodeAsync sets network error message when a network exception occurs
        /// </summary>
        [Fact]
        public async Task ResendVerificationCodeAsync_WhenNetworkError_SetsNetworkErrorMessage()
        {
            // Arrange
            _mockAuthService.SetupException(new HttpRequestException());

            // Act
            await _viewModel.ResendVerificationCodeAsync.ExecuteAsync(null);

            // Assert
            Assert.True(_viewModel.HasError);
            Assert.Equal(ErrorMessages.NetworkError, _viewModel.ErrorMessage);
        }

        /// <summary>
        /// Tests that changing the verification code clears any existing error message
        /// </summary>
        [Fact]
        public void OnVerificationCodeChanged_ClearsError()
        {
            // Arrange
            _viewModel.ErrorMessage = "Some error";

            // Act
            _viewModel.VerificationCode = "123";

            // Assert
            Assert.False(_viewModel.HasError);
            Assert.True(string.IsNullOrEmpty(_viewModel.ErrorMessage));
        }

        /// <summary>
        /// Tests that changing the verification code resets the validation state
        /// </summary>
        [Fact]
        public void OnVerificationCodeChanged_ResetsValidationState()
        {
            // Arrange
            _viewModel.VerificationCode = "123456";
            _viewModel.ValidateVerificationCode.Execute(null);
            Assert.True(_viewModel.IsVerificationCodeValid);

            // Act
            _viewModel.VerificationCode = "1234";

            // Assert
            Assert.False(_viewModel.IsVerificationCodeValid);
        }
    }
}