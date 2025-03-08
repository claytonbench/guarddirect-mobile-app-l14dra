# src/test/MAUI/SecurityPatrol.MAUI.UnitTests/Services/LocationServiceTests.cs
using System; // System 8.0+
using System.Collections.Generic; // System.Collections.Generic 8.0+
using System.Threading.Tasks; // System.Threading.Tasks 8.0+
using Xunit; // xunit 2.4.2
using Moq; // Moq 4.18.4
using FluentAssertions; // FluentAssertions 6.11.0
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging 8.0.0
using SecurityPatrol.MAUI.UnitTests.Setup; // TestBase
using SecurityPatrol.Services; // LocationService, ILocationService
using SecurityPatrol.Models; // LocationModel, LocationChangedEventArgs
using SecurityPatrol.TestCommon.Data; // TestLocations
using SecurityPatrol.Models; // ConnectivityChangedEventArgs

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    /// <summary>
    /// Unit tests for the LocationService class to verify its functionality for GPS location tracking.
    /// </summary>
    public class LocationServiceTests : TestBase, IDisposable
    {
        private Mock<ILocationRepository> MockLocationRepository { get; set; }
        private Mock<ILocationSyncService> MockLocationSyncService { get; set; }
        private Mock<BackgroundLocationService> MockBackgroundService { get; set; }
        private Mock<INetworkService> MockNetworkService { get; set; }
        private Mock<ISettingsService> MockSettingsService { get; set; }
        private Mock<ILogger<LocationService>> MockLogger { get; set; }
        private LocationService LocationService { get; set; }

        /// <summary>
        /// Initializes a new instance of the LocationServiceTests class with test setup
        /// </summary>
        public LocationServiceTests()
        {
            Setup();
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        [Fact]
        public void Setup()
        {
            MockLocationRepository = new Mock<ILocationRepository>();
            MockLocationSyncService = new Mock<ILocationSyncService>();
            MockBackgroundService = new Mock<BackgroundLocationService>();
            MockNetworkService = new Mock<INetworkService>();
            MockSettingsService = new Mock<ISettingsService>();
            MockLogger = new Mock<ILogger<LocationService>>();

            MockLocationRepository.Setup(x => x.SaveLocationAsync(It.IsAny<LocationModel>())).ReturnsAsync(1);
            MockLocationRepository.Setup(x => x.SaveLocationBatchAsync(It.IsAny<IEnumerable<LocationModel>>())).Returns(Task.CompletedTask);
            MockLocationRepository.Setup(x => x.GetRecentLocationsAsync(It.IsAny<int>>())).ReturnsAsync(new List<LocationModel> { TestLocations.DefaultLocationModel });

            MockLocationSyncService.Setup(x => x.SyncLocationsAsync(It.IsAny<int>())).ReturnsAsync(true);

            MockBackgroundService.Setup(x => x.Start()).Returns(Task.CompletedTask);
            MockBackgroundService.Setup(x => x.Stop()).Returns(Task.CompletedTask);
            MockBackgroundService.Setup(x => x.IsRunning()).Returns(false);

            MockNetworkService.Setup(x => x.IsConnected).Returns(true);

            MockSettingsService.Setup(x => x.GetValue<bool>(It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            LocationService = new LocationService(MockLocationRepository.Object, MockLocationSyncService.Object, MockBackgroundService.Object, MockNetworkService.Object, MockSettingsService.Object, MockLogger.Object);
        }

        /// <summary>
        /// Cleans up resources after each test
        /// </summary>
        public void Dispose()
        {
            Cleanup();
        }

        /// <summary>
        /// Tests that StartTracking starts the background service
        /// </summary>
        [Fact]
        public async Task StartTracking_ShouldStartBackgroundService()
        {
            // Act
            await LocationService.StartTracking();

            // Assert
            MockBackgroundService.Verify(x => x.Start(), Times.Once());
            LocationService.IsTracking.Should().BeTrue();
        }

        /// <summary>
        /// Tests that StartTracking doesn't restart tracking if already tracking
        /// </summary>
        [Fact]
        public async Task StartTracking_WhenAlreadyTracking_ShouldNotStartAgain()
        {
            // Arrange
            MockBackgroundService.Setup(x => x.IsRunning()).Returns(true);

            // Act
            await LocationService.StartTracking();

            // Assert
            MockBackgroundService.Verify(x => x.Start(), Times.Never());
            LocationService.IsTracking.Should().BeTrue();
        }

        /// <summary>
        /// Tests that StopTracking stops the background service
        /// </summary>
        [Fact]
        public async Task StopTracking_ShouldStopBackgroundService()
        {
            // Arrange
            MockBackgroundService.Setup(x => x.IsRunning()).Returns(true);

            // Act
            await LocationService.StopTracking();

            // Assert
            MockBackgroundService.Verify(x => x.Stop(), Times.Once());
            LocationService.IsTracking.Should().BeFalse();
        }

        /// <summary>
        /// Tests that StopTracking does nothing if not tracking
        /// </summary>
        [Fact]
        public async Task StopTracking_WhenNotTracking_ShouldDoNothing()
        {
            // Arrange
            MockBackgroundService.Setup(x => x.IsRunning()).Returns(false);

            // Act
            await LocationService.StopTracking();

            // Assert
            MockBackgroundService.Verify(x => x.Stop(), Times.Never());
            LocationService.IsTracking.Should().BeFalse();
        }

        /// <summary>
        /// Tests that GetCurrentLocation returns a valid location
        /// </summary>
        [Fact]
        public async Task GetCurrentLocation_ShouldReturnLocation()
        {
            // Arrange
            var testLocation = TestLocations.DefaultLocationModel;

            // Act
            var result = await LocationService.GetCurrentLocation();

            // Assert
            result.Should().NotBeNull();
            result.Latitude.Should().Be(testLocation.Latitude);
            result.Longitude.Should().Be(testLocation.Longitude);
        }

        /// <summary>
        /// Tests that location changes from the background service raise the LocationChanged event
        /// </summary>
        [Fact]
        public async Task OnLocationChanged_ShouldRaiseEvent()
        {
            // Arrange
            MockBackgroundService.Setup(x => x.IsRunning()).Returns(true);
            await LocationService.StartTracking();

            var testLocation = new LocationModel { Latitude = 123.456, Longitude = 789.012 };
            LocationChangedEventArgs receivedEventArgs = null;

            LocationService.LocationChanged += (sender, e) =>
            {
                receivedEventArgs = e;
            };

            // Act
            MockBackgroundService.Raise(x => x.LocationChanged += null, new LocationChangedEventArgs(testLocation));

            // Assert
            receivedEventArgs.Should().NotBeNull();
            receivedEventArgs.Location.Should().BeEquivalentTo(testLocation);
        }

        /// <summary>
        /// Tests that location changes are added to the queue for batch processing
        /// </summary>
        [Fact]
        public async Task OnLocationChanged_ShouldAddLocationToQueue()
        {
            // Arrange
            MockBackgroundService.Setup(x => x.IsRunning()).Returns(true);
            await LocationService.StartTracking();

            var testLocation = new LocationModel { Latitude = 123.456, Longitude = 789.012 };

            // Act
            MockBackgroundService.Raise(x => x.LocationChanged += null, new LocationChangedEventArgs(testLocation));

            // Assert
            MockLocationRepository.Verify(x => x.SaveLocationAsync(It.IsAny<LocationModel>()), Times.Once());
        }

        /// <summary>
        /// Tests that connectivity changes trigger synchronization when connected
        /// </summary>
        [Fact]
        public async Task OnConnectivityChanged_WhenConnected_ShouldTriggerSync()
        {
            // Arrange
            MockBackgroundService.Setup(x => x.IsRunning()).Returns(true);
            await LocationService.StartTracking();
            MockNetworkService.Setup(x => x.IsConnected).Returns(true);

            // Act
            MockNetworkService.Raise(x => x.ConnectivityChanged += null, new ConnectivityChangedEventArgs(true, "WiFi", ConnectionQuality.Excellent));

            // Assert
            MockLocationSyncService.Verify(x => x.SyncLocationsAsync(It.IsAny<int>()), Times.Once());
        }

        /// <summary>
        /// Tests that connectivity changes don't trigger synchronization when disconnected
        /// </summary>
        [Fact]
        public async Task OnConnectivityChanged_WhenDisconnected_ShouldNotTriggerSync()
        {
            // Arrange
            MockBackgroundService.Setup(x => x.IsRunning()).Returns(true);
            await LocationService.StartTracking();
            MockNetworkService.Setup(x => x.IsConnected).Returns(false);

            // Act
            MockNetworkService.Raise(x => x.ConnectivityChanged += null, new ConnectivityChangedEventArgs(false, "None", ConnectionQuality.None));

            // Assert
            MockLocationSyncService.Verify(x => x.SyncLocationsAsync(It.IsAny<int>()), Times.Never());
        }

        /// <summary>
        /// Tests that SetBatteryOptimization updates the setting
        /// </summary>
        [Fact]
        public async Task SetBatteryOptimization_ShouldUpdateSetting()
        {
            // Act
            await LocationService.SetBatteryOptimization(true);

            // Assert
            MockSettingsService.Verify(x => x.SetValue(It.IsAny<string>(), true), Times.Once());
        }

        /// <summary>
        /// Tests that SetBatteryOptimization restarts the background service when tracking
        /// </summary>
        [Fact]
        public async Task SetBatteryOptimization_WhenTracking_ShouldRestartBackgroundService()
        {
            // Arrange
            MockBackgroundService.Setup(x => x.IsRunning()).Returns(true);
            await LocationService.StartTracking();

            // Act
            await LocationService.SetBatteryOptimization(false);

            // Assert
            MockBackgroundService.Verify(x => x.Stop(), Times.Once());
            MockBackgroundService.Verify(x => x.Start(), Times.Once());
        }

        /// <summary>
        /// Tests that GetRecentLocations returns locations from the repository
        /// </summary>
        [Fact]
        public async Task GetRecentLocations_ShouldReturnLocationsFromRepository()
        {
            // Arrange
            var testLocations = new List<LocationModel> { TestLocations.DefaultLocationModel };
            MockLocationRepository.Setup(x => x.GetRecentLocationsAsync(It.IsAny<int>>())).ReturnsAsync(testLocations);

            // Act
            var result = await LocationService.GetRecentLocations(10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(testLocations.Count);
            MockLocationRepository.Verify(x => x.GetRecentLocationsAsync(10), Times.Once());
        }
    }
}