using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Extensions.Logging;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.Helpers;
using SecurityPatrol.TestCommon.Data;

namespace SecurityPatrol.MAUI.UnitTests.Helpers
{
    /// <summary>
    /// Test class containing unit tests for the LocationHelper class
    /// </summary>
    public class LocationHelperTests : TestBase
    {
        /// <summary>
        /// Initializes a new instance of the LocationHelperTests class
        /// </summary>
        public LocationHelperTests() : base()
        {
            // Call base constructor to initialize test environment
        }

        /// <summary>
        /// Tests that GetCurrentLocationAsync returns a valid location when permissions are granted
        /// </summary>
        [Fact]
        public async Task GetCurrentLocationAsync_WithValidPermissions_ReturnsLocation()
        {
            // Arrange: Mock PermissionHelper to return true for CheckLocationPermissionsAsync
            var mockPermissionHelper = new Mock<PermissionHelper>();
            
            // Arrange: Set up a test location in MockLocationService
            var testLocation = TestLocations.DefaultLocationModel;
            MockLocationService.Setup(m => m.GetCurrentLocationAsync(It.IsAny<GeolocationAccuracy>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()))
                .ReturnsAsync(testLocation);

            // Act: Call LocationHelper.GetCurrentLocationAsync with default parameters
            var result = await LocationHelper.GetCurrentLocationAsync();

            // Assert: Verify the returned location matches the expected test location
            result.Should().NotBeNull();
            result.Latitude.Should().Be(testLocation.Latitude);
            result.Longitude.Should().Be(testLocation.Longitude);
            result.Accuracy.Should().Be(testLocation.Accuracy);
            // Assert: Verify GetCurrentLocation was called on the location service
            MockLocationService.Verify(m => m.GetCurrentLocationAsync(It.IsAny<GeolocationAccuracy>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()), Times.Once);
        }

        /// <summary>
        /// Tests that GetCurrentLocationAsync throws UnauthorizedAccessException when permissions are not granted
        /// </summary>
        [Fact]
        public async Task GetCurrentLocationAsync_WithoutPermissions_ThrowsUnauthorizedAccessException()
        {
            // Arrange: Mock PermissionHelper to return false for CheckLocationPermissionsAsync
            
            // Act & Assert: Verify that calling LocationHelper.GetCurrentLocationAsync throws UnauthorizedAccessException
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                LocationHelper.GetCurrentLocationAsync());
        }

        /// <summary>
        /// Tests that GetCurrentLocationAsync propagates exceptions from the location service
        /// </summary>
        [Fact]
        public async Task GetCurrentLocationAsync_WhenServiceThrowsException_PropagatesException()
        {
            // Arrange: Mock PermissionHelper to return true for CheckLocationPermissionsAsync
            
            // Arrange: Configure MockLocationService to throw a test exception
            var testException = new Exception("Test location service exception");
            MockLocationService.Setup(m => m.GetCurrentLocationAsync(It.IsAny<GeolocationAccuracy>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()))
                .ThrowsAsync(testException);

            // Act & Assert: Verify that calling LocationHelper.GetCurrentLocationAsync throws the same exception
            var exception = await Assert.ThrowsAsync<Exception>(() => 
                LocationHelper.GetCurrentLocationAsync());
            
            exception.Should().BeSameAs(testException);
        }

        /// <summary>
        /// Tests that CalculateDistance correctly calculates the distance between two coordinates
        /// </summary>
        [Fact]
        public void CalculateDistance_WithValidCoordinates_ReturnsCorrectDistance()
        {
            // Arrange: Define two sets of coordinates with known distance
            double lat1 = 40.7128; // NYC latitude
            double lon1 = -74.0060; // NYC longitude
            double lat2 = 34.0522; // LA latitude
            double lon2 = -118.2437; // LA longitude
            double expectedDistance = 3944000; // Approximate distance in meters
            double tolerance = 10000; // 10km tolerance due to Haversine approximation

            // Act: Call LocationHelper.CalculateDistance with the coordinates
            double distance = LocationHelper.CalculateDistance(lat1, lon1, lat2, lon2);

            // Assert: Verify the calculated distance is within acceptable margin of error from expected distance
            distance.Should().BeApproximately(expectedDistance, tolerance);
        }

        /// <summary>
        /// Tests that CalculateDistance returns zero when the same coordinates are provided
        /// </summary>
        [Fact]
        public void CalculateDistance_WithSameCoordinates_ReturnsZero()
        {
            // Arrange: Define a single set of coordinates
            double lat = 40.7128;
            double lon = -74.0060;

            // Act: Call LocationHelper.CalculateDistance with the same coordinates for both points
            double distance = LocationHelper.CalculateDistance(lat, lon, lat, lon);

            // Assert: Verify the calculated distance is zero
            distance.Should().Be(0);
        }

        /// <summary>
        /// Tests that CalculateDistance correctly calculates the distance between two LocationModel objects
        /// </summary>
        [Fact]
        public void CalculateDistance_WithLocationModels_ReturnsCorrectDistance()
        {
            // Arrange: Create two LocationModel objects with known coordinates
            var location1 = new LocationModel
            {
                Latitude = 40.7128,
                Longitude = -74.0060
            };

            var location2 = new LocationModel
            {
                Latitude = 34.0522,
                Longitude = -118.2437
            };

            double expectedDistance = 3944000; // Approximate distance in meters
            double tolerance = 10000; // 10km tolerance due to Haversine approximation

            // Act: Call LocationHelper.CalculateDistance with the LocationModel objects
            double distance = LocationHelper.CalculateDistance(location1, location2);

            // Assert: Verify the calculated distance is within acceptable margin of error from expected distance
            distance.Should().BeApproximately(expectedDistance, tolerance);
        }

        /// <summary>
        /// Tests that CalculateDistance throws ArgumentNullException when null LocationModel objects are provided
        /// </summary>
        [Fact]
        public void CalculateDistance_WithNullLocationModels_ThrowsArgumentNullException()
        {
            // Arrange: Create one valid LocationModel and set the other to null
            var location = new LocationModel
            {
                Latitude = 40.7128,
                Longitude = -74.0060
            };

            // Act & Assert: Verify that calling LocationHelper.CalculateDistance with null parameter throws ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => LocationHelper.CalculateDistance(null, location));
            // Act & Assert: Verify that calling LocationHelper.CalculateDistance with null for the other parameter also throws ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => LocationHelper.CalculateDistance(location, null));
        }

        /// <summary>
        /// Tests that ConvertMetersToFeet correctly converts meters to feet
        /// </summary>
        [Theory]
        [InlineData(1, 3.28084)]
        [InlineData(100, 328.084)]
        [InlineData(0, 0)]
        public void ConvertMetersToFeet_WithValidInput_ReturnsCorrectConversion(double meters, double expectedFeet)
        {
            // Arrange: Use theory with inline data for various meter values and expected feet conversions

            // Act: Call LocationHelper.ConvertMetersToFeet with the meter value
            double feet = LocationHelper.ConvertMetersToFeet(meters);

            // Assert: Verify the converted value matches the expected feet value within acceptable precision
            feet.Should().BeApproximately(expectedFeet, 0.001);
        }

        /// <summary>
        /// Tests that ConvertFeetToMeters correctly converts feet to meters
        /// </summary>
        [Theory]
        [InlineData(3.28084, 1)]
        [InlineData(328.084, 100)]
        [InlineData(0, 0)]
        public void ConvertFeetToMeters_WithValidInput_ReturnsCorrectConversion(double feet, double expectedMeters)
        {
            // Arrange: Use theory with inline data for various feet values and expected meter conversions

            // Act: Call LocationHelper.ConvertFeetToMeters with the feet value
            double meters = LocationHelper.ConvertFeetToMeters(feet);

            // Assert: Verify the converted value matches the expected meter value within acceptable precision
            meters.Should().BeApproximately(expectedMeters, 0.001);
        }

        /// <summary>
        /// Tests that FormatDistance correctly formats distances in metric units
        /// </summary>
        [Theory]
        [InlineData(10, "10 m")]
        [InlineData(1500, "1.5 km")]
        [InlineData(0, "0 m")]
        public void FormatDistance_WithMetricUnits_ReturnsCorrectFormat(double distance, string expected)
        {
            // Arrange: Use theory with inline data for various distances and expected formatted strings

            // Act: Call LocationHelper.FormatDistance with the distance value and useImperial=false
            string formatted = LocationHelper.FormatDistance(distance, false);

            // Assert: Verify the formatted string matches the expected format
            formatted.Should().Be(expected);
        }

        /// <summary>
        /// Tests that FormatDistance correctly formats distances in imperial units
        /// </summary>
        [Theory]
        [InlineData(10, "33 ft")]
        [InlineData(1609.34, "1.0 mi")]
        [InlineData(0, "0 ft")]
        public void FormatDistance_WithImperialUnits_ReturnsCorrectFormat(double distance, string expected)
        {
            // Arrange: Use theory with inline data for various distances and expected formatted strings

            // Act: Call LocationHelper.FormatDistance with the distance value and useImperial=true
            string formatted = LocationHelper.FormatDistance(distance, true);

            // Assert: Verify the formatted string matches the expected format
            formatted.Should().Be(expected);
        }

        /// <summary>
        /// Tests that IsWithinDistance returns true when coordinates are within the specified threshold
        /// </summary>
        [Fact]
        public void IsWithinDistance_WithinThreshold_ReturnsTrue()
        {
            // Arrange: Define two sets of coordinates that are close to each other
            double lat1 = 40.7128;
            double lon1 = -74.0060;
            double lat2 = 40.7130;
            double lon2 = -74.0062;
            // Arrange: Define a threshold distance that is greater than the actual distance
            double threshold = 100; // 100 meters

            // Act: Call LocationHelper.IsWithinDistance with the coordinates and threshold
            bool result = LocationHelper.IsWithinDistance(lat1, lon1, lat2, lon2, threshold);

            // Assert: Verify the result is true
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that IsWithinDistance returns false when coordinates are beyond the specified threshold
        /// </summary>
        [Fact]
        public void IsWithinDistance_BeyondThreshold_ReturnsFalse()
        {
            // Arrange: Define two sets of coordinates that are far from each other
            double lat1 = 40.7128;
            double lon1 = -74.0060;
            double lat2 = 40.7200;
            double lon2 = -74.0200;
            // Arrange: Define a threshold distance that is less than the actual distance
            double threshold = 100; // 100 meters

            // Act: Call LocationHelper.IsWithinDistance with the coordinates and threshold
            bool result = LocationHelper.IsWithinDistance(lat1, lon1, lat2, lon2, threshold);

            // Assert: Verify the result is false
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that IsWithinDistance correctly determines proximity using LocationModel objects
        /// </summary>
        [Fact]
        public void IsWithinDistance_WithLocationModels_ReturnsCorrectResult()
        {
            // Arrange: Create two LocationModel objects with known coordinates
            var location1 = new LocationModel
            {
                Latitude = 40.7128,
                Longitude = -74.0060
            };

            var location2 = new LocationModel
            {
                Latitude = 40.7130,
                Longitude = -74.0062
            };

            // Arrange: Calculate the actual distance between them
            double actualDistance = LocationHelper.CalculateDistance(location1, location2);
            // Arrange: Define a threshold slightly greater than the actual distance
            double thresholdWithin = actualDistance + 10;

            // Act: Call LocationHelper.IsWithinDistance with the LocationModels and threshold
            bool result = LocationHelper.IsWithinDistance(location1, location2, thresholdWithin);

            // Assert: Verify the result is true
            result.Should().BeTrue();

            // Act: Call LocationHelper.IsWithinDistance with a smaller threshold
            bool resultBeyond = LocationHelper.IsWithinDistance(location1, location2, actualDistance - 10);

            // Assert: Verify the result is false
            resultBeyond.Should().BeFalse();
        }

        /// <summary>
        /// Tests that IsWithinDistance throws ArgumentNullException when null LocationModel objects are provided
        /// </summary>
        [Fact]
        public void IsWithinDistance_WithNullLocationModels_ThrowsArgumentNullException()
        {
            // Arrange: Create one valid LocationModel and set the other to null
            var location = new LocationModel
            {
                Latitude = 40.7128,
                Longitude = -74.0060
            };
            double threshold = 100;

            // Act & Assert: Verify that calling LocationHelper.IsWithinDistance with null parameter throws ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => 
                LocationHelper.IsWithinDistance(null, location, threshold));
            // Act & Assert: Verify that calling LocationHelper.IsWithinDistance with null for the other parameter also throws ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => 
                LocationHelper.IsWithinDistance(location, null, threshold));
        }

        /// <summary>
        /// Tests that GetLocationTrackingAccuracy returns Medium accuracy when battery optimization is enabled
        /// </summary>
        [Fact]
        public void GetLocationTrackingAccuracy_WithBatteryOptimized_ReturnsMediumAccuracy()
        {
            // Act: Call LocationHelper.GetLocationTrackingAccuracy with batteryOptimized=true
            var accuracy = LocationHelper.GetLocationTrackingAccuracy(true);

            // Assert: Verify the result is GeolocationAccuracy.Medium
            accuracy.Should().Be(GeolocationAccuracy.Medium);
        }

        /// <summary>
        /// Tests that GetLocationTrackingAccuracy returns Best accuracy when battery optimization is disabled
        /// </summary>
        [Fact]
        public void GetLocationTrackingAccuracy_WithoutBatteryOptimized_ReturnsBestAccuracy()
        {
            // Act: Call LocationHelper.GetLocationTrackingAccuracy with batteryOptimized=false
            var accuracy = LocationHelper.GetLocationTrackingAccuracy(false);

            // Assert: Verify the result is GeolocationAccuracy.Best
            accuracy.Should().Be(GeolocationAccuracy.Best);
        }
    }
}