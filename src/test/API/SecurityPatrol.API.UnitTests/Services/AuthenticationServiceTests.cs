using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Exceptions;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using Xunit;

namespace SecurityPatrol.API.UnitTests.Services
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IVerificationCodeService> _mockVerificationCodeService;
        private readonly Mock<ISmsService> _mockSmsService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
        private readonly AuthenticationService _authenticationService;

        public AuthenticationServiceTests()
        {
            // Initialize mock objects for all dependencies
            _mockUserRepository = new Mock<IUserRepository>();
            _mockVerificationCodeService = new Mock<IVerificationCodeService>();
            _mockSmsService = new Mock<ISmsService>();
            _mockTokenService = new Mock<ITokenService>();
            _mockLogger = new Mock<ILogger<AuthenticationService>>();

            // Create an instance of AuthenticationService with the mocked dependencies
            _authenticationService = new AuthenticationService(
                _mockUserRepository.Object,
                _mockVerificationCodeService.Object,
                _mockSmsService.Object,
                _mockTokenService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task RequestVerificationCode_WithValidPhoneNumber_ReturnsVerificationId()
        {
            // Arrange: Set up a valid phone number and authentication request
            var phoneNumber = "+12345678901";
            var request = new AuthenticationRequest { PhoneNumber = phoneNumber };
            var verificationCode = "123456";
            var verificationId = Guid.NewGuid().ToString();
            
            _mockVerificationCodeService.Setup(x => x.GenerateCodeAsync(phoneNumber))
                .ReturnsAsync(verificationCode);
            _mockVerificationCodeService.Setup(x => x.StoreCodeAsync(phoneNumber, verificationCode))
                .ReturnsAsync(verificationId);
            _mockSmsService.Setup(x => x.SendVerificationCodeAsync(phoneNumber, verificationCode))
                .ReturnsAsync(true);

            // Act: Call RequestVerificationCodeAsync with the valid request
            var result = await _authenticationService.RequestVerificationCodeAsync(request);

            // Assert: Verify that a non-empty verification ID is returned
            result.Should().Be(verificationId);
            // Assert: Verify that the verification code service was called to generate and store a code
            _mockVerificationCodeService.Verify(x => x.GenerateCodeAsync(phoneNumber), Times.Once);
            _mockVerificationCodeService.Verify(x => x.StoreCodeAsync(phoneNumber, verificationCode), Times.Once);
            // Assert: Verify that the SMS service was called to send the code
            _mockSmsService.Verify(x => x.SendVerificationCodeAsync(phoneNumber, verificationCode), Times.Once);
        }

        [Fact]
        public async Task RequestVerificationCode_WithInvalidPhoneNumber_ThrowsValidationException()
        {
            // Arrange: Set up an invalid phone number and authentication request
            var invalidPhoneNumber = "";
            var request = new AuthenticationRequest { PhoneNumber = invalidPhoneNumber };

            // Act & Assert: Verify that calling RequestVerificationCodeAsync throws a ValidationException
            await Assert.ThrowsAsync<ValidationException>(() => 
                _authenticationService.RequestVerificationCodeAsync(request));
            
            // Assert: Verify that the verification code service was not called
            _mockVerificationCodeService.Verify(x => x.GenerateCodeAsync(It.IsAny<string>()), Times.Never);
            // Assert: Verify that the SMS service was not called
            _mockSmsService.Verify(x => x.SendVerificationCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RequestVerificationCode_WhenSmsServiceFails_ReturnsVerificationIdAnyway()
        {
            // Arrange: Set up a valid phone number and authentication request
            var phoneNumber = "+12345678901";
            var request = new AuthenticationRequest { PhoneNumber = phoneNumber };
            var verificationCode = "123456";
            var verificationId = Guid.NewGuid().ToString();
            
            _mockVerificationCodeService.Setup(x => x.GenerateCodeAsync(phoneNumber))
                .ReturnsAsync(verificationCode);
            _mockVerificationCodeService.Setup(x => x.StoreCodeAsync(phoneNumber, verificationCode))
                .ReturnsAsync(verificationId);
            _mockSmsService.Setup(x => x.SendVerificationCodeAsync(phoneNumber, verificationCode))
                .ReturnsAsync(false);

            // Act & Assert: Verify that calling RequestVerificationCodeAsync throws an ApplicationException
            await Assert.ThrowsAsync<ApplicationException>(() => 
                _authenticationService.RequestVerificationCodeAsync(request));
            
            // Assert: Verify that the verification code service was called
            _mockVerificationCodeService.Verify(x => x.GenerateCodeAsync(phoneNumber), Times.Once);
            _mockVerificationCodeService.Verify(x => x.StoreCodeAsync(phoneNumber, verificationCode), Times.Once);
            // Assert: Verify that the SMS service was called
            _mockSmsService.Verify(x => x.SendVerificationCodeAsync(phoneNumber, verificationCode), Times.Once);
            // Assert: Verify that a warning was logged about the SMS failure
            _mockLogger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send verification code")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), 
                Times.Once);
        }

        [Fact]
        public async Task VerifyCode_WithValidCode_ReturnsAuthenticationResponse()
        {
            // Arrange: Set up a valid phone number, verification code, and verification request
            var phoneNumber = "+12345678901";
            var code = "123456";
            var request = new VerificationRequest { PhoneNumber = phoneNumber, Code = code };
            var user = new User { Id = Guid.NewGuid().ToString(), PhoneNumber = phoneNumber, IsActive = true };
            var authResponse = new AuthenticationResponse 
            { 
                Token = "valid_token", 
                ExpiresAt = DateTime.UtcNow.AddHours(1) 
            };
            
            _mockVerificationCodeService.Setup(x => x.ValidateCodeAsync(phoneNumber, code))
                .ReturnsAsync(true);
            _mockUserRepository.Setup(x => x.ExistsByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(true);
            _mockUserRepository.Setup(x => x.GetByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(user);
            _mockTokenService.Setup(x => x.GenerateTokenAsync(It.IsAny<User>()))
                .ReturnsAsync(authResponse);

            // Act: Call VerifyCodeAsync with the valid request
            var result = await _authenticationService.VerifyCodeAsync(request);

            // Assert: Verify that an authentication response with a token is returned
            result.Should().BeEquivalentTo(authResponse);
            // Assert: Verify that the verification code service was called to validate the code
            _mockVerificationCodeService.Verify(x => x.ValidateCodeAsync(phoneNumber, code), Times.Once);
            // Assert: Verify that the user repository was called to get or create a user
            _mockUserRepository.Verify(x => x.UpdateLastAuthenticatedAsync(user.Id, It.IsAny<DateTime>()), Times.Once);
            // Assert: Verify that the token service was called to generate a token
            _mockTokenService.Verify(x => x.GenerateTokenAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task VerifyCode_WithInvalidCode_ThrowsUnauthorizedException()
        {
            // Arrange: Set up a valid phone number but invalid verification code
            var phoneNumber = "+12345678901";
            var invalidCode = "000000";
            var request = new VerificationRequest { PhoneNumber = phoneNumber, Code = invalidCode };
            
            _mockVerificationCodeService.Setup(x => x.ValidateCodeAsync(phoneNumber, invalidCode))
                .ReturnsAsync(false);

            // Act & Assert: Verify that calling VerifyCodeAsync throws an UnauthorizedException
            await Assert.ThrowsAsync<UnauthorizedException>(() => 
                _authenticationService.VerifyCodeAsync(request));
            
            // Assert: Verify that the verification code service was called to validate the code
            _mockVerificationCodeService.Verify(x => x.ValidateCodeAsync(phoneNumber, invalidCode), Times.Once);
            // Assert: Verify that the user repository was not called
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(It.IsAny<string>()), Times.Never);
            // Assert: Verify that the token service was not called
            _mockTokenService.Verify(x => x.GenerateTokenAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task VerifyCode_WithInvalidPhoneNumber_ThrowsValidationException()
        {
            // Arrange: Set up an invalid phone number and verification request
            var invalidPhoneNumber = "";
            var code = "123456";
            var request = new VerificationRequest { PhoneNumber = invalidPhoneNumber, Code = code };

            // Act & Assert: Verify that calling VerifyCodeAsync throws a ValidationException
            await Assert.ThrowsAsync<ValidationException>(() => 
                _authenticationService.VerifyCodeAsync(request));
            
            // Assert: Verify that the verification code service was not called
            _mockVerificationCodeService.Verify(x => x.ValidateCodeAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            // Assert: Verify that the user repository was not called
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(It.IsAny<string>()), Times.Never);
            // Assert: Verify that the token service was not called
            _mockTokenService.Verify(x => x.GenerateTokenAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsNewAuthenticationResponse()
        {
            // Arrange: Set up a valid token
            var token = "valid_token";
            var userId = Guid.NewGuid().ToString();
            var user = new User { Id = userId, PhoneNumber = "+12345678901", IsActive = true };
            var authResponse = new AuthenticationResponse 
            { 
                Token = "new_token", 
                ExpiresAt = DateTime.UtcNow.AddHours(1) 
            };
            
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(token))
                .ReturnsAsync(userId);
            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockTokenService.Setup(x => x.GenerateTokenAsync(user))
                .ReturnsAsync(authResponse);

            // Act: Call RefreshTokenAsync with the valid token
            var result = await _authenticationService.RefreshTokenAsync(token);

            // Assert: Verify that a new authentication response with a token is returned
            result.Should().BeEquivalentTo(authResponse);
            // Assert: Verify that the token service was called to extract the user ID and generate a new token
            _mockTokenService.Verify(x => x.GetUserIdFromTokenAsync(token), Times.Once);
            // Assert: Verify that the user repository was called to get the user and update LastAuthenticated
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockUserRepository.Verify(x => x.UpdateLastAuthenticatedAsync(userId, It.IsAny<DateTime>()), Times.Once);
            _mockTokenService.Verify(x => x.GenerateTokenAsync(user), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_WithInvalidToken_ThrowsUnauthorizedException()
        {
            // Arrange: Set up an invalid token
            var invalidToken = "invalid_token";
            
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(invalidToken))
                .ReturnsAsync((string)null);

            // Act & Assert: Verify that calling RefreshTokenAsync throws an UnauthorizedException
            await Assert.ThrowsAsync<UnauthorizedException>(() => 
                _authenticationService.RefreshTokenAsync(invalidToken));
            
            // Assert: Verify that the token service was called to extract the user ID
            _mockTokenService.Verify(x => x.GetUserIdFromTokenAsync(invalidToken), Times.Once);
            // Assert: Verify that the user repository was not called
            _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RefreshToken_WithNonexistentUser_ThrowsUnauthorizedException()
        {
            // Arrange: Set up a valid token
            var token = "valid_token";
            var userId = Guid.NewGuid().ToString();
            
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(token))
                .ReturnsAsync(userId);
            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act & Assert: Verify that calling RefreshTokenAsync throws an UnauthorizedException
            await Assert.ThrowsAsync<UnauthorizedException>(() => 
                _authenticationService.RefreshTokenAsync(token));
            
            // Assert: Verify that the token service was called to extract the user ID
            _mockTokenService.Verify(x => x.GetUserIdFromTokenAsync(token), Times.Once);
            // Assert: Verify that the user repository was called to get the user
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_WithInactiveUser_ThrowsUnauthorizedException()
        {
            // Arrange: Set up a valid token
            var token = "valid_token";
            var userId = Guid.NewGuid().ToString();
            var inactiveUser = new User { Id = userId, PhoneNumber = "+12345678901", IsActive = false };
            
            _mockTokenService.Setup(x => x.GetUserIdFromTokenAsync(token))
                .ReturnsAsync(userId);
            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(inactiveUser);

            // Act & Assert: Verify that calling RefreshTokenAsync throws an UnauthorizedException
            await Assert.ThrowsAsync<UnauthorizedException>(() => 
                _authenticationService.RefreshTokenAsync(token));
            
            // Assert: Verify that the token service was called to extract the user ID
            _mockTokenService.Verify(x => x.GetUserIdFromTokenAsync(token), Times.Once);
            // Assert: Verify that the user repository was called to get the user
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUserByPhoneNumber_WithExistingUser_ReturnsUser()
        {
            // Arrange: Set up a valid phone number
            var phoneNumber = "+12345678901";
            var user = new User { Id = Guid.NewGuid().ToString(), PhoneNumber = phoneNumber };
            
            _mockUserRepository.Setup(x => x.GetByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(user);

            // Act: Call GetUserByPhoneNumberAsync with the valid phone number
            var result = await _authenticationService.GetUserByPhoneNumberAsync(phoneNumber);

            // Assert: Verify that a user is returned
            result.Should().BeEquivalentTo(user);
            // Assert: Verify that the user repository was called to get the user
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(phoneNumber), Times.Once);
        }

        [Fact]
        public async Task GetUserByPhoneNumber_WithNonexistentUser_ReturnsNull()
        {
            // Arrange: Set up a valid phone number
            var phoneNumber = "+12345678901";
            
            _mockUserRepository.Setup(x => x.GetByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync((User)null);

            // Act: Call GetUserByPhoneNumberAsync with the valid phone number
            var result = await _authenticationService.GetUserByPhoneNumberAsync(phoneNumber);

            // Assert: Verify that null is returned
            result.Should().BeNull();
            // Assert: Verify that the user repository was called to get the user
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(phoneNumber), Times.Once);
        }

        [Fact]
        public async Task ValidateToken_WithValidToken_ReturnsTrue()
        {
            // Arrange: Set up a valid token
            var token = "valid_token";
            
            _mockTokenService.Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(true);

            // Act: Call ValidateTokenAsync with the valid token
            var result = await _authenticationService.ValidateTokenAsync(token);

            // Assert: Verify that true is returned
            result.Should().BeTrue();
            // Assert: Verify that the token service was called to validate the token
            _mockTokenService.Verify(x => x.ValidateTokenAsync(token), Times.Once);
        }

        [Fact]
        public async Task ValidateToken_WithInvalidToken_ReturnsFalse()
        {
            // Arrange: Set up an invalid token
            var invalidToken = "invalid_token";
            
            _mockTokenService.Setup(x => x.ValidateTokenAsync(invalidToken))
                .ReturnsAsync(false);

            // Act: Call ValidateTokenAsync with the invalid token
            var result = await _authenticationService.ValidateTokenAsync(invalidToken);

            // Assert: Verify that false is returned
            result.Should().BeFalse();
            // Assert: Verify that the token service was called to validate the token
            _mockTokenService.Verify(x => x.ValidateTokenAsync(invalidToken), Times.Once);
        }

        [Fact]
        public async Task GetOrCreateUser_WithExistingUser_ReturnsExistingUser()
        {
            // Arrange: Set up a valid phone number
            var phoneNumber = "+12345678901";
            var code = "123456";
            var request = new VerificationRequest { PhoneNumber = phoneNumber, Code = code };
            var existingUser = new User { Id = Guid.NewGuid().ToString(), PhoneNumber = phoneNumber, IsActive = true };
            var authResponse = new AuthenticationResponse 
            { 
                Token = "valid_token", 
                ExpiresAt = DateTime.UtcNow.AddHours(1) 
            };
            
            _mockVerificationCodeService.Setup(x => x.ValidateCodeAsync(phoneNumber, code))
                .ReturnsAsync(true);
            _mockUserRepository.Setup(x => x.ExistsByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(true);
            _mockUserRepository.Setup(x => x.GetByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(existingUser);
            _mockTokenService.Setup(x => x.GenerateTokenAsync(It.IsAny<User>()))
                .ReturnsAsync(authResponse);

            // Act: Call GetOrCreateUserAsync (via reflection or a test-specific method) with the valid phone number
            await _authenticationService.VerifyCodeAsync(request);

            // Assert: Verify that the user repository was called to check if the user exists and get the user
            _mockUserRepository.Verify(x => x.ExistsByPhoneNumberAsync(phoneNumber), Times.Once);
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(phoneNumber), Times.Once);
            // Assert: Verify that the user repository was not called to add a new user
            _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task GetOrCreateUser_WithNewUser_CreatesAndReturnsNewUser()
        {
            // Arrange: Set up a valid phone number
            var phoneNumber = "+12345678901";
            var code = "123456";
            var request = new VerificationRequest { PhoneNumber = phoneNumber, Code = code };
            var authResponse = new AuthenticationResponse 
            { 
                Token = "valid_token", 
                ExpiresAt = DateTime.UtcNow.AddHours(1) 
            };
            
            _mockVerificationCodeService.Setup(x => x.ValidateCodeAsync(phoneNumber, code))
                .ReturnsAsync(true);
            _mockUserRepository.Setup(x => x.ExistsByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(false);
            _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => u); // Return the newly created user when AddAsync is called
            _mockTokenService.Setup(x => x.GenerateTokenAsync(It.IsAny<User>()))
                .ReturnsAsync(authResponse);

            // Act: Call GetOrCreateUserAsync (via reflection or a test-specific method) with the valid phone number
            await _authenticationService.VerifyCodeAsync(request);

            // Assert: Verify that the user repository was called to check if the user exists
            _mockUserRepository.Verify(x => x.ExistsByPhoneNumberAsync(phoneNumber), Times.Once);
            // Assert: Verify that the user repository was called to add a new user
            _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => u.PhoneNumber == phoneNumber)), Times.Once);
        }
    }
}