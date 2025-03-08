using System;
using Xunit;
using FluentAssertions;
using SecurityPatrol.Helpers;
using SecurityPatrol.Constants;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.MAUI.UnitTests.Setup;

namespace SecurityPatrol.MAUI.UnitTests.Helpers
{
    /// <summary>
    /// Contains unit tests for the ValidationHelper class to verify validation logic for various inputs.
    /// </summary>
    public class ValidationHelperTests 
    {
        [Fact]
        public void ValidatePhoneNumber_WithValidPhoneNumber_ShouldReturnTrue()
        {
            // Arrange
            string phoneNumber = TestConstants.TestPhoneNumber;

            // Act
            var result = ValidationHelper.ValidatePhoneNumber(phoneNumber);

            // Assert
            result.isValid.Should().BeTrue();
            result.errorMessage.Should().BeNull();
        }

        [Fact]
        public void ValidatePhoneNumber_WithInvalidPhoneNumber_ShouldReturnFalse()
        {
            // Arrange
            string phoneNumber = "12345";

            // Act
            var result = ValidationHelper.ValidatePhoneNumber(phoneNumber);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be(ErrorMessages.InvalidPhoneNumber);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidatePhoneNumber_WithNullOrEmpty_ShouldReturnFalse(string phoneNumber)
        {
            // Act
            var result = ValidationHelper.ValidatePhoneNumber(phoneNumber);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be(ErrorMessages.InvalidPhoneNumber);
        }

        [Fact]
        public void ValidateVerificationCode_WithValidCode_ShouldReturnTrue()
        {
            // Arrange
            string code = TestConstants.TestVerificationCode;

            // Act
            var result = ValidationHelper.ValidateVerificationCode(code);

            // Assert
            result.isValid.Should().BeTrue();
            result.errorMessage.Should().BeNull();
        }

        [Theory]
        [InlineData("12345")]
        [InlineData("1234567")]
        public void ValidateVerificationCode_WithInvalidLength_ShouldReturnFalse(string code)
        {
            // Act
            var result = ValidationHelper.ValidateVerificationCode(code);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be(ErrorMessages.InvalidVerificationCode);
        }

        [Theory]
        [InlineData("12345a")]
        [InlineData("abc123")]
        public void ValidateVerificationCode_WithNonDigits_ShouldReturnFalse(string code)
        {
            // Act
            var result = ValidationHelper.ValidateVerificationCode(code);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be(ErrorMessages.InvalidVerificationCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidateVerificationCode_WithNullOrEmpty_ShouldReturnFalse(string code)
        {
            // Act
            var result = ValidationHelper.ValidateVerificationCode(code);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be(ErrorMessages.InvalidVerificationCode);
        }

        [Fact]
        public void ValidateReportText_WithValidText_ShouldReturnTrue()
        {
            // Arrange
            string text = TestConstants.TestReportText;

            // Act
            var result = ValidationHelper.ValidateReportText(text);

            // Assert
            result.isValid.Should().BeTrue();
            result.errorMessage.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidateReportText_WithNullOrEmpty_ShouldReturnFalse(string text)
        {
            // Act
            var result = ValidationHelper.ValidateReportText(text);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be(ErrorMessages.ReportEmpty);
        }

        [Fact]
        public void ValidateReportText_WithTooLongText_ShouldReturnFalse()
        {
            // Arrange
            string text = new string('a', AppConstants.ReportMaxLength + 1);

            // Act
            var result = ValidationHelper.ValidateReportText(text);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be(ErrorMessages.ReportTooLong);
        }

        [Fact]
        public void ValidateCheckpointProximity_WithinThreshold_ShouldReturnTrue()
        {
            // Arrange
            double userLat = 34.0522;
            double userLon = -118.2437;
            double checkpointLat = 34.0523;
            double checkpointLon = -118.2438;
            // This is a small enough distance to be within the 50 feet threshold

            // Act
            var result = ValidationHelper.ValidateCheckpointProximity(
                userLat, userLon, checkpointLat, checkpointLon);

            // Assert
            result.isValid.Should().BeTrue();
            result.errorMessage.Should().BeNull();
        }

        [Fact]
        public void ValidateCheckpointProximity_BeyondThreshold_ShouldReturnFalse()
        {
            // Arrange
            double userLat = 34.0522;
            double userLon = -118.2437;
            double checkpointLat = 34.0530;
            double checkpointLon = -118.2450;
            // This is a large enough distance to be beyond the 50 feet threshold

            // Act
            var result = ValidationHelper.ValidateCheckpointProximity(
                userLat, userLon, checkpointLat, checkpointLon);

            // Assert
            result.isValid.Should().BeFalse();
            result.errorMessage.Should().Be(ErrorMessages.CheckpointTooFar);
        }

        [Fact]
        public void CalculateDistance_ShouldReturnCorrectDistance()
        {
            // Arrange
            double lat1 = 34.0522;
            double lon1 = -118.2437;
            double lat2 = 34.0530;
            double lon2 = -118.2450;
            
            // Expected distance calculated using an external tool (approximately 111 meters)
            double expectedDistance = 111.0;
            double tolerance = 5.0; // Allow a 5-meter tolerance for floating-point calculations

            // Act
            double calculatedDistance = ValidationHelper.CalculateDistance(lat1, lon1, lat2, lon2);

            // Assert
            calculatedDistance.Should().BeApproximately(expectedDistance, tolerance);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsNullOrEmpty_WithNullOrEmpty_ShouldReturnTrue(string value)
        {
            // Act
            bool result = ValidationHelper.IsNullOrEmpty(value);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsNullOrEmpty_WithNonEmpty_ShouldReturnFalse()
        {
            // Arrange
            string value = "Test";

            // Act
            bool result = ValidationHelper.IsNullOrEmpty(value);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("123456")]
        [InlineData("0")]
        public void IsDigitsOnly_WithDigitsOnly_ShouldReturnTrue(string value)
        {
            // Act
            bool result = ValidationHelper.IsDigitsOnly(value);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("123a")]
        [InlineData("abc")]
        [InlineData("123 456")]
        public void IsDigitsOnly_WithNonDigits_ShouldReturnFalse(string value)
        {
            // Act
            bool result = ValidationHelper.IsDigitsOnly(value);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsDigitsOnly_WithNullOrEmpty_ShouldReturnFalse(string value)
        {
            // Act
            bool result = ValidationHelper.IsDigitsOnly(value);

            // Assert
            result.Should().BeFalse();
        }
    }
}