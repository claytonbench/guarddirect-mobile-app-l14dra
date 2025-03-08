using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using Xunit;

namespace SecurityPatrol.UnitTests.Services
{
    public class PatrolServiceTests
    {
        private Mock<ICheckpointRepository> _mockCheckpointRepository;
        private Mock<ILocationService> _mockLocationService;
        private Mock<IGeofenceService> _mockGeofenceService;
        private Mock<IMapService> _mockMapService;
        private Mock<ILogger<PatrolService>> _mockLogger;
        private PatrolService _patrolService;

        public PatrolServiceTests()
        {
            Setup();
        }

        private void Setup()
        {
            // Initialize mocks
            _mockCheckpointRepository = new Mock<ICheckpointRepository>();
            _mockLocationService = new Mock<ILocationService>();
            _mockGeofenceService = new Mock<IGeofenceService>();
            _mockMapService = new Mock<IMapService>();
            _mockLogger = new Mock<ILogger<PatrolService>>();

            // Create PatrolService with mocked dependencies
            _patrolService = new PatrolService(
                _mockCheckpointRepository.Object,
                _mockLocationService.Object,
                _mockGeofenceService.Object,
                _mockMapService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetLocations_ShouldReturnLocations()
        {
            // Arrange
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(10, 1);
            var testCheckpoints2 = TestDataGenerator.CreateCheckpointModels(5, 2);
            var allCheckpoints = testCheckpoints.Concat(testCheckpoints2).ToList();
            
            _mockCheckpointRepository.Setup(r => r.GetAllCheckpointsAsync())
                .ReturnsAsync(allCheckpoints);

            // Act
            var result = await _patrolService.GetLocations();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count()); // Should return 2 distinct locations
            Assert.Contains(result, l => l.Id == 1);
            Assert.Contains(result, l => l.Id == 2);
            
            // Verify the repository method was called
            _mockCheckpointRepository.Verify(r => r.GetAllCheckpointsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCheckpoints_WithValidLocationId_ShouldReturnCheckpoints()
        {
            // Arrange
            int locationId = 1;
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);

            // Act
            var result = await _patrolService.GetCheckpoints(locationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count());
            Assert.All(result, checkpoint => Assert.Equal(locationId, checkpoint.LocationId));
            
            // Verify the repository method was called with correct locationId
            _mockCheckpointRepository.Verify(r => r.GetCheckpointsByLocationIdAsync(locationId), Times.Once);
        }

        [Fact]
        public async Task GetCheckpoints_WithInvalidLocationId_ShouldThrowArgumentException()
        {
            // Arrange
            int invalidLocationId = 0;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _patrolService.GetCheckpoints(invalidLocationId));
            
            // Verify that the repository method was not called
            _mockCheckpointRepository.Verify(r => r.GetCheckpointsByLocationIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task StartPatrol_WithValidLocationId_ShouldInitializePatrol()
        {
            // Arrange
            int locationId = 1;
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);
            
            _mockGeofenceService.Setup(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            
            _mockMapService.Setup(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _patrolService.StartPatrol(locationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(locationId, result.LocationId);
            Assert.Equal(5, result.TotalCheckpoints);
            Assert.Equal(0, result.VerifiedCheckpoints);
            Assert.True(_patrolService.IsPatrolActive);
            Assert.Equal(locationId, _patrolService.CurrentLocationId);
            
            // Verify appropriate methods were called
            _mockCheckpointRepository.Verify(r => r.GetCheckpointsByLocationIdAsync(locationId), Times.Once);
            _mockCheckpointRepository.Verify(r => r.ClearCheckpointStatusesAsync(locationId), Times.Once);
            _mockGeofenceService.Verify(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()), Times.Once);
            _mockMapService.Verify(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()), Times.Once);
        }

        [Fact]
        public async Task StartPatrol_WithNoCheckpoints_ShouldThrowException()
        {
            // Arrange
            int locationId = 1;
            
            // Empty checkpoint list
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(new List<CheckpointModel>());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _patrolService.StartPatrol(locationId));
            
            // Verify repository was called but not the other methods
            _mockCheckpointRepository.Verify(r => r.GetCheckpointsByLocationIdAsync(locationId), Times.Once);
            _mockGeofenceService.Verify(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()), Times.Never);
        }

        [Fact]
        public async Task EndPatrol_WhenPatrolActive_ShouldEndPatrol()
        {
            // Arrange
            int locationId = 1;
            
            // Start a patrol first
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);
            
            _mockGeofenceService.Setup(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            
            _mockGeofenceService.Setup(g => g.StopMonitoring())
                .Returns(Task.CompletedTask);
            
            _mockMapService.Setup(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
                
            await _patrolService.StartPatrol(locationId);
            
            // Act
            var result = await _patrolService.EndPatrol();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(locationId, result.LocationId);
            Assert.False(_patrolService.IsPatrolActive);
            Assert.Null(_patrolService.CurrentLocationId);
            Assert.NotNull(result.EndTime); // EndTime should be set
            
            // Verify appropriate methods were called
            _mockGeofenceService.Verify(g => g.StopMonitoring(), Times.Once);
            _mockMapService.Verify(m => m.ClearCheckpoints(), Times.Once);
        }

        [Fact]
        public async Task EndPatrol_WhenNoPatrolActive_ShouldReturnNull()
        {
            // Arrange - no active patrol

            // Act
            var result = await _patrolService.EndPatrol();

            // Assert
            Assert.Null(result);
            
            // Verify that StopMonitoring was not called
            _mockGeofenceService.Verify(g => g.StopMonitoring(), Times.Never);
        }

        [Fact]
        public async Task GetPatrolStatus_WithActivePatrol_ShouldReturnStatus()
        {
            // Arrange
            int locationId = 1;
            
            // Start a patrol first
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);
            
            _mockGeofenceService.Setup(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            
            _mockMapService.Setup(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
                
            await _patrolService.StartPatrol(locationId);
            
            // Act
            var result = await _patrolService.GetPatrolStatus(locationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(locationId, result.LocationId);
            Assert.Equal(5, result.TotalCheckpoints);
            Assert.Equal(0, result.VerifiedCheckpoints);
            
            // Verify repository methods were not called (using cached status)
            _mockCheckpointRepository.Verify(r => r.GetCheckpointStatusesAsync(locationId), Times.Never);
        }

        [Fact]
        public async Task GetPatrolStatus_WithInactivePatrol_ShouldCreateNewStatus()
        {
            // Arrange
            int locationId = 1;
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            var testStatuses = new List<CheckpointStatus>
            {
                new CheckpointStatus { CheckpointId = 1, IsVerified = true, VerificationTime = DateTime.UtcNow },
                new CheckpointStatus { CheckpointId = 2, IsVerified = true, VerificationTime = DateTime.UtcNow }
            };
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);
                
            _mockCheckpointRepository.Setup(r => r.GetCheckpointStatusesAsync(locationId))
                .ReturnsAsync(testStatuses);

            // Act
            var result = await _patrolService.GetPatrolStatus(locationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(locationId, result.LocationId);
            Assert.Equal(5, result.TotalCheckpoints);
            Assert.Equal(2, result.VerifiedCheckpoints);
            
            // Verify repository methods were called
            _mockCheckpointRepository.Verify(r => r.GetCheckpointsByLocationIdAsync(locationId), Times.Once);
            _mockCheckpointRepository.Verify(r => r.GetCheckpointStatusesAsync(locationId), Times.Once);
        }

        [Fact]
        public async Task VerifyCheckpoint_WhenInProximity_ShouldVerifyCheckpoint()
        {
            // Arrange
            int locationId = 1;
            int checkpointId = 2;
            
            // Start a patrol first
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            var checkpointToVerify = testCheckpoints.First(c => c.Id == checkpointId);
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);
            
            _mockGeofenceService.Setup(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            
            _mockMapService.Setup(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
                
            _mockMapService.Setup(m => m.UpdateCheckpointStatus(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
                
            _mockCheckpointRepository.Setup(r => r.SaveCheckpointStatusAsync(It.IsAny<CheckpointStatus>()))
                .ReturnsAsync(true);
                
            // Current location is close to checkpoint (within the ProximityThresholdFeet distance)
            var currentLocation = new LocationModel
            {
                Latitude = checkpointToVerify.Latitude,
                Longitude = checkpointToVerify.Longitude
            };
            
            _mockLocationService.Setup(l => l.GetCurrentLocation())
                .ReturnsAsync(currentLocation);
                
            await _patrolService.StartPatrol(locationId);
            
            // Act
            var result = await _patrolService.VerifyCheckpoint(checkpointId);

            // Assert
            Assert.True(result);
            
            // Verify repository method was called
            _mockCheckpointRepository.Verify(r => r.SaveCheckpointStatusAsync(It.Is<CheckpointStatus>(
                s => s.CheckpointId == checkpointId && s.IsVerified)), Times.Once);
                
            _mockMapService.Verify(m => m.UpdateCheckpointStatus(checkpointId, true), Times.Once);
        }

        [Fact]
        public async Task VerifyCheckpoint_WhenNotInProximity_ShouldReturnFalse()
        {
            // Arrange
            int locationId = 1;
            int checkpointId = 2;
            
            // Start a patrol first
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            var checkpointToVerify = testCheckpoints.First(c => c.Id == checkpointId);
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);
            
            _mockGeofenceService.Setup(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            
            _mockMapService.Setup(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
                
            // Current location is FAR from checkpoint (different hemisphere)
            var currentLocation = new LocationModel
            {
                Latitude = -checkpointToVerify.Latitude,
                Longitude = -checkpointToVerify.Longitude
            };
            
            _mockLocationService.Setup(l => l.GetCurrentLocation())
                .ReturnsAsync(currentLocation);
                
            await _patrolService.StartPatrol(locationId);
            
            // Act
            var result = await _patrolService.VerifyCheckpoint(checkpointId);

            // Assert
            Assert.False(result);
            
            // Verify repository method was NOT called
            _mockCheckpointRepository.Verify(r => r.SaveCheckpointStatusAsync(It.IsAny<CheckpointStatus>()), Times.Never);
        }

        [Fact]
        public async Task VerifyCheckpoint_WhenNoPatrolActive_ShouldReturnFalse()
        {
            // Arrange
            int checkpointId = 1;
            
            // No active patrol

            // Act
            var result = await _patrolService.VerifyCheckpoint(checkpointId);

            // Assert
            Assert.False(result);
            
            // Verify repository and location methods were not called
            _mockCheckpointRepository.Verify(r => r.SaveCheckpointStatusAsync(It.IsAny<CheckpointStatus>()), Times.Never);
            _mockLocationService.Verify(l => l.GetCurrentLocation(), Times.Never);
        }

        [Fact]
        public async Task CheckProximity_WhenPatrolActive_ShouldReturnCheckpointsInProximity()
        {
            // Arrange
            int locationId = 1;
            double testLatitude = 34.0522;
            double testLongitude = -118.2437;
            
            // Start a patrol first
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);
            
            _mockGeofenceService.Setup(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            
            _mockMapService.Setup(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
                
            // Set up geofence service to return checkpoints in proximity
            var checkpointsInRange = new List<int> { 1, 3 };
            _mockGeofenceService.Setup(g => g.CheckProximity(testLatitude, testLongitude))
                .ReturnsAsync(checkpointsInRange);
                
            await _patrolService.StartPatrol(locationId);
            
            // Act
            var result = await _patrolService.CheckProximity(testLatitude, testLongitude);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(1, result);
            Assert.Contains(3, result);
            
            // Verify geofence service was called with correct coordinates
            _mockGeofenceService.Verify(g => g.CheckProximity(testLatitude, testLongitude), Times.Once);
        }

        [Fact]
        public async Task CheckProximity_WhenNoPatrolActive_ShouldReturnEmptyList()
        {
            // Arrange
            double testLatitude = 34.0522;
            double testLongitude = -118.2437;
            
            // No active patrol

            // Act
            var result = await _patrolService.CheckProximity(testLatitude, testLongitude);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            
            // Verify geofence service was not called
            _mockGeofenceService.Verify(g => g.CheckProximity(It.IsAny<double>(), It.IsAny<double>()), Times.Never);
        }

        [Fact]
        public async Task OnGeofenceProximityChanged_ShouldRaiseCheckpointProximityChangedEvent()
        {
            // Arrange
            int locationId = 1;
            int checkpointId = 2;
            
            // Start a patrol first
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);
            
            _mockGeofenceService.Setup(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            
            _mockMapService.Setup(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
                
            _mockMapService.Setup(m => m.HighlightCheckpoint(It.IsAny<int>(), It.IsAny<bool>()));
                
            await _patrolService.StartPatrol(locationId);
            
            // Create event handler to capture event
            CheckpointProximityEventArgs capturedArgs = null;
            _patrolService.CheckpointProximityChanged += (sender, args) => capturedArgs = args;
            
            // Create event args to trigger event
            var proximityEventArgs = new CheckpointProximityEventArgs(checkpointId, 25.0, true);
            
            // Act
            // Simulate the geofence service raising the proximity event
            _mockGeofenceService.Raise(g => g.ProximityChanged += null, proximityEventArgs);

            // Assert
            Assert.NotNull(capturedArgs);
            Assert.Equal(checkpointId, capturedArgs.CheckpointId);
            Assert.Equal(25.0, capturedArgs.Distance);
            Assert.True(capturedArgs.IsInRange);
            
            // Verify map service was called to highlight checkpoint
            _mockMapService.Verify(m => m.HighlightCheckpoint(checkpointId, true), Times.Once);
        }

        [Fact]
        public async Task OnLocationChanged_ShouldCheckProximity()
        {
            // Arrange
            int locationId = 1;
            
            // Start a patrol first
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);
            
            _mockGeofenceService.Setup(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            
            _mockMapService.Setup(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
                
            // Set up geofence service for CheckProximity
            _mockGeofenceService.Setup(g => g.CheckProximity(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(new List<int>());
                
            await _patrolService.StartPatrol(locationId);
            
            // Create test location update
            var locationChangedArgs = new LocationChangedEventArgs(
                new LocationModel { Latitude = 34.0522, Longitude = -118.2437 });
            
            // Act
            // Simulate the location service raising the location changed event
            _mockLocationService.Raise(l => l.LocationChanged += null, locationChangedArgs);

            // Assert
            // Need to wait a short time since the event handler is async void
            await Task.Delay(100);
            
            // Verify geofence service was called to check proximity
            _mockGeofenceService.Verify(g => g.CheckProximity(
                It.Is<double>(lat => Math.Abs(lat - 34.0522) < 0.0001),
                It.Is<double>(lon => Math.Abs(lon - (-118.2437)) < 0.0001)
            ), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldEndPatrolAndUnsubscribeFromEvents()
        {
            // Arrange
            int locationId = 1;
            
            // Start a patrol to have something active
            var testCheckpoints = TestDataGenerator.CreateCheckpointModels(5, locationId);
            
            _mockCheckpointRepository.Setup(r => r.GetCheckpointsByLocationIdAsync(locationId))
                .ReturnsAsync(testCheckpoints);
            
            _mockGeofenceService.Setup(g => g.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
            
            _mockGeofenceService.Setup(g => g.StopMonitoring())
                .Returns(Task.CompletedTask);
            
            _mockMapService.Setup(m => m.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()))
                .Returns(Task.CompletedTask);
                
            _patrolService.StartPatrol(locationId).Wait();
            
            // Reset mocks to clear any previous calls
            _mockMapService.Invocations.Clear();
            _mockGeofenceService.Invocations.Clear();
            
            // Act
            _patrolService.Dispose();

            // Assert
            // Check if StopMonitoring was called
            _mockGeofenceService.Verify(g => g.StopMonitoring(), Times.Once);
            
            // To test event unsubscription, try raising the events again
            // and verify they don't trigger any actions
            _mockGeofenceService.Raise(g => g.ProximityChanged += null, 
                new CheckpointProximityEventArgs(1, 10.0, true));
                
            _mockLocationService.Raise(l => l.LocationChanged += null, 
                new LocationChangedEventArgs(new LocationModel { Latitude = 34.0522, Longitude = -118.2437 }));
                
            // Since events were unsubscribed, map/geofence services should not be called again
            _mockMapService.Verify(m => m.HighlightCheckpoint(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
            _mockGeofenceService.Verify(g => g.CheckProximity(It.IsAny<double>(), It.IsAny<double>()), Times.Never);
        }
    }
}