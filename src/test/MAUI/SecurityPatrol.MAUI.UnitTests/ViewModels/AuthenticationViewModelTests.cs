using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;
using FluentAssertions;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.ViewModels;
using SecurityPatrol.Constants;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UnitTests.ViewModels
{
    /// <summary>
    /// Tests for the AuthenticationViewModel class, verifying verification code validation,
    /// authentication completion, and navigation after successful authentication
    /// </summary>
    public class AuthenticationViewModelTests : TestBase
    {
        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        private void Setup()
        {
            // Reset mock objects
            MockAuthService.Reset();
            MockNavigationService.Reset();

            // Setup navigation service to return test phone number
            MockNavigationService.Setup(n => n.GetRouteParameter(NavigationConstants.ParamPhoneNumber))
                .Returns(TestConstants.TestPhoneNumber);

            // Default auth service setup
            SetupMockAuthService();
        }

        /// <summary>
        /// Creates an instance of AuthenticationViewModel for testing
        /// </summary>
        private AuthenticationViewModel CreateViewModel()
        {
            return new AuthenticationViewModel(
                MockNavigationService.Object,
                MockAuthService.Object,
                MockAuthService.Object);
        }

        [Fact]
        public async Task InitializeAsync_WhenUserIsAuthenticated_ShouldNavigateToMainPage()
        {
            // Arrange
            Setup();
            SetupMockAuthenticationState(true);
            var viewModel = CreateViewModel();

            // Act
            await viewModel.InitializeAsync();

            // Assert
            MockNavigationService.Verify(n => n.NavigateToAsync(NavigationConstants.MainPage), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenUserIsNotAuthenticated_ShouldNotNavigateToMainPage()
        {
            // Arrange
            Setup();
            SetupMockAuthenticationState(false);
            var viewModel = CreateViewModel();

            // Act
            await viewModel.InitializeAsync();

            // Assert
            MockNavigationService.Verify(n => n.NavigateToAsync(NavigationConstants.MainPage), Times.Never);
        }

        [Fact]
        public async Task InitializeAsync_WhenPhoneNumberIsNull_ShouldNavigateBack()
        {
            // Arrange
            Setup();
            SetupMockAuthenticationState(false);
            MockNavigationService.Setup(n => n.GetRouteParameter(NavigationConstants.ParamPhoneNumber))
                .Returns(null);
            var viewModel = CreateViewModel();

            // Act
            await viewModel.InitializeAsync();

            // Assert
            MockNavigationService.Verify(n => n.NavigateBackAsync(), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenPhoneNumberIsProvided_ShouldSetPhoneNumber()
        {
            // Arrange
            Setup();
            SetupMockAuthenticationState(false);
            var viewModel = CreateViewModel();

            // Act
            await viewModel.InitializeAsync();

            // Assert
            viewModel.PhoneNumber.Should().Be(TestConstants.TestPhoneNumber);
        }

        [Fact]
        public void ValidateVerificationCode_WithValidCode_ShouldReturnTrue()
        {
            // Arrange
            Setup();
            var viewModel = CreateViewModel();
            viewModel.VerificationCode = TestConstants.TestVerificationCode; // Valid 6-digit code

            // Act
            var result = viewModel.ValidateVerificationCode();

            // Assert
            result.Should().BeTrue();
            viewModel.IsVerificationCodeValid.Should().BeTrue();
            viewModel.HasError.Should().BeFalse();
        }

        [Fact]
        public void ValidateVerificationCode_WithInvalidCode_ShouldReturnFalse()
        {
            // Arrange
            Setup();
            var viewModel = CreateViewModel();
            viewModel.VerificationCode = "12345"; // Too short

            // Act
            var result = viewModel.ValidateVerificationCode();

            // Assert
            result.Should().BeFalse();
            viewModel.IsVerificationCodeValid.Should().BeFalse();
            viewModel.HasError.Should().BeTrue();
            viewModel.ErrorMessage.Should().Be(ErrorMessages.InvalidVerificationCode);
        }

        [Fact]
        public void ValidateVerificationCode_WithEmptyCode_ShouldReturnFalse()
        {
            // Arrange
            Setup();
            var viewModel = CreateViewModel();
            viewModel.VerificationCode = "";

            // Act
            var result = viewModel.ValidateVerificationCode();

            // Assert
            result.Should().BeFalse();
            viewModel.IsVerificationCodeValid.Should().BeFalse();
            viewModel.HasError.Should().BeTrue();
            viewModel.ErrorMessage.Should().Be(ErrorMessages.InvalidVerificationCode);
        }

        [Fact]
        public async Task VerifyCodeAsync_WithInvalidCode_ShouldNotCallAuthService()
        {
            // Arrange
            Setup();
            var viewModel = CreateViewModel();
            viewModel.VerificationCode = "12345"; // Invalid code

            // Act
            await viewModel.VerifyCodeAsync();

            // Assert
            MockAuthService.Verify(a => a.VerifyCode(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyCodeAsync_WithValidCode_ShouldCallAuthService()
        {
            // Arrange
            Setup();
            var viewModel = CreateViewModel();
            viewModel.VerificationCode = TestConstants.TestVerificationCode;

            // Act
            await viewModel.VerifyCodeAsync();

            // Assert
            MockAuthService.Verify(a => a.VerifyCode(TestConstants.TestVerificationCode), Times.Once);
        }

        [Fact]
        public async Task VerifyCodeAsync_WhenSuccessful_ShouldNavigateToMainPage()
        {
            // Arrange
            Setup();
            MockAuthService.Setup(a => a.VerifyCode(It.IsAny<string>())).ReturnsAsync(true);
            var viewModel = CreateViewModel();
            viewModel.VerificationCode = TestConstants.TestVerificationCode;

            // Act
            await viewModel.VerifyCodeAsync();

            // Assert
            MockNavigationService.Verify(n => n.NavigateToAsync(NavigationConstants.MainPage), Times.Once);
        }

        [Fact]
        public async Task VerifyCodeAsync_WhenFailed_ShouldSetErrorMessage()
        {
            // Arrange
            Setup();
            SetupMockAuthenticationFailure();
            var viewModel = CreateViewModel();
            viewModel.VerificationCode = TestConstants.TestVerificationCode;

            // Act
            await viewModel.VerifyCodeAsync();

            // Assert
            viewModel.HasError.Should().BeTrue();
            viewModel.ErrorMessage.Should().Be(ErrorMessages.AuthenticationFailed);
        }

        [Fact]
        public async Task VerifyCodeAsync_WhenExceptionOccurs_ShouldSetErrorMessage()
        {
            // Arrange
            Setup();
            SetupMockAuthenticationException();
            var viewModel = CreateViewModel();
            viewModel.VerificationCode = TestConstants.TestVerificationCode;

            // Act
            await viewModel.VerifyCodeAsync();

            // Assert
            viewModel.HasError.Should().BeTrue();
            viewModel.ErrorMessage.Should().Be(ErrorMessages.NetworkError);
        }

        [Fact]
        public async Task ResendVerificationCodeAsync_WithValidPhoneNumber_ShouldCallAuthService()
        {
            // Arrange
            Setup();
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync(); // Set phone number

            // Act
            await viewModel.ResendVerificationCodeAsync();

            // Assert
            MockAuthService.Verify(a => a.RequestVerificationCode(TestConstants.TestPhoneNumber), Times.Once);
        }

        [Fact]
        public async Task ResendVerificationCodeAsync_WhenSuccessful_ShouldShowSuccessMessage()
        {
            // Arrange
            Setup();
            MockAuthService.Setup(a => a.RequestVerificationCode(It.IsAny<string>())).ReturnsAsync(true);
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Act
            await viewModel.ResendVerificationCodeAsync();

            // Assert
            viewModel.HasError.Should().BeFalse();
        }

        [Fact]
        public async Task ResendVerificationCodeAsync_WhenFailed_ShouldSetErrorMessage()
        {
            // Arrange
            Setup();
            SetupMockAuthenticationFailure();
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Act
            await viewModel.ResendVerificationCodeAsync();

            // Assert
            viewModel.HasError.Should().BeTrue();
        }

        [Fact]
        public async Task ResendVerificationCodeAsync_WhenExceptionOccurs_ShouldSetErrorMessage()
        {
            // Arrange
            Setup();
            SetupMockAuthenticationException();
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            // Act
            await viewModel.ResendVerificationCodeAsync();

            // Assert
            viewModel.HasError.Should().BeTrue();
            viewModel.ErrorMessage.Should().Be(ErrorMessages.NetworkError);
        }

        [Fact]
        public void OnVerificationCodeChanged_ShouldClearErrorAndResetValidation()
        {
            // Arrange
            Setup();
            var viewModel = CreateViewModel();
            viewModel.VerificationCode = "12345"; // Invalid code
            viewModel.ValidateVerificationCode(); // Trigger error
            viewModel.HasError.Should().BeTrue();
            viewModel.IsVerificationCodeValid.Should().BeFalse();

            // Act
            viewModel.VerificationCode = "1"; // Change code

            // Assert
            viewModel.HasError.Should().BeFalse();
            viewModel.IsVerificationCodeValid.Should().BeFalse();
        }
    }
}