using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Infrastructure.Services;
using SecurityPatrol.Application.Services;
using SecurityPatrol.API.UnitTests.Setup;

namespace SecurityPatrol.API.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the VerificationCodeService class to verify its functionality for generating, storing, and validating verification codes.
    /// </summary>
    public class VerificationCodeServiceTests
    {
        private readonly Mock<IDateTime> _mockDateTime;
        private readonly Mock<ILogger<VerificationCodeService>> _mockLogger;
        private readonly VerificationCodeService _verificationCodeService;

        /// <summary>
        /// Initializes a new instance of the VerificationCodeServiceTests class with mocked dependencies
        /// </summary>
        public VerificationCodeServiceTests()
        {
            _mockDateTime = new Mock<IDateTime>();
            _mockLogger = new Mock<ILogger<VerificationCodeService>>();
            
            // Setup a fixed date/time for predictable testing
            var fixedTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _mockDateTime.Setup(dt => dt.UtcNow()).Returns(fixedTime);
            
            // Initialize the service with mocked dependencies
            _verificationCodeService = new VerificationCodeService(_mockDateTime.Object, _mockLogger.Object);
        }

        /// <summary>
        /// Tests that GenerateCodeAsync returns a valid verification code when provided with a valid phone number
        /// </summary>
        [Fact]
        public async Task GenerateCodeAsync_WithValidPhoneNumber_ReturnsCode()
        {
            // Act
            var result = await _verificationCodeService.GenerateCodeAsync(TestConstants.TestPhoneNumber);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveLength(6); // Default code length is 6
            result.Should().MatchRegex("^\\d+$"); // Should contain only digits
        }

        /// <summary>
        /// Tests that GenerateCodeAsync throws an ArgumentException when provided with an invalid phone number
        /// </summary>
        [Fact]
        public async Task GenerateCodeAsync_WithInvalidPhoneNumber_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _verificationCodeService.GenerateCodeAsync(null));
                
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _verificationCodeService.GenerateCodeAsync(string.Empty));
        }

        /// <summary>
        /// Tests that StoreCodeAsync returns a valid verification ID when provided with valid phone number and code
        /// </summary>
        [Fact]
        public async Task StoreCodeAsync_WithValidData_ReturnsVerificationId()
        {
            // Act
            var result = await _verificationCodeService.StoreCodeAsync(
                TestConstants.TestPhoneNumber, 
                TestConstants.TestVerificationCode);
            
            // Assert
            result.Should().NotBeNull();
            Guid.TryParse(result, out _).Should().BeTrue(); // Should be a valid GUID
        }

        /// <summary>
        /// Tests that StoreCodeAsync throws an ArgumentException when provided with invalid data
        /// </summary>
        [Fact]
        public async Task StoreCodeAsync_WithInvalidData_ThrowsArgumentException()
        {
            // Act & Assert - Test with null phone number
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _verificationCodeService.StoreCodeAsync(null, TestConstants.TestVerificationCode));
            
            // Test with empty phone number
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _verificationCodeService.StoreCodeAsync(string.Empty, TestConstants.TestVerificationCode));
            
            // Test with null verification code
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _verificationCodeService.StoreCodeAsync(TestConstants.TestPhoneNumber, null));
            
            // Test with empty verification code
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _verificationCodeService.StoreCodeAsync(TestConstants.TestPhoneNumber, string.Empty));
        }

        /// <summary>
        /// Tests that ValidateCodeAsync returns true when provided with a valid verification ID and matching code
        /// </summary>
        [Fact]
        public async Task ValidateCodeAsync_WithValidData_ReturnsTrue()
        {
            // Arrange - First store a verification code
            var verificationId = await _verificationCodeService.StoreCodeAsync(
                TestConstants.TestPhoneNumber, 
                TestConstants.TestVerificationCode);
            
            // Act - Validate the code
            var result = await _verificationCodeService.ValidateCodeAsync(
                verificationId, 
                TestConstants.TestVerificationCode);
            
            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ValidateCodeAsync returns false when provided with a valid verification ID but non-matching code
        /// </summary>
        [Fact]
        public async Task ValidateCodeAsync_WithInvalidCode_ReturnsFalse()
        {
            // Arrange - First store a verification code
            var verificationId = await _verificationCodeService.StoreCodeAsync(
                TestConstants.TestPhoneNumber, 
                TestConstants.TestVerificationCode);
            
            // Act - Validate with wrong code
            var result = await _verificationCodeService.ValidateCodeAsync(
                verificationId, 
                "999999"); // Different code than what was stored
            
            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ValidateCodeAsync returns false when provided with an invalid verification ID
        /// </summary>
        [Fact]
        public async Task ValidateCodeAsync_WithInvalidVerificationId_ReturnsFalse()
        {
            // Act - Use a non-existent verification ID
            var result = await _verificationCodeService.ValidateCodeAsync(
                Guid.NewGuid().ToString(), // Random non-existent ID
                TestConstants.TestVerificationCode);
            
            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ValidateCodeAsync returns false when the verification code has expired
        /// </summary>
        [Fact]
        public async Task ValidateCodeAsync_WithExpiredCode_ReturnsFalse()
        {
            // Arrange - First store a verification code
            var verificationId = await _verificationCodeService.StoreCodeAsync(
                TestConstants.TestPhoneNumber, 
                TestConstants.TestVerificationCode);
            
            // Advance time beyond the expiration period (default is 10 minutes)
            var expiredTime = _mockDateTime.Object.UtcNow().AddMinutes(15); // 15 minutes in the future
            _mockDateTime.Setup(dt => dt.UtcNow()).Returns(expiredTime);
            
            // Act - Try to validate the now-expired code
            var result = await _verificationCodeService.ValidateCodeAsync(
                verificationId, 
                TestConstants.TestVerificationCode);
            
            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that GetCodeExpirationAsync returns the correct expiration time for a valid verification ID
        /// </summary>
        [Fact]
        public async Task GetCodeExpirationAsync_WithValidVerificationId_ReturnsExpirationTime()
        {
            // Arrange - First store a verification code
            var verificationId = await _verificationCodeService.StoreCodeAsync(
                TestConstants.TestPhoneNumber, 
                TestConstants.TestVerificationCode);
            
            // Act
            var result = await _verificationCodeService.GetCodeExpirationAsync(verificationId);
            
            // Assert
            result.Should().NotBeNull();
            // Default expiration is 10 minutes from creation
            result.Value.Should().BeCloseTo(_mockDateTime.Object.UtcNow().AddMinutes(10), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Tests that GetCodeExpirationAsync returns null for an invalid verification ID
        /// </summary>
        [Fact]
        public async Task GetCodeExpirationAsync_WithInvalidVerificationId_ReturnsNull()
        {
            // Act - Use a non-existent verification ID
            var result = await _verificationCodeService.GetCodeExpirationAsync(Guid.NewGuid().ToString());
            
            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that ClearExpiredCodesAsync removes expired verification codes
        /// </summary>
        [Fact]
        public async Task ClearExpiredCodesAsync_RemovesExpiredCodes()
        {
            // Arrange - First store a verification code
            var verificationId = await _verificationCodeService.StoreCodeAsync(
                TestConstants.TestPhoneNumber, 
                TestConstants.TestVerificationCode);
            
            // Advance time beyond the expiration period
            var expiredTime = _mockDateTime.Object.UtcNow().AddMinutes(15); // 15 minutes in the future
            _mockDateTime.Setup(dt => dt.UtcNow()).Returns(expiredTime);
            
            // Act - Run the cleanup process
            await _verificationCodeService.ClearExpiredCodesAsync();
            
            // Verify the code was removed by trying to get its expiration time
            var expirationResult = await _verificationCodeService.GetCodeExpirationAsync(verificationId);
            
            // Assert
            expirationResult.Should().BeNull(); // Should be null because code was removed
        }
    }
}