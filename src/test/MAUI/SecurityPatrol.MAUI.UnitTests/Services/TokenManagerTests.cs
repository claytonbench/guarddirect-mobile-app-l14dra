using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Helpers;
using SecurityPatrol.Constants;

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the TokenManager class to verify its functionality for secure token management.
    /// </summary>
    public class TokenManagerTests : TestBase
    {
        private readonly TokenManager _tokenManager;
        private readonly Mock<ILogger<TokenManager>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the TokenManagerTests class with required test setup
        /// </summary>
        public TokenManagerTests()
        {
            _mockLogger = new Mock<ILogger<TokenManager>>();
            _tokenManager = new TokenManager(_mockLogger.Object);
        }

        /// <summary>
        /// Performs cleanup after test execution
        /// </summary>
        public override void Dispose()
        {
            base.Cleanup();
            _tokenManager.ClearToken().GetAwaiter().GetResult();
            _mockLogger.Reset();
        }

        [Fact]
        public async Task StoreToken_ValidToken_StoresSuccessfully()
        {
            // Arrange
            string testToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            
            // Mock the SecurityHelper to verify calls
            var securityHelperMock = new Mock<Action<string, string>>();
            SecurityHelper.SaveToSecureStorage = async (key, value) => 
            {
                securityHelperMock.Object(key, value);
                await Task.CompletedTask;
            };

            // Act
            await _tokenManager.StoreToken(testToken);

            // Assert
            securityHelperMock.Verify(m => m("auth_token", testToken), Times.Once);
            securityHelperMock.Verify(m => m("auth_token_expiry", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task StoreToken_NullOrEmptyToken_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _tokenManager.StoreToken(null));
            await Assert.ThrowsAsync<ArgumentException>(() => _tokenManager.StoreToken(string.Empty));
        }

        [Fact]
        public async Task StoreToken_SecurityHelperThrowsException_PropagatesException()
        {
            // Arrange
            string testToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            
            // Mock the SecurityHelper to throw an exception
            SecurityHelper.SaveToSecureStorage = async (key, value) => 
            {
                throw new Exception("Test security helper exception");
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _tokenManager.StoreToken(testToken));
            exception.Message.Should().Be("Test security helper exception");
        }

        [Fact]
        public async Task RetrieveToken_TokenExists_ReturnsToken()
        {
            // Arrange
            string testToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            
            // Mock the SecurityHelper to return the test token
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                if (key == "auth_token") return testToken;
                return null;
            };

            // Act
            var result = await _tokenManager.RetrieveToken();

            // Assert
            result.Should().Be(testToken);
        }

        [Fact]
        public async Task RetrieveToken_TokenDoesNotExist_ReturnsNull()
        {
            // Arrange
            SecurityHelper.GetFromSecureStorage = async (key) => null;

            // Act
            var result = await _tokenManager.RetrieveToken();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task RetrieveToken_SecurityHelperThrowsException_ReturnsNull()
        {
            // Arrange
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                throw new Exception("Test security helper exception");
            };

            // Act
            var result = await _tokenManager.RetrieveToken();

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ClearToken_TokenExists_RemovesToken()
        {
            // Arrange
            var securityHelperMock = new Mock<Action<string>>();
            SecurityHelper.RemoveFromSecureStorage = async (key) => 
            {
                securityHelperMock.Object(key);
                await Task.CompletedTask;
            };

            // Act
            await _tokenManager.ClearToken();

            // Assert
            securityHelperMock.Verify(m => m("auth_token"), Times.Once);
            securityHelperMock.Verify(m => m("auth_token_expiry"), Times.Once);
        }

        [Fact]
        public async Task ClearToken_SecurityHelperThrowsException_PropagatesException()
        {
            // Arrange
            SecurityHelper.RemoveFromSecureStorage = async (key) => 
            {
                throw new Exception("Test security helper exception");
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _tokenManager.ClearToken());
            exception.Message.Should().Be("Test security helper exception");
        }

        [Fact]
        public async Task IsTokenValid_ValidTokenExists_ReturnsTrue()
        {
            // Arrange
            string testToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            string expiryTime = DateTime.UtcNow.AddHours(1).ToString("o"); // Future time
            
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                if (key == "auth_token") return testToken;
                if (key == "auth_token_expiry") return expiryTime;
                return null;
            };

            // Act
            var result = await _tokenManager.IsTokenValid();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsTokenValid_TokenExpired_ReturnsFalse()
        {
            // Arrange
            string testToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            string expiryTime = DateTime.UtcNow.AddHours(-1).ToString("o"); // Past time
            
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                if (key == "auth_token") return testToken;
                if (key == "auth_token_expiry") return expiryTime;
                return null;
            };

            // Act
            var result = await _tokenManager.IsTokenValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsTokenValid_NoToken_ReturnsFalse()
        {
            // Arrange
            SecurityHelper.GetFromSecureStorage = async (key) => null;

            // Act
            var result = await _tokenManager.IsTokenValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsTokenValid_NoExpiryTime_ReturnsFalse()
        {
            // Arrange
            string testToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                if (key == "auth_token") return testToken;
                return null;
            };

            // Act
            var result = await _tokenManager.IsTokenValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsTokenValid_SecurityHelperThrowsException_ReturnsFalse()
        {
            // Arrange
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                throw new Exception("Test security helper exception");
            };

            // Act
            var result = await _tokenManager.IsTokenValid();

            // Assert
            result.Should().BeFalse();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetTokenExpiryTime_ExpiryTimeExists_ReturnsDateTime()
        {
            // Arrange
            DateTime testExpiry = DateTime.UtcNow.AddHours(1);
            string expiryTimeStr = testExpiry.ToString("o");
            
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                if (key == "auth_token_expiry") return expiryTimeStr;
                return null;
            };

            // Act
            var result = await _tokenManager.GetTokenExpiryTime();

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().BeCloseTo(testExpiry, TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public async Task GetTokenExpiryTime_NoExpiryTime_ReturnsNull()
        {
            // Arrange
            SecurityHelper.GetFromSecureStorage = async (key) => null;

            // Act
            var result = await _tokenManager.GetTokenExpiryTime();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetTokenExpiryTime_InvalidExpiryTimeFormat_ReturnsNull()
        {
            // Arrange
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                if (key == "auth_token_expiry") return "not-a-valid-date-time";
                return null;
            };

            // Act
            var result = await _tokenManager.GetTokenExpiryTime();

            // Assert
            result.Should().BeNull();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task IsTokenExpiringSoon_TokenExpiringWithinThreshold_ReturnsTrue()
        {
            // Arrange
            // Set expiry time to 15 minutes in the future (within 30 minute threshold)
            DateTime expiryTime = DateTime.UtcNow.AddMinutes(15);
            string expiryTimeStr = expiryTime.ToString("o");
            
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                if (key == "auth_token_expiry") return expiryTimeStr;
                return null;
            };

            // Act
            var result = await _tokenManager.IsTokenExpiringSoon();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsTokenExpiringSoon_TokenNotExpiringWithinThreshold_ReturnsFalse()
        {
            // Arrange
            // Set expiry time to 60 minutes in the future (outside 30 minute threshold)
            DateTime expiryTime = DateTime.UtcNow.AddMinutes(60);
            string expiryTimeStr = expiryTime.ToString("o");
            
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                if (key == "auth_token_expiry") return expiryTimeStr;
                return null;
            };

            // Act
            var result = await _tokenManager.IsTokenExpiringSoon();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsTokenExpiringSoon_NoExpiryTime_ReturnsFalse()
        {
            // Arrange
            SecurityHelper.GetFromSecureStorage = async (key) => null;

            // Act
            var result = await _tokenManager.IsTokenExpiringSoon();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsTokenExpiringSoon_SecurityHelperThrowsException_ReturnsFalse()
        {
            // Arrange
            SecurityHelper.GetFromSecureStorage = async (key) => 
            {
                throw new Exception("Test security helper exception");
            };

            // Act
            var result = await _tokenManager.IsTokenExpiringSoon();

            // Assert
            result.Should().BeFalse();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}