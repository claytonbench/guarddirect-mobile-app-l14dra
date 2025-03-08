using System;
using System.Globalization;
using System.IO;
using Xunit;
using FluentAssertions;
using Microsoft.Maui.Graphics;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Converters;
using SecurityPatrol.Models;

namespace SecurityPatrol.MAUI.UnitTests.Converters
{
    public class BoolToVisibilityConverterTests : TestBase
    {
        [Fact]
        public void BoolToVisibilityConverter_Convert_WithTrueValue_ReturnsVisible()
        {
            // Arrange
            var converter = new BoolToVisibilityConverter();

            // Act
            var result = converter.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(true);
        }

        [Fact]
        public void BoolToVisibilityConverter_Convert_WithFalseValue_ReturnsCollapsed()
        {
            // Arrange
            var converter = new BoolToVisibilityConverter();

            // Act
            var result = converter.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(false);
        }

        [Fact]
        public void BoolToVisibilityConverter_Convert_WithNonBoolValue_ReturnsCollapsed()
        {
            // Arrange
            var converter = new BoolToVisibilityConverter();

            // Act
            var result = converter.Convert("not a bool", typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(false);
        }

        [Fact]
        public void BoolToVisibilityConverter_ConvertBack_WithVisibleValue_ReturnsTrue()
        {
            // Arrange
            var converter = new BoolToVisibilityConverter();

            // Act
            var result = converter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(true);
        }

        [Fact]
        public void BoolToVisibilityConverter_ConvertBack_WithCollapsedValue_ReturnsFalse()
        {
            // Arrange
            var converter = new BoolToVisibilityConverter();

            // Act
            var result = converter.ConvertBack(false, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(false);
        }
    }

    public class ByteArrayToImageSourceConverterTests : TestBase
    {
        [Fact]
        public void ByteArrayToImageSourceConverter_Convert_WithValidByteArray_ReturnsImageSource()
        {
            // Arrange
            var converter = new ByteArrayToImageSourceConverter();
            var imageData = new byte[] { 1, 2, 3, 4, 5 }; // Simple byte array

            // Act
            var result = converter.Convert(imageData, typeof(ImageSource), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<ImageSource>();
        }

        [Fact]
        public void ByteArrayToImageSourceConverter_Convert_WithNullValue_ReturnsNull()
        {
            // Arrange
            var converter = new ByteArrayToImageSourceConverter();

            // Act
            var result = converter.Convert(null, typeof(ImageSource), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ByteArrayToImageSourceConverter_Convert_WithNonByteArrayValue_ReturnsNull()
        {
            // Arrange
            var converter = new ByteArrayToImageSourceConverter();

            // Act
            var result = converter.Convert("not a byte array", typeof(ImageSource), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ByteArrayToImageSourceConverter_ConvertBack_Always_ReturnsNull()
        {
            // Arrange
            var converter = new ByteArrayToImageSourceConverter();

            // Act
            var result = converter.ConvertBack(new MemoryStream(), typeof(byte[]), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeNull();
        }
    }

    public class DateTimeConverterTests : TestBase
    {
        [Fact]
        public void DateTimeConverter_Convert_WithDateTime_ReturnsFormattedString()
        {
            // Arrange
            var converter = new DateTimeConverter();
            var dateTime = new DateTime(2023, 1, 15, 14, 30, 0);

            // Act
            var result = converter.Convert(dateTime, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<string>();
            result.As<string>().Should().NotBeEmpty();
        }

        [Fact]
        public void DateTimeConverter_Convert_WithDateParameter_ReturnsDateOnlyFormat()
        {
            // Arrange
            var converter = new DateTimeConverter();
            var dateTime = new DateTime(2023, 1, 15, 14, 30, 0);

            // Act
            var result = converter.Convert(dateTime, typeof(string), "Date", CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<string>();
            result.As<string>().Should().Be("01/15/2023");
        }

        [Fact]
        public void DateTimeConverter_Convert_WithTimeParameter_ReturnsTimeOnlyFormat()
        {
            // Arrange
            var converter = new DateTimeConverter();
            var dateTime = new DateTime(2023, 1, 15, 14, 30, 0);

            // Act
            var result = converter.Convert(dateTime, typeof(string), "Time", CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<string>();
            result.As<string>().Should().Be("02:30 PM");
        }

        [Fact]
        public void DateTimeConverter_Convert_WithNonDateTimeValue_ReturnsEmptyString()
        {
            // Arrange
            var converter = new DateTimeConverter();

            // Act
            var result = converter.Convert("not a datetime", typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<string>();
            result.As<string>().Should().BeEmpty();
        }

        [Fact]
        public void DateTimeConverter_ConvertBack_Always_ThrowsNotImplementedException()
        {
            // Arrange
            var converter = new DateTimeConverter();

            // Act & Assert
            Action action = () => converter.ConvertBack("01/15/2023", typeof(DateTime), null, CultureInfo.InvariantCulture);
            action.Should().Throw<NotImplementedException>();
        }
    }

    public class DistanceConverterTests : TestBase
    {
        [Fact]
        public void DistanceConverter_Convert_WithNumericValue_ReturnsFormattedString()
        {
            // Arrange
            var converter = new DistanceConverter();
            double distanceInMeters = 1000;

            // Act
            var result = converter.Convert(distanceInMeters, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<string>();
            result.As<string>().Should().NotBeEmpty();
            result.As<string>().Should().Contain("ft"); // Default should be feet
        }

        [Fact]
        public void DistanceConverter_Convert_WithFeetParameter_ReturnsFeetUnit()
        {
            // Arrange
            var converter = new DistanceConverter();
            double distanceInMeters = 100;

            // Act
            var result = converter.Convert(distanceInMeters, typeof(string), "Feet", CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<string>();
            result.As<string>().Should().Contain("ft");
        }

        [Fact]
        public void DistanceConverter_Convert_WithMetersParameter_ReturnsMetersUnit()
        {
            // Arrange
            var converter = new DistanceConverter();
            double distanceInMeters = 100;

            // Act
            var result = converter.Convert(distanceInMeters, typeof(string), "Meters", CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<string>();
            result.As<string>().Should().Contain("m");
        }

        [Fact]
        public void DistanceConverter_Convert_WithNonNumericValue_ReturnsEmptyString()
        {
            // Arrange
            var converter = new DistanceConverter();

            // Act
            var result = converter.Convert("not a number", typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<string>();
            result.As<string>().Should().BeEmpty();
        }

        [Fact]
        public void DistanceConverter_ConvertBack_Always_ThrowsNotImplementedException()
        {
            // Arrange
            var converter = new DistanceConverter();

            // Act & Assert
            Action action = () => converter.ConvertBack("100 ft", typeof(double), null, CultureInfo.InvariantCulture);
            action.Should().Throw<NotImplementedException>();
        }
    }

    public class InverseBoolConverterTests : TestBase
    {
        [Fact]
        public void InverseBoolConverter_Convert_WithTrueValue_ReturnsFalse()
        {
            // Arrange
            var converter = new InverseBoolConverter();

            // Act
            var result = converter.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(false);
        }

        [Fact]
        public void InverseBoolConverter_Convert_WithFalseValue_ReturnsTrue()
        {
            // Arrange
            var converter = new InverseBoolConverter();

            // Act
            var result = converter.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(true);
        }

        [Fact]
        public void InverseBoolConverter_Convert_WithNonBoolValue_ReturnsFalse()
        {
            // Arrange
            var converter = new InverseBoolConverter();

            // Act
            var result = converter.Convert("not a bool", typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(false);
        }

        [Fact]
        public void InverseBoolConverter_ConvertBack_WithTrueValue_ReturnsFalse()
        {
            // Arrange
            var converter = new InverseBoolConverter();

            // Act
            var result = converter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(false);
        }

        [Fact]
        public void InverseBoolConverter_ConvertBack_WithFalseValue_ReturnsTrue()
        {
            // Arrange
            var converter = new InverseBoolConverter();

            // Act
            var result = converter.ConvertBack(false, typeof(bool), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be(true);
        }
    }

    public class StatusToColorConverterTests : TestBase
    {
        [Fact]
        public void StatusToColorConverter_Convert_WithBooleanTrue_ReturnsSuccessColor()
        {
            // Arrange
            var converter = new StatusToColorConverter();

            // Act
            var result = converter.Convert(true, typeof(Color), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<Color>();
        }

        [Fact]
        public void StatusToColorConverter_Convert_WithBooleanFalse_ReturnsErrorColor()
        {
            // Arrange
            var converter = new StatusToColorConverter();

            // Act
            var result = converter.Convert(false, typeof(Color), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<Color>();
        }

        [Fact]
        public void StatusToColorConverter_Convert_WithClockStatusTrue_ReturnsClockInColor()
        {
            // Arrange
            var converter = new StatusToColorConverter();
            var clockStatus = new ClockStatus { IsClocked = true };

            // Act
            var result = converter.Convert(clockStatus, typeof(Color), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<Color>();
        }

        [Fact]
        public void StatusToColorConverter_Convert_WithClockStatusFalse_ReturnsClockOutColor()
        {
            // Arrange
            var converter = new StatusToColorConverter();
            var clockStatus = new ClockStatus { IsClocked = false };

            // Act
            var result = converter.Convert(clockStatus, typeof(Color), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<Color>();
        }

        [Fact]
        public void StatusToColorConverter_Convert_WithCheckpointStatusVerified_ReturnsCheckpointCompletedColor()
        {
            // Arrange
            var converter = new StatusToColorConverter();
            var checkpointStatus = new CheckpointStatus { IsVerified = true };

            // Act
            var result = converter.Convert(checkpointStatus, typeof(Color), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<Color>();
        }

        [Fact]
        public void StatusToColorConverter_Convert_WithSyncItemFailed_ReturnsSyncFailedColor()
        {
            // Arrange
            var converter = new StatusToColorConverter();
            var syncItem = new SyncItem("TestType", "TestId", 1)
            {
                RetryCount = 1,
                ErrorMessage = "Test error"
            };

            // Act
            var result = converter.Convert(syncItem, typeof(Color), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<Color>();
        }

        [Fact]
        public void StatusToColorConverter_Convert_WithPatrolStatusComplete_ReturnsSuccessColor()
        {
            // Arrange
            var converter = new StatusToColorConverter();
            var patrolStatus = new PatrolStatus
            {
                TotalCheckpoints = 5,
                VerifiedCheckpoints = 5
            };

            // Act
            var result = converter.Convert(patrolStatus, typeof(Color), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<Color>();
        }

        [Fact]
        public void StatusToColorConverter_Convert_WithNullValue_ReturnsInactiveColor()
        {
            // Arrange
            var converter = new StatusToColorConverter();

            // Act
            var result = converter.Convert(null, typeof(Color), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<Color>();
        }

        [Fact]
        public void StatusToColorConverter_ConvertBack_Always_ThrowsNotImplementedException()
        {
            // Arrange
            var converter = new StatusToColorConverter();

            // Act & Assert
            Action action = () => converter.ConvertBack(Colors.Green, typeof(bool), null, CultureInfo.InvariantCulture);
            action.Should().Throw<NotImplementedException>();
        }
    }
}