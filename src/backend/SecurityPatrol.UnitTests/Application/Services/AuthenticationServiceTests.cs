using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.UnitTests.Helpers;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Exceptions;
using System.Reflection;

namespace SecurityPatrol.UnitTests.Application.Services
{
    /// <summary>
    /// Contains unit tests for the AuthenticationService class to verify its functionality
    /// </summary>
    public class AuthenticationServiceTests : TestBase
    {
        private readonly AuthenticationService _authenticationService;
        private readonly Mock<ILogger<AuthenticationService>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the AuthenticationServiceTests class with the required dependencies
        /// </summary>
        public AuthenticationServiceTests()
        {
            _mockLogger = CreateMockLogger<AuthenticationService>();
            _authenticationService = new AuthenticationService(
                MockUserRepository.Object,
                MockVerificationCodeService.Object,
                MockSmsService.Object,
                MockTokenService.Object,
                _mockLogger.Object);
        }

        /// <summary>
        /// Tests that RequestVerificationCodeAsync returns a verification ID when provided with a valid phone number
        /// </summary>
        [Fact]
        public async Task RequestVerificationCode_WithValidPhoneNumber_ReturnsVerificationId()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = "+15551234567" };
            var verificationCode = "123456";
            var verificationId = "verification-123";
            
            MockVerificationCodeService.Setup(s => s.GenerateCodeAsync(request.PhoneNumber))
                .ReturnsAsync(verificationCode);
            
            MockVerificationCodeService.Setup(s => s.StoreCodeAsync(request.PhoneNumber, verificationCode))
                .ReturnsAsync(verificationId);
            
            MockSmsService.Setup(s => s.SendVerificationCodeAsync(request.PhoneNumber, verificationCode))
                .ReturnsAsync(true);

            // Act
            var result = await _authenticationService.RequestVerificationCodeAsync(request);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Be(verificationId);
            
            MockVerificationCodeService.Verify(s => s.GenerateCodeAsync(request.PhoneNumber), Times.Once);
            MockVerificationCodeService.Verify(s => s.StoreCodeAsync(request.PhoneNumber, verificationCode), Times.Once);
            MockSmsService.Verify(s => s.SendVerificationCodeAsync(request.PhoneNumber, verificationCode), Times.Once);
        }

        /// <summary>
        /// Tests that RequestVerificationCodeAsync throws a ValidationException when provided with a null request
        /// </summary>
        [Fact]
        public async Task RequestVerificationCode_WithNullRequest_ThrowsValidationException()
        {
            // Arrange & Act & Assert
            await AssertExceptionAsync<ArgumentNullException>(() => 
                _authenticationService.RequestVerificationCodeAsync(null));
        }

        /// <summary>
        /// Tests that RequestVerificationCodeAsync throws a ValidationException when provided with an invalid phone number
        /// </summary>
        [Fact]
        public async Task RequestVerificationCode_WithInvalidPhoneNumber_ThrowsValidationException()
        {
            // Arrange
            var request = new AuthenticationRequest { PhoneNumber = "" };

            // Act & Assert
            await AssertExceptionAsync<ValidationException>(() => 
                _authenticationService.RequestVerificationCodeAsync(request));
        }

        /// <summary>
        /// Tests that VerifyCodeAsync returns an authentication response with a token when provided with a valid verification request
        /// </summary>
        [Fact]
        public async Task VerifyCode_WithValidRequest_ReturnsAuthenticationResponse()
        {
            // Arrange
            var request = new VerificationRequest 
            { 
                PhoneNumber = "+15551234567", 
                Code = "123456" 
            };
            
            var user = TestData.GetTestUserByPhoneNumber(request.PhoneNumber);
            var authResponse = new AuthenticationResponse 
            { 
                Token = "test-jwt-token", 
                ExpiresAt = DateTime.UtcNow.AddHours(1) 
            };
            
            MockVerificationCodeService.Setup(s => s.ValidateCodeAsync(request.PhoneNumber, request.Code))
                .ReturnsAsync(true);
            
            MockUserRepository.Setup(r => r.ExistsByPhoneNumberAsync(request.PhoneNumber))
                .ReturnsAsync(true);
            
            MockUserRepository.Setup(r => r.GetByPhoneNumberAsync(request.PhoneNumber))
                .ReturnsAsync(user);
            
            MockTokenService.Setup(s => s.GenerateTokenAsync(It.IsAny<User>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _authenticationService.VerifyCodeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be(authResponse.Token);
            result.ExpiresAt.Should().Be(authResponse.ExpiresAt);
            
            MockVerificationCodeService.Verify(s => s.ValidateCodeAsync(request.PhoneNumber, request.Code), Times.Once);
            MockUserRepository.Verify(r => r.ExistsByPhoneNumberAsync(request.PhoneNumber), Times.Once);
            MockUserRepository.Verify(r => r.GetByPhoneNumberAsync(request.PhoneNumber), Times.Once);
            MockTokenService.Verify(s => s.GenerateTokenAsync(It.IsAny<User>()), Times.Once);
        }

        /// <summary>
        /// Tests that VerifyCodeAsync throws a ValidationException when provided with a null request
        /// </summary>
        [Fact]
        public async Task VerifyCode_WithNullRequest_ThrowsValidationException()
        {
            // Arrange & Act & Assert
            await AssertExceptionAsync<ArgumentNullException>(() => 
                _authenticationService.VerifyCodeAsync(null));
        }

        /// <summary>
        /// Tests that VerifyCodeAsync throws an UnauthorizedException when provided with an invalid verification code
        /// </summary>
        [Fact]
        public async Task VerifyCode_WithInvalidCode_ThrowsUnauthorizedException()
        {
            // Arrange
            var request = new VerificationRequest 
            { 
                PhoneNumber = "+15551234567", 
                Code = "invalid" 
            };
            
            MockVerificationCodeService.Setup(s => s.ValidateCodeAsync(request.PhoneNumber, request.Code))
                .ReturnsAsync(false);

            // Act & Assert
            await AssertExceptionAsync<UnauthorizedException>(() => 
                _authenticationService.VerifyCodeAsync(request));
        }

        /// <summary>
        /// Tests that RefreshTokenAsync returns a new authentication response with a refreshed token when provided with a valid token
        /// </summary>
        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsNewAuthenticationResponse()
        {
            // Arrange
            var token = "valid-token";
            var userId = "user1";
            var user = TestData.GetTestUserById(userId);
            var refreshedResponse = new AuthenticationResponse 
            { 
                Token = "refreshed-jwt-token", 
                ExpiresAt = DateTime.UtcNow.AddHours(1) 
            };
            
            MockTokenService.Setup(s => s.GetUserIdFromTokenAsync(token))
                .ReturnsAsync(userId);
            
            MockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
            
            MockTokenService.Setup(s => s.GenerateTokenAsync(user))
                .ReturnsAsync(refreshedResponse);

            // Act
            var result = await _authenticationService.RefreshTokenAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be(refreshedResponse.Token);
            result.ExpiresAt.Should().Be(refreshedResponse.ExpiresAt);
            
            MockTokenService.Verify(s => s.GetUserIdFromTokenAsync(token), Times.Once);
            MockUserRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
            MockTokenService.Verify(s => s.GenerateTokenAsync(user), Times.Once);
        }

        /// <summary>
        /// Tests that RefreshTokenAsync throws a ValidationException when provided with a null token
        /// </summary>
        [Fact]
        public async Task RefreshToken_WithNullToken_ThrowsValidationException()
        {
            // Arrange & Act & Assert
            await AssertExceptionAsync<ArgumentNullException>(() => 
                _authenticationService.RefreshTokenAsync(null));
        }

        /// <summary>
        /// Tests that RefreshTokenAsync throws an UnauthorizedException when provided with an invalid token
        /// </summary>
        [Fact]
        public async Task RefreshToken_WithInvalidToken_ThrowsUnauthorizedException()
        {
            // Arrange
            var token = "invalid-token";
            
            MockTokenService.Setup(s => s.GetUserIdFromTokenAsync(token))
                .ReturnsAsync((string)null);

            // Act & Assert
            await AssertExceptionAsync<UnauthorizedException>(() => 
                _authenticationService.RefreshTokenAsync(token));
        }

        /// <summary>
        /// Tests that GetUserByPhoneNumberAsync returns a user when provided with a valid phone number that exists in the repository
        /// </summary>
        [Fact]
        public async Task GetUserByPhoneNumber_WithValidPhoneNumber_ReturnsUser()
        {
            // Arrange
            var phoneNumber = "+15551234567";
            var user = TestData.GetTestUserByPhoneNumber(phoneNumber);
            
            MockUserRepository.Setup(r => r.GetByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(user);

            // Act
            var result = await _authenticationService.GetUserByPhoneNumberAsync(phoneNumber);

            // Assert
            result.Should().NotBeNull();
            result.PhoneNumber.Should().Be(phoneNumber);
            
            MockUserRepository.Verify(r => r.GetByPhoneNumberAsync(phoneNumber), Times.Once);
        }

        /// <summary>
        /// Tests that GetUserByPhoneNumberAsync returns null when provided with a phone number that doesn't exist in the repository
        /// </summary>
        [Fact]
        public async Task GetUserByPhoneNumber_WithNonExistentPhoneNumber_ReturnsNull()
        {
            // Arrange
            var phoneNumber = "+15559999999";
            
            MockUserRepository.Setup(r => r.GetByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync((User)null);

            // Act
            var result = await _authenticationService.GetUserByPhoneNumberAsync(phoneNumber);

            // Assert
            result.Should().BeNull();
            
            MockUserRepository.Verify(r => r.GetByPhoneNumberAsync(phoneNumber), Times.Once);
        }

        /// <summary>
        /// Tests that ValidateTokenAsync returns true when provided with a valid token
        /// </summary>
        [Fact]
        public async Task ValidateToken_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var token = "valid-token";
            
            MockTokenService.Setup(s => s.ValidateTokenAsync(token))
                .ReturnsAsync(true);

            // Act
            var result = await _authenticationService.ValidateTokenAsync(token);

            // Assert
            result.Should().BeTrue();
            
            MockTokenService.Verify(s => s.ValidateTokenAsync(token), Times.Once);
        }

        /// <summary>
        /// Tests that ValidateTokenAsync returns false when provided with an invalid token
        /// </summary>
        [Fact]
        public async Task ValidateToken_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var token = "invalid-token";
            
            MockTokenService.Setup(s => s.ValidateTokenAsync(token))
                .ReturnsAsync(false);

            // Act
            var result = await _authenticationService.ValidateTokenAsync(token);

            // Assert
            result.Should().BeFalse();
            
            MockTokenService.Verify(s => s.ValidateTokenAsync(token), Times.Once);
        }

        /// <summary>
        /// Tests that the private GetOrCreateUserAsync method returns an existing user when the user exists in the repository
        /// </summary>
        [Fact]
        public async Task GetOrCreateUser_WithExistingUser_ReturnsExistingUser()
        {
            // Arrange
            var phoneNumber = "+15551234567";
            var existingUser = TestData.GetTestUserByPhoneNumber(phoneNumber);
            
            MockUserRepository.Setup(r => r.ExistsByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(true);
            
            MockUserRepository.Setup(r => r.GetByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(existingUser);

            // Act - Use reflection to access the private method
            MethodInfo methodInfo = typeof(AuthenticationService).GetMethod("GetOrCreateUserAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            var result = await (Task<User>)methodInfo.Invoke(_authenticationService, new object[] { phoneNumber });

            // Assert
            result.Should().NotBeNull();
            result.PhoneNumber.Should().Be(phoneNumber);
            
            MockUserRepository.Verify(r => r.ExistsByPhoneNumberAsync(phoneNumber), Times.Once);
            MockUserRepository.Verify(r => r.GetByPhoneNumberAsync(phoneNumber), Times.Once);
            MockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        /// <summary>
        /// Tests that the private GetOrCreateUserAsync method creates and returns a new user when the user doesn't exist in the repository
        /// </summary>
        [Fact]
        public async Task GetOrCreateUser_WithNewUser_CreatesAndReturnsNewUser()
        {
            // Arrange
            var phoneNumber = "+15559999999";
            
            MockUserRepository.Setup(r => r.ExistsByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(false);
            
            MockUserRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => { u.Id = Guid.NewGuid().ToString(); return u; });

            // Act - Use reflection to access the private method
            MethodInfo methodInfo = typeof(AuthenticationService).GetMethod("GetOrCreateUserAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            var result = await (Task<User>)methodInfo.Invoke(_authenticationService, new object[] { phoneNumber });

            // Assert
            result.Should().NotBeNull();
            result.PhoneNumber.Should().Be(phoneNumber);
            result.IsActive.Should().BeTrue();
            
            MockUserRepository.Verify(r => r.ExistsByPhoneNumberAsync(phoneNumber), Times.Once);
            MockUserRepository.Verify(r => r.GetByPhoneNumberAsync(phoneNumber), Times.Never);
            MockUserRepository.Verify(r => r.AddAsync(It.Is<User>(u => u.PhoneNumber == phoneNumber)), Times.Once);
        }
    }
}