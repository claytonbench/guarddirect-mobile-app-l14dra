using System; // System 8.0+
using System.Collections.Generic; // System.Collections.Generic 8.0+
using System.Linq; // System.Linq 8.0+
using System.Threading.Tasks; // System.Threading.Tasks 8.0+
using FluentAssertions; // FluentAssertions 6.11.0
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging 8.0.0
using Moq; // Moq 4.18.4
using SecurityPatrol.MAUI.UnitTests.Setup; // SecurityPatrol.MAUI.UnitTests
using SecurityPatrol.Models; // SecurityPatrol.Models
using SecurityPatrol.Services; // SecurityPatrol.Services
using SecurityPatrol.TestCommon.Data; // SecurityPatrol.TestCommon.Data
using SecurityPatrol.TestCommon.Mocks; // SecurityPatrol.TestCommon.Mocks
using Xunit; // Xunit 2.4.2

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    /// <summary>
    /// Unit tests for the PatrolService class that verify its functionality for patrol management, checkpoint verification, and proximity detection.
    /// </summary>
    public class PatrolServiceTests : TestBase
    {
        private Mock<ICheckpointRepository> MockCheckpointRepository { get; set; }
        private PatrolService PatrolService { get; set; }
        private List<LocationModel> TestLocations { get; set; }
        private List<CheckpointModel> TestCheckpoints { get; set; }

        /// <summary>
        /// Initializes a new instance of the PatrolServiceTests class with test setup
        /// </summary>
        public PatrolServiceTests()
        {
            // Call base constructor to initialize TestBase
            // Initialize MockCheckpointRepository with new Mock<ICheckpointRepository>()
            MockCheckpointRepository = new Mock<ICheckpointRepository>();
            // Initialize TestLocations with a list of test location models
            TestLocations = new List<LocationModel> { TestLocations.GetTestLocationModel(1) };
            // Initialize TestCheckpoints with a list of test checkpoint models
            TestCheckpoints = new List<CheckpointModel> { TestCheckpoints.GetTestCheckpointModel(1) };

            // Setup mock repository methods
            SetupMockRepository();

            // Initialize PatrolService with mocked dependencies
            PatrolService = new PatrolService(
                MockCheckpointRepository.Object,
                MockLocationService.Object,
                MockGeofenceService.Object,
                MockMapService.Object,
                Mock.Of<ILogger<PatrolService>>());
        }

        /// <summary>
        /// Sets up the mock checkpoint repository with test data
        /// </summary>
        private void SetupMockRepository()
        {
            // Setup MockCheckpointRepository.Setup(x => x.GetLocationsAsync()).ReturnsAsync(TestLocations)
            MockCheckpointRepository.Setup(x => x.GetCheckpointsByLocationIdAsync(It.IsAny<int>())).ReturnsAsync(TestCheckpoints);
            MockCheckpointRepository.Setup(x => x.GetCheckpointByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => TestCheckpoints.FirstOrDefault(c => c.Id == id));
            MockCheckpointRepository.Setup(x => x.SaveCheckpointStatusAsync(It.IsAny<CheckpointStatus>())).ReturnsAsync(true);
            MockCheckpointRepository.Setup(x => x.GetCheckpointStatusesAsync(It.IsAny<int>())).ReturnsAsync(new List<CheckpointStatus>());
            MockCheckpointRepository.Setup(x => x.ClearCheckpointStatusesAsync(It.IsAny<int>())).ReturnsAsync(true);
        }

        /// <summary>
        /// Tests that GetLocations returns the expected list of locations
        /// </summary>
        [Fact]
        public async Task Test_GetLocations_ReturnsLocations()
        {
            // Call PatrolService.GetLocations()
            var result = await PatrolService.GetLocations();

            // Assert that the result is not null
            result.Should().NotBeNull();
            // Assert that the result contains the expected number of locations
            result.Should().HaveCount(1);
            // Assert that the result contains locations with the expected properties
            result.First().Id.Should().Be(1);
        }

        /// <summary>
        /// Tests that GetCheckpoints returns the expected list of checkpoints for a valid location ID
        /// </summary>
        [Fact]
        public async Task Test_GetCheckpoints_WithValidLocationId_ReturnsCheckpoints()
        {
            // Call PatrolService.GetCheckpoints(1)
            var result = await PatrolService.GetCheckpoints(1);

            // Assert that the result is not null
            result.Should().NotBeNull();
            // Assert that the result contains the expected number of checkpoints
            result.Should().HaveCount(1);
            // Assert that the result contains checkpoints with the expected properties
            result.First().Id.Should().Be(1);
        }

        /// <summary>
        /// Tests that GetCheckpoints throws an ArgumentException for an invalid location ID
        /// </summary>
        [Fact]
        public async Task Test_GetCheckpoints_WithInvalidLocationId_ThrowsArgumentException()
        {
            // Create a Func that calls PatrolService.GetCheckpoints(0)
            Func<Task> act = async () => await PatrolService.GetCheckpoints(0);

            // Assert that invoking the Func throws an ArgumentException
            await act.Should().ThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Tests that StartPatrol correctly initializes a patrol for a valid location ID
        /// </summary>
        [Fact]
        public async Task Test_StartPatrol_WithValidLocationId_InitializesPatrol()
        {
            // Call PatrolService.StartPatrol(1)
            var result = await PatrolService.StartPatrol(1);

            // Assert that the result is not null
            result.Should().NotBeNull();
            // Assert that PatrolService.IsPatrolActive is true
            PatrolService.IsPatrolActive.Should().BeTrue();
            // Assert that PatrolService.CurrentLocationId is 1
            PatrolService.CurrentLocationId.Should().Be(1);
            // Assert that the result.LocationId is 1
            result.LocationId.Should().Be(1);
            // Assert that the result.TotalCheckpoints matches TestCheckpoints.Count
            result.TotalCheckpoints.Should().Be(TestCheckpoints.Count);
            // Assert that the result.VerifiedCheckpoints is 0
            result.VerifiedCheckpoints.Should().Be(0);
            // Assert that the result.StartTime is approximately DateTime.UtcNow
            result.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            // Assert that the result.EndTime is null
            result.EndTime.Should().BeNull();

            // Verify that MockGeofenceService.StartMonitoring was called
            MockGeofenceService.Verify(x => x.StartMonitoring(It.IsAny<IEnumerable<CheckpointModel>>()), Times.Once);
            // Verify that MockMapService.DisplayCheckpoints was called
            MockMapService.Verify(x => x.DisplayCheckpoints(It.IsAny<IEnumerable<CheckpointModel>>()), Times.Once);
        }

        /// <summary>
        /// Tests that StartPatrol throws an ArgumentException for an invalid location ID
        /// </summary>
        [Fact]
        public async Task Test_StartPatrol_WithInvalidLocationId_ThrowsArgumentException()
        {
            // Create a Func that calls PatrolService.StartPatrol(0)
            Func<Task> act = async () => await PatrolService.StartPatrol(0);

            // Assert that invoking the Func throws an ArgumentException
            await act.Should().ThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Tests that StartPatrol throws an InvalidOperationException when no checkpoints are found for the location
        /// </summary>
        [Fact]
        public async Task Test_StartPatrol_WithNoCheckpoints_ThrowsInvalidOperationException()
        {
            // Setup MockCheckpointRepository to return empty checkpoint list
            MockCheckpointRepository.Setup(x => x.GetCheckpointsByLocationIdAsync(It.IsAny<int>())).ReturnsAsync(new List<CheckpointModel>());

            // Create a Func that calls PatrolService.StartPatrol(1)
            Func<Task> act = async () => await PatrolService.StartPatrol(1);

            // Assert that invoking the Func throws an InvalidOperationException
            await act.Should().ThrowAsync<InvalidOperationException>();

            // Reset MockCheckpointRepository setup
            MockCheckpointRepository.Reset();
            SetupMockRepository();
        }

        /// <summary>
        /// Tests that EndPatrol correctly completes an active patrol
        /// </summary>
        [Fact]
        public async Task Test_EndPatrol_WhenPatrolActive_CompletesPatrol()
        {
            // Call PatrolService.StartPatrol(1) to initialize a patrol
            await PatrolService.StartPatrol(1);

            // Call PatrolService.EndPatrol()
            var result = await PatrolService.EndPatrol();

            // Assert that the result is not null
            result.Should().NotBeNull();
            // Assert that PatrolService.IsPatrolActive is false
            PatrolService.IsPatrolActive.Should().BeFalse();
            // Assert that PatrolService.CurrentLocationId is null
            PatrolService.CurrentLocationId.Should().BeNull();
            // Assert that the result.LocationId is 1
            result.LocationId.Should().Be(1);
            // Assert that the result.EndTime is not null
            result.EndTime.Should().NotBeNull();

            // Verify that MockGeofenceService.StopMonitoring was called
            MockGeofenceService.Verify(x => x.StopMonitoring(), Times.Once);
            // Verify that MockMapService.ClearCheckpoints was called
            MockMapService.Verify(x => x.ClearCheckpoints(), Times.Once);
        }

        /// <summary>
        /// Tests that EndPatrol returns null when no patrol is active
        /// </summary>
        [Fact]
        public async Task Test_EndPatrol_WhenNoPatrolActive_ReturnsNull()
        {
            // Call PatrolService.EndPatrol() without starting a patrol
            var result = await PatrolService.EndPatrol();

            // Assert that the result is null
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that VerifyCheckpoint correctly verifies a checkpoint when in proximity
        /// </summary>
        [Fact]
        public async Task Test_VerifyCheckpoint_WhenInProximity_VerifiesCheckpoint()
        {
            // Call PatrolService.StartPatrol(1) to initialize a patrol
            await PatrolService.StartPatrol(1);

            // Setup MockLocationService to return a location near the checkpoint
            MockLocationService.SetupCurrentLocation(new LocationModel { Latitude = 34.0522, Longitude = -118.2437 });

            // Setup MockGeofenceService to indicate the checkpoint is in proximity
            MockGeofenceService.SetupCheckpointDistance(1, 10);

            // Call PatrolService.VerifyCheckpoint(1)
            var result = await PatrolService.VerifyCheckpoint(1);

            // Assert that the result is true
            result.Should().BeTrue();

            // Verify that MockCheckpointRepository.SaveCheckpointStatusAsync was called
            MockCheckpointRepository.Verify(x => x.SaveCheckpointStatusAsync(It.IsAny<CheckpointStatus>()), Times.Once);
            // Verify that MockMapService.UpdateCheckpointStatus was called
            MockMapService.Verify(x => x.UpdateCheckpointStatus(1, true), Times.Once);
        }

        /// <summary>
        /// Tests that VerifyCheckpoint returns false when not in proximity to the checkpoint
        /// </summary>
        [Fact]
        public async Task Test_VerifyCheckpoint_WhenNotInProximity_ReturnsFalse()
        {
            // Call PatrolService.StartPatrol(1) to initialize a patrol
            await PatrolService.StartPatrol(1);

            // Setup MockLocationService to return a location far from the checkpoint
            MockLocationService.SetupCurrentLocation(new LocationModel { Latitude = 35.0522, Longitude = -119.2437 });

            // Setup MockGeofenceService to indicate the checkpoint is not in proximity
            MockGeofenceService.SetupCheckpointDistance(1, 1000);

            // Call PatrolService.VerifyCheckpoint(1)
            var result = await PatrolService.VerifyCheckpoint(1);

            // Assert that the result is false
            result.Should().BeFalse();

            // Verify that MockCheckpointRepository.SaveCheckpointStatusAsync was not called
            MockCheckpointRepository.Verify(x => x.SaveCheckpointStatusAsync(It.IsAny<CheckpointStatus>()), Times.Never);
        }

        /// <summary>
        /// Tests that VerifyCheckpoint returns false when no patrol is active
        /// </summary>
        [Fact]
        public async Task Test_VerifyCheckpoint_WhenNoPatrolActive_ReturnsFalse()
        {
            // Call PatrolService.VerifyCheckpoint(1) without starting a patrol
            var result = await PatrolService.VerifyCheckpoint(1);

            // Assert that the result is false
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that VerifyCheckpoint returns false for an invalid checkpoint ID
        /// </summary>
        [Fact]
        public async Task Test_VerifyCheckpoint_WithInvalidCheckpointId_ReturnsFalse()
        {
            // Call PatrolService.StartPatrol(1) to initialize a patrol
            await PatrolService.StartPatrol(1);

            // Call PatrolService.VerifyCheckpoint(999) with a non-existent checkpoint ID
            var result = await PatrolService.VerifyCheckpoint(999);

            // Assert that the result is false
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that GetPatrolStatus returns the current patrol status when a patrol is active
        /// </summary>
        [Fact]
        public async Task Test_GetPatrolStatus_WhenPatrolActive_ReturnsStatus()
        {
            // Call PatrolService.StartPatrol(1) to initialize a patrol
            await PatrolService.StartPatrol(1);

            // Call PatrolService.GetPatrolStatus(1)
            var result = await PatrolService.GetPatrolStatus(1);

            // Assert that the result is not null
            result.Should().NotBeNull();
            // Assert that the result.LocationId is 1
            result.LocationId.Should().Be(1);
            // Assert that the result.TotalCheckpoints matches TestCheckpoints.Count
            result.TotalCheckpoints.Should().Be(TestCheckpoints.Count);
            // Assert that the result.VerifiedCheckpoints is 0
            result.VerifiedCheckpoints.Should().Be(0);
        }

        /// <summary>
        /// Tests that GetPatrolStatus returns a new status when no patrol is active
        /// </summary>
        [Fact]
        public async Task Test_GetPatrolStatus_WhenNoPatrolActive_ReturnsNewStatus()
        {
            // Call PatrolService.GetPatrolStatus(1) without starting a patrol
            var result = await PatrolService.GetPatrolStatus(1);

            // Assert that the result is not null
            result.Should().NotBeNull();
            // Assert that the result.LocationId is 1
            result.LocationId.Should().Be(1);
            // Assert that the result.TotalCheckpoints matches TestCheckpoints.Count
            result.TotalCheckpoints.Should().Be(TestCheckpoints.Count);
            // Assert that the result.VerifiedCheckpoints is 0
            result.VerifiedCheckpoints.Should().Be(0);
            // Assert that the result.StartTime is DateTime.MinValue
            result.StartTime.Should().Be(DateTime.MinValue);
            // Assert that the result.EndTime is null
            result.EndTime.Should().BeNull();
        }

        /// <summary>
        /// Tests that CheckProximity returns checkpoints in proximity when a patrol is active
        /// </summary>
        [Fact]
        public async Task Test_CheckProximity_WhenPatrolActive_ReturnsCheckpointsInProximity()
        {
            // Call PatrolService.StartPatrol(1) to initialize a patrol
            await PatrolService.StartPatrol(1);

            // Setup MockGeofenceService.CheckProximity to return a list of checkpoint IDs
            MockGeofenceService.Setup(x => x.CheckProximity(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(new List<int> { 1 });

            // Call PatrolService.CheckProximity(34.0522, -118.2437)
            var result = await PatrolService.CheckProximity(34.0522, -118.2437);

            // Assert that the result is not null
            result.Should().NotBeNull();
            // Assert that the result contains the expected checkpoint IDs
            result.Should().Contain(1);

            // Verify that MockGeofenceService.CheckProximity was called with the correct coordinates
            MockGeofenceService.Verify(x => x.CheckProximity(34.0522, -118.2437), Times.Once);
        }

        /// <summary>
        /// Tests that CheckProximity returns an empty list when no patrol is active
        /// </summary>
        [Fact]
        public async Task Test_CheckProximity_WhenNoPatrolActive_ReturnsEmptyList()
        {
            // Call PatrolService.CheckProximity(34.0522, -118.2437) without starting a patrol
            var result = await PatrolService.CheckProximity(34.0522, -118.2437);

            // Assert that the result is not null
            result.Should().NotBeNull();
            // Assert that the result is empty
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that the CheckpointProximityChanged event is raised when proximity changes
        /// </summary>
        [Fact]
        public async Task Test_ProximityChanged_RaisesEvent()
        {
            // Call PatrolService.StartPatrol(1) to initialize a patrol
            await PatrolService.StartPatrol(1);

            // Create an event monitoring variable
            bool eventRaised = false;
            CheckpointProximityEventArgs eventArgs = null;

            // Subscribe to the PatrolService.CheckpointProximityChanged event
            PatrolService.CheckpointProximityChanged += (sender, e) =>
            {
                eventRaised = true;
                eventArgs = e;
            };

            // Trigger the MockGeofenceService.ProximityChanged event
            MockGeofenceService.SimulateProximityChanged(1, 10, true);

            // Assert that the event was raised
            eventRaised.Should().BeTrue();
            // Assert that the event args contain the expected checkpoint ID and proximity status
            eventArgs.Should().NotBeNull();
            eventArgs.CheckpointId.Should().Be(1);
            eventArgs.IsInRange.Should().BeTrue();
        }

        /// <summary>
        /// Tests that location changes trigger proximity checks
        /// </summary>
        [Fact]
        public async Task Test_LocationChanged_TriggersProximityCheck()
        {
            // Call PatrolService.StartPatrol(1) to initialize a patrol
            await PatrolService.StartPatrol(1);

            // Trigger the MockLocationService.LocationChanged event with new coordinates
            MockLocationService.SimulateLocationChanged(new LocationModel { Latitude = 34.0522, Longitude = -118.2437 });

            // Verify that MockGeofenceService.CheckProximity was called with the new coordinates
            MockGeofenceService.Verify(x => x.CheckProximity(34.0522, -118.2437), Times.Once);
        }

        /// <summary>
        /// Tests that Dispose properly cleans up resources
        /// </summary>
        [Fact]
        public void Test_Dispose_CleansUpResources()
        {
            // Call PatrolService.StartPatrol(1) to initialize a patrol
            PatrolService.StartPatrol(1);

            // Call PatrolService.Dispose()
            PatrolService.Dispose();

            // Assert that PatrolService.IsPatrolActive is false
            PatrolService.IsPatrolActive.Should().BeFalse();

            // Verify that MockGeofenceService.StopMonitoring was called
            MockGeofenceService.Verify(x => x.StopMonitoring(), Times.Once);
        }
    }
}