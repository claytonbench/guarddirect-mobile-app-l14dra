using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.Helpers;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    public class GeofenceServiceTests
    {
        private readonly Mock<ILogger<GeofenceService>> _mockLogger;
        private readonly GeofenceService _geofenceService;
        private readonly List<CheckpointModel> _testCheckpoints;

        public GeofenceServiceTests()
        {
            // Initialize mock logger
            _mockLogger = new Mock<ILogger<GeofenceService>>();
            
            // Create GeofenceService instance with mocked logger
            _geofenceService = new GeofenceService(_mockLogger.Object);
            
            // Initialize test checkpoints using test data generator
            _testCheckpoints = TestCheckpoints.GenerateCheckpointModels(TestConstants.TestLocationId, 5);
        }

        [Fact]
        public async Task StartMonitoring_WithValidCheckpoints_ShouldStartMonitoring()
        {
            // Arrange - ensure we have test checkpoints
            _testCheckpoints.Should().NotBeNull();
            _testCheckpoints.Should().NotBeEmpty();

            // Act
            await _geofenceService.StartMonitoring(_testCheckpoints);

            // Assert
            var result = await _geofenceService.CheckProximity(_testCheckpoints[0].Latitude, _testCheckpoints[0].Longitude);
            result.Should().Contain(_testCheckpoints[0].Id);
        }

        [Fact]
        public async Task StartMonitoring_WithNullCheckpoints_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _geofenceService.StartMonitoring(null));
        }

        [Fact]
        public async Task StopMonitoring_WhenMonitoring_ShouldStopMonitoring()
        {
            // Arrange
            await _geofenceService.StartMonitoring(_testCheckpoints);

            // Act
            await _geofenceService.StopMonitoring();

            // Assert
            var result = await _geofenceService.CheckProximity(_testCheckpoints[0].Latitude, _testCheckpoints[0].Longitude);
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task CheckProximity_WhenNotMonitoring_ShouldReturnEmptyList()
        {
            // Arrange - ensure we're not monitoring (default state)
            
            // Act
            var result = await _geofenceService.CheckProximity(TestConstants.TestLatitude, TestConstants.TestLongitude);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task CheckProximity_WithCheckpointsInRange_ShouldReturnCheckpointIds()
        {
            // Arrange
            await _geofenceService.StartMonitoring(_testCheckpoints);
            
            // Use the first checkpoint coordinates to ensure it's in range
            var testCheckpoint = _testCheckpoints[0];
            
            // Act
            var result = await _geofenceService.CheckProximity(testCheckpoint.Latitude, testCheckpoint.Longitude);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain(testCheckpoint.Id);
        }

        [Fact]
        public async Task CheckProximity_WithNoCheckpointsInRange_ShouldReturnEmptyList()
        {
            // Arrange
            await _geofenceService.StartMonitoring(_testCheckpoints);
            
            // Use coordinates far away from test checkpoints
            double farAwayLatitude = TestConstants.TestLatitude + 1; // 1 degree is very far
            double farAwayLongitude = TestConstants.TestLongitude + 1;
            
            // Act
            var result = await _geofenceService.CheckProximity(farAwayLatitude, farAwayLongitude);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ProximityChanged_WhenStatusChanges_ShouldRaiseEvent()
        {
            // Arrange
            await _geofenceService.StartMonitoring(_testCheckpoints);
            
            var testCheckpoint = _testCheckpoints[0];
            bool eventRaised = false;
            CheckpointProximityEventArgs eventArgs = null;
            
            _geofenceService.ProximityChanged += (sender, args) => 
            {
                eventRaised = true;
                eventArgs = args;
            };
            
            // First, check with coordinates far away to establish "not in proximity" state
            double farAwayLatitude = TestConstants.TestLatitude + 1;
            double farAwayLongitude = TestConstants.TestLongitude + 1;
            await _geofenceService.CheckProximity(farAwayLatitude, farAwayLongitude);
            
            // Act - now check with coordinates near the test checkpoint to trigger a status change
            await _geofenceService.CheckProximity(testCheckpoint.Latitude, testCheckpoint.Longitude);

            // Assert
            eventRaised.Should().BeTrue();
            eventArgs.Should().NotBeNull();
            eventArgs.CheckpointId.Should().Be(testCheckpoint.Id);
            eventArgs.IsInRange.Should().BeTrue();
        }

        [Fact]
        public void ProximityThresholdFeet_WhenSet_ShouldChangeThreshold()
        {
            // Arrange
            double newThreshold = 100.0;
            
            // Act
            _geofenceService.ProximityThresholdFeet = newThreshold;
            
            // Assert
            _geofenceService.ProximityThresholdFeet.Should().Be(newThreshold);
        }

        [Fact]
        public async Task CheckProximity_WithDifferentThresholds_ShouldReturnDifferentResults()
        {
            // Arrange
            await _geofenceService.StartMonitoring(_testCheckpoints);
            var testCheckpoint = _testCheckpoints[0];
            
            // Calculate a position that should be between 50 and 100 feet from the checkpoint
            // First, convert 75 feet to meters
            double distanceInMeters = LocationHelper.ConvertFeetToMeters(75);
            
            // Adjust position by moving north (simplified approach)
            // This is a rough approximation - 1 degree latitude is about 111,000 meters
            double testLatitude = testCheckpoint.Latitude + (distanceInMeters / 111000);
            double testLongitude = testCheckpoint.Longitude;
            
            // Set a small threshold first (50 feet)
            _geofenceService.ProximityThresholdFeet = 50;
            
            // Act - First, check with a small threshold (point should be out of range)
            var smallThresholdResult = await _geofenceService.CheckProximity(testLatitude, testLongitude);
            
            // Now set a larger threshold (100 feet)
            _geofenceService.ProximityThresholdFeet = 100;
            
            // And check again (point should be in range now)
            var largeThresholdResult = await _geofenceService.CheckProximity(testLatitude, testLongitude);

            // Assert
            smallThresholdResult.Should().NotContain(testCheckpoint.Id);
            largeThresholdResult.Should().Contain(testCheckpoint.Id);
        }

        [Fact]
        public async Task StartMonitoring_WhenAlreadyMonitoring_ShouldRestartWithNewCheckpoints()
        {
            // Arrange
            await _geofenceService.StartMonitoring(_testCheckpoints);
            
            // Create a new set of test checkpoints
            var newCheckpoints = TestCheckpoints.GenerateCheckpointModels(TestConstants.TestLocationId + 1, 3);
            
            // Act
            await _geofenceService.StartMonitoring(newCheckpoints);
            
            // Test by checking proximity to a new checkpoint
            var result = await _geofenceService.CheckProximity(newCheckpoints[0].Latitude, newCheckpoints[0].Longitude);

            // Assert
            result.Should().Contain(newCheckpoints[0].Id);
            
            // Verify old checkpoints are no longer being monitored
            var oldResult = await _geofenceService.CheckProximity(_testCheckpoints[0].Latitude, _testCheckpoints[0].Longitude);
            oldResult.Should().NotContain(_testCheckpoints[0].Id);
        }
    }
}