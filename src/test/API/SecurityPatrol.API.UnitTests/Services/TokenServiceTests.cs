using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Constants;
using SecurityPatrol.API.UnitTests.Setup;

namespace SecurityPatrol.API.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the TokenService class to verify token generation, validation, refresh, and user information extraction functionality.
    /// </summary>
    public class TokenServiceTests : TestBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        private readonly TokenService _tokenService;
        private readonly User _testUser;

        /// <summary>
        /// Initializes a new instance of the TokenServiceTests class with mocked dependencies
        /// </summary>
        public TokenServiceTests()
        {
            // Set up Configuration mock with JWT settings
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["JWT:Issuer"]).Returns("test-issuer");
            configMock.Setup(c => c["JWT:Audience"]).Returns("test-audience");
            configMock.Setup(c => c["JWT:SecretKey"]).Returns("very-secure-test-secret-key-with-sufficient-length");
            configMock.Setup(c => c["JWT:ExpirationMinutes"]).Returns("60");
            
            _configuration = configMock.Object;
            
            // Set up Logger mock
            _logger = Mock.Of<ILogger<TokenService>>();
            
            // Initialize the service
            _tokenService = new TokenService(_configuration, _logger);
            
            // Create a test user
            _testUser = new User
            {
                Id = TestConstants.TestUserId,
                PhoneNumber = TestConstants.TestPhoneNumber
            };
        }

        /// <summary>
        /// Tests that GenerateTokenAsync returns a valid token when provided with a valid user
        /// </summary>
        [Fact]
        public async Task GenerateTokenAsync_WithValidUser_ReturnsValidToken()
        {
            // Act
            var result = await _tokenService.GenerateTokenAsync(_testUser);
            
            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        /// <summary>
        /// Tests that GenerateTokenAsync throws ArgumentNullException when provided with a null user
        /// </summary>
        [Fact]
        public async Task GenerateTokenAsync_WithNullUser_ThrowsArgumentNullException()
        {
            // Act & Assert
            await AssertExceptionAsync<ArgumentNullException>(() => _tokenService.GenerateTokenAsync(null));
        }

        /// <summary>
        /// Tests that ValidateTokenAsync returns true when provided with a valid token
        /// </summary>
        [Fact]
        public async Task ValidateTokenAsync_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var tokenResponse = await _tokenService.GenerateTokenAsync(_testUser);
            
            // Act
            var result = await _tokenService.ValidateTokenAsync(tokenResponse.Token);
            
            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ValidateTokenAsync returns false when provided with an invalid token
        /// </summary>
        [Fact]
        public async Task ValidateTokenAsync_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var invalidToken = "invalid.token.string";
            
            // Act
            var result = await _tokenService.ValidateTokenAsync(invalidToken);
            
            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ValidateTokenAsync returns false when provided with a null token
        /// </summary>
        [Fact]
        public async Task ValidateTokenAsync_WithNullToken_ReturnsFalse()
        {
            // Act
            var result = await _tokenService.ValidateTokenAsync(null);
            
            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that RefreshTokenAsync returns a new token when provided with a valid token
        /// </summary>
        [Fact]
        public async Task RefreshTokenAsync_WithValidToken_ReturnsNewToken()
        {
            // Arrange
            var tokenResponse = await _tokenService.GenerateTokenAsync(_testUser);
            
            // Act
            var result = await _tokenService.RefreshTokenAsync(tokenResponse.Token);
            
            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            result.Token.Should().NotBe(tokenResponse.Token);
        }

        /// <summary>
        /// Tests that RefreshTokenAsync returns null when provided with an invalid token
        /// </summary>
        [Fact]
        public async Task RefreshTokenAsync_WithInvalidToken_ReturnsNull()
        {
            // Arrange
            var invalidToken = "invalid.token.string";
            
            // Act
            var result = await _tokenService.RefreshTokenAsync(invalidToken);
            
            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that RefreshTokenAsync returns null when provided with a null token
        /// </summary>
        [Fact]
        public async Task RefreshTokenAsync_WithNullToken_ReturnsNull()
        {
            // Act
            var result = await _tokenService.RefreshTokenAsync(null);
            
            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetPrincipalFromTokenAsync returns a valid ClaimsPrincipal when provided with a valid token
        /// </summary>
        [Fact]
        public async Task GetPrincipalFromTokenAsync_WithValidToken_ReturnsPrincipal()
        {
            // Arrange
            var tokenResponse = await _tokenService.GenerateTokenAsync(_testUser);
            
            // Act
            var result = await _tokenService.GetPrincipalFromTokenAsync(tokenResponse.Token);
            
            // Assert
            result.Should().NotBeNull();
            result.Identity.Should().NotBeNull();
            result.Identity.IsAuthenticated.Should().BeTrue();
            result.HasClaim(c => c.Type == ClaimTypes.UserId && c.Value == _testUser.Id).Should().BeTrue();
            result.HasClaim(c => c.Type == ClaimTypes.PhoneNumber && c.Value == _testUser.PhoneNumber).Should().BeTrue();
            result.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == Roles.SecurityPersonnel).Should().BeTrue();
        }

        /// <summary>
        /// Tests that GetPrincipalFromTokenAsync returns null when provided with an invalid token
        /// </summary>
        [Fact]
        public async Task GetPrincipalFromTokenAsync_WithInvalidToken_ReturnsNull()
        {
            // Arrange
            var invalidToken = "invalid.token.string";
            
            // Act
            var result = await _tokenService.GetPrincipalFromTokenAsync(invalidToken);
            
            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetPrincipalFromTokenAsync returns null when provided with a null token
        /// </summary>
        [Fact]
        public async Task GetPrincipalFromTokenAsync_WithNullToken_ReturnsNull()
        {
            // Act
            var result = await _tokenService.GetPrincipalFromTokenAsync(null);
            
            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetUserIdFromTokenAsync returns the correct user ID when provided with a valid token
        /// </summary>
        [Fact]
        public async Task GetUserIdFromTokenAsync_WithValidToken_ReturnsUserId()
        {
            // Arrange
            var tokenResponse = await _tokenService.GenerateTokenAsync(_testUser);
            
            // Act
            var result = await _tokenService.GetUserIdFromTokenAsync(tokenResponse.Token);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Be(_testUser.Id);
        }

        /// <summary>
        /// Tests that GetUserIdFromTokenAsync returns null when provided with an invalid token
        /// </summary>
        [Fact]
        public async Task GetUserIdFromTokenAsync_WithInvalidToken_ReturnsNull()
        {
            // Arrange
            var invalidToken = "invalid.token.string";
            
            // Act
            var result = await _tokenService.GetUserIdFromTokenAsync(invalidToken);
            
            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetUserIdFromTokenAsync returns null when provided with a null token
        /// </summary>
        [Fact]
        public async Task GetUserIdFromTokenAsync_WithNullToken_ReturnsNull()
        {
            // Act
            var result = await _tokenService.GetUserIdFromTokenAsync(null);
            
            // Assert
            result.Should().BeNull();
        }
    }
}