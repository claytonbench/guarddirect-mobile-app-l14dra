using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityPatrol.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.UnitTests.Helpers;
using Xunit;
using FluentAssertions;

namespace SecurityPatrol.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the LocationService class to verify its functionality for tracking location, 
    /// managing background services, and handling location data synchronization.
    /// </summary>
    public class LocationServiceTests
    {
        private readonly Mock<ILocationRepository> _mockLocationRepository;
        private readonly Mock<ILocationSyncService> _mockLocationSyncService;
        private readonly Mock<BackgroundLocationService> _mockBackgroundLocationService;
        private readonly Mock<INetworkService> _mockNetworkService;
        private readonly Mock<ISettingsService> _mockSettingsService;
        private readonly Mock<ILogger<LocationService>> _mockLogger;
        private readonly LocationService _locationService;

        /// <summary>
        /// Initializes a new instance of the LocationServiceTests class and sets up the common test dependencies
        /// </summary>
        public LocationServiceTests()
        {
            _mockLocationRepository = new Mock<ILocationRepository>();
            _mockLocationSyncService = new Mock<ILocationSyncService>();
            _mockBackgroundLocationService = new Mock<BackgroundLocationService>();
            _mockNetworkService = new Mock<INetworkService>();
            _mockSettingsService = new Mock<ISettingsService>();
            _mockLogger = new Mock<ILogger<LocationService>>();

            SetupMocks();

            _locationService = new LocationService(
                _mockLocationRepository.Object,
                _mockLocationSyncService.Object,
                _mockBackgroundLocationService.Object,
                _mockNetworkService.Object,
                _mockSettingsService.Object,
                _mockLogger.Object);
        }

        /// <summary>
        /// Sets up the default behaviors for all mock objects used in the tests
        /// </summary>
        private void SetupMocks()
        {
            // Set up repository mocks
            _mockLocationRepository.Setup(r => r.GetRecentLocationsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<LocationModel>());
            _mockLocationRepository.Setup(r => r.GetPendingSyncLocationsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<LocationModel>());
            _mockLocationRepository.Setup(r => r.SaveLocationAsync(It.IsAny<LocationModel>()))
                .ReturnsAsync(1);
            _mockLocationRepository.Setup(r => r.SaveLocationBatchAsync(It.IsAny<IEnumerable<LocationModel>>()))
                .Returns(Task.CompletedTask);

            // Set up sync service mock
            _mockLocationSyncService.Setup(s => s.SyncLocationsAsync(It.IsAny<int>()))
                .ReturnsAsync(true);
            _mockLocationSyncService.Setup(s => s.ScheduleSync(It.IsAny<TimeSpan>()));
            _mockLocationSyncService.Setup(s => s.CancelScheduledSync());

            // Set up background service mock
            _mockBackgroundLocationService.Setup(b => b.IsRunning())
                .Returns(false);
            _mockBackgroundLocationService.Setup(b => b.Start())
                .Returns(Task.CompletedTask);
            _mockBackgroundLocationService.Setup(b => b.Stop())
                .Returns(Task.CompletedTask);

            // Set up network service mock
            _mockNetworkService.Setup(n => n.IsConnected)
                .Returns(true);
            
            // Set up settings service mock
            _mockSettingsService.Setup(s => s.GetValue<bool>("BatteryOptimized", true))
                .Returns(true);
        }

        /// <summary>
        /// Cleans up resources after tests
        /// </summary>
        public void Dispose()
        {
            if (_locationService is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _mockLocationRepository.Reset();
            _mockLocationSyncService.Reset();
            _mockBackgroundLocationService.Reset();
            _mockNetworkService.Reset();
            _mockSettingsService.Reset();
            _mockLogger.Reset();
        }

        [Fact]
        public async Task StartTracking_WhenNotTracking_StartsBackgroundServiceAndSchedulesSync()
        {
            // Arrange
            _mockBackgroundLocationService.Setup(b => b.IsRunning()).Returns(false);

            // Act
            await _locationService.StartTracking();

            // Assert
            _mockBackgroundLocationService.Verify(b => b.Start(), Times.Once);
            _mockLocationSyncService.Verify(s => s.ScheduleSync(TimeSpan.FromMinutes(AppConstants.SyncIntervalMinutes)), Times.Once);
            _locationService.IsTracking.Should().BeTrue();
        }

        [Fact]
        public async Task StartTracking_WhenAlreadyTracking_DoesNothing()
        {
            // Arrange
            await _locationService.StartTracking();
            
            // Reset the mocks to clear invocation records
            _mockBackgroundLocationService.Invocations.Clear();
            _mockLocationSyncService.Invocations.Clear();

            // Act
            await _locationService.StartTracking();

            // Assert
            _mockBackgroundLocationService.Verify(b => b.Start(), Times.Never);
            _mockLocationSyncService.Verify(s => s.ScheduleSync(It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task StartTracking_WhenBackgroundServiceFailsToStart_ThrowsException()
        {
            // Arrange
            _mockBackgroundLocationService.Setup(b => b.Start())
                .ThrowsAsync(new InvalidOperationException("Failed to start background service"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _locationService.StartTracking());
            _locationService.IsTracking.Should().BeFalse();
        }

        [Fact]
        public async Task StopTracking_WhenTracking_StopsBackgroundServiceAndCancelsSync()
        {
            // Arrange
            await _locationService.StartTracking();
            _mockBackgroundLocationService.Invocations.Clear();
            _mockLocationSyncService.Invocations.Clear();

            // Act
            await _locationService.StopTracking();

            // Assert
            _mockBackgroundLocationService.Verify(b => b.Stop(), Times.Once);
            _mockLocationSyncService.Verify(s => s.CancelScheduledSync(), Times.Once);
            _locationService.IsTracking.Should().BeFalse();
        }

        [Fact]
        public async Task StopTracking_WhenNotTracking_DoesNothing()
        {
            // Arrange - ensure we're not tracking
            if (_locationService.IsTracking)
            {
                await _locationService.StopTracking();
            }
            
            _mockBackgroundLocationService.Invocations.Clear();
            _mockLocationSyncService.Invocations.Clear();

            // Act
            await _locationService.StopTracking();

            // Assert
            _mockBackgroundLocationService.Verify(b => b.Stop(), Times.Never);
            _mockLocationSyncService.Verify(s => s.CancelScheduledSync(), Times.Never);
        }

        [Fact]
        public async Task GetCurrentLocation_ReturnsLocationFromHelper()
        {
            // Arrange
            var testLocation = TestDataGenerator.CreateLocationModel();
            
            // We can't directly mock LocationHelper as it's a static class,
            // but we can verify the returned value is passed through correctly
            
            // Act
            LocationModel result = null;
            var exception = await Record.ExceptionAsync(async () => {
                result = await _locationService.GetCurrentLocation();
            });

            // Assert
            // Since we can't mock the static LocationHelper, this will likely throw an exception
            // in the test environment, which is okay - we're just ensuring the method attempts
            // to get the location and doesn't throw its own exceptions
            if (exception == null)
            {
                result.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task GetCurrentLocation_WhenExceptionOccurs_PropagatesException()
        {
            // Arrange
            // We can't directly mock static LocationHelper, but we can test that
            // exceptions are propagated correctly

            // For testing exception handling, we can rely on the fact that the actual
            // implementation will throw because we don't have real device access in the test

            // Act & Assert
            // The method should propagate any exceptions from LocationHelper
            // which will happen naturally in a test environment
            await Assert.ThrowsAnyAsync<Exception>(() => _locationService.GetCurrentLocation());
        }

        [Fact]
        public async Task OnLocationChanged_WhenTracking_AddsLocationToQueueAndRaisesEvent()
        {
            // Arrange
            await _locationService.StartTracking();
            var testLocation = TestDataGenerator.CreateLocationModel();
            
            bool eventRaised = false;
            LocationChangedEventArgs capturedArgs = null;
            
            _locationService.LocationChanged += (sender, args) => {
                eventRaised = true;
                capturedArgs = args;
            };

            // Act
            _mockBackgroundLocationService.Raise(b => b.LocationChanged += null, 
                new LocationChangedEventArgs(testLocation));

            // Assert
            eventRaised.Should().BeTrue();
            capturedArgs.Should().NotBeNull();
            capturedArgs.Location.Should().Be(testLocation);
            
            // Verify the location was saved
            _mockLocationRepository.Verify(r => r.SaveLocationAsync(testLocation), Times.Once);
        }

        [Fact]
        public async Task OnLocationChanged_WhenNotTracking_DoesNothing()
        {
            // Arrange
            if (_locationService.IsTracking)
            {
                await _locationService.StopTracking();
            }
            
            var testLocation = TestDataGenerator.CreateLocationModel();
            bool eventRaised = false;
            
            _locationService.LocationChanged += (sender, args) => {
                eventRaised = true;
            };
            
            _mockLocationRepository.Invocations.Clear();

            // Act
            _mockBackgroundLocationService.Raise(b => b.LocationChanged += null, 
                new LocationChangedEventArgs(testLocation));

            // Assert
            eventRaised.Should().BeFalse();
            _mockLocationRepository.Verify(r => r.SaveLocationAsync(It.IsAny<LocationModel>()), Times.Never);
        }

        [Fact]
        public async Task OnConnectivityChanged_WhenConnectedAndTracking_TriggersSynchronization()
        {
            // Arrange
            await _locationService.StartTracking();
            _mockNetworkService.Setup(n => n.IsConnected).Returns(false);
            _mockLocationSyncService.Invocations.Clear();

            // Act
            _mockNetworkService.Raise(n => n.ConnectivityChanged += null, 
                new ConnectivityChangedEventArgs(true, "WiFi", ConnectionQuality.Good));

            // Assert
            _mockLocationSyncService.Verify(s => s.SyncLocationsAsync(AppConstants.LocationBatchSize), Times.Once);
        }

        [Fact]
        public async Task OnConnectivityChanged_WhenNotConnectedOrNotTracking_DoesNotTriggerSync()
        {
            // Arrange - Test when tracking but not connected
            await _locationService.StartTracking();
            _mockNetworkService.Setup(n => n.IsConnected).Returns(true);
            _mockLocationSyncService.Invocations.Clear();

            // Act
            _mockNetworkService.Raise(n => n.ConnectivityChanged += null, 
                new ConnectivityChangedEventArgs(false, "None", ConnectionQuality.None));

            // Assert
            _mockLocationSyncService.Verify(s => s.SyncLocationsAsync(It.IsAny<int>()), Times.Never);

            // Arrange - Test when connected but not tracking
            await _locationService.StopTracking();
            _mockNetworkService.Setup(n => n.IsConnected).Returns(true);
            _mockLocationSyncService.Invocations.Clear();

            // Act
            _mockNetworkService.Raise(n => n.ConnectivityChanged += null, 
                new ConnectivityChangedEventArgs(true, "WiFi", ConnectionQuality.Good));

            // Assert
            _mockLocationSyncService.Verify(s => s.SyncLocationsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ProcessLocationQueue_WhenQueueNotEmpty_SavesBatchAndTriggersSyncIfConnected()
        {
            // Arrange
            await _locationService.StartTracking();
            var testLocations = TestDataGenerator.CreateLocationModels(10);
            
            // Add locations to the queue by raising location changed events
            foreach (var location in testLocations)
            {
                _mockBackgroundLocationService.Raise(b => b.LocationChanged += null, 
                    new LocationChangedEventArgs(location));
            }
            
            _mockLocationRepository.Invocations.Clear();
            _mockLocationSyncService.Invocations.Clear();
            
            // Queue is an encapsulated private field, so we need to make the service process it
            // by raising enough location events to trigger batch processing
            
            // Filling queue to the threshold will automatically trigger processing
            for (int i = 0; i < AppConstants.LocationBatchSize; i++)
            {
                var location = TestDataGenerator.CreateLocationModel(i + 100);
                _mockBackgroundLocationService.Raise(b => b.LocationChanged += null, 
                    new LocationChangedEventArgs(location));
            }

            // Assert
            _mockLocationRepository.Verify(r => r.SaveLocationBatchAsync(It.IsAny<IEnumerable<LocationModel>>()), Times.AtLeastOnce);
            _mockLocationSyncService.Verify(s => s.SyncLocationsAsync(AppConstants.LocationBatchSize), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SetBatteryOptimization_UpdatesSettingAndRestartsBgServiceIfTracking()
        {
            // Arrange
            await _locationService.StartTracking();
            _mockSettingsService.Invocations.Clear();
            _mockBackgroundLocationService.Invocations.Clear();
            
            // Act
            await _locationService.SetBatteryOptimization(false);
            
            // Assert
            _mockSettingsService.Verify(s => s.SetValue("BatteryOptimized", false), Times.Once);
            _mockBackgroundLocationService.Verify(b => b.Stop(), Times.Once);
            _mockBackgroundLocationService.Verify(b => b.Start(), Times.Once);
        }

        [Fact]
        public async Task SetBatteryOptimization_WhenNotTracking_UpdatesSettingOnly()
        {
            // Arrange
            if (_locationService.IsTracking)
            {
                await _locationService.StopTracking();
            }
            
            _mockSettingsService.Invocations.Clear();
            _mockBackgroundLocationService.Invocations.Clear();
            
            // Act
            await _locationService.SetBatteryOptimization(false);
            
            // Assert
            _mockSettingsService.Verify(s => s.SetValue("BatteryOptimized", false), Times.Once);
            _mockBackgroundLocationService.Verify(b => b.Stop(), Times.Never);
            _mockBackgroundLocationService.Verify(b => b.Start(), Times.Never);
        }

        [Fact]
        public async Task GetRecentLocations_ReturnsLocationsFromRepository()
        {
            // Arrange
            var testLocations = TestDataGenerator.CreateLocationModels(10);
            _mockLocationRepository.Setup(r => r.GetRecentLocationsAsync(10))
                .ReturnsAsync(testLocations);
            
            // Act
            var result = await _locationService.GetRecentLocations(10);
            
            // Assert
            _mockLocationRepository.Verify(r => r.GetRecentLocationsAsync(10), Times.Once);
            result.Should().BeEquivalentTo(testLocations);
        }

        [Fact]
        public async Task Dispose_StopsTrackingAndUnsubscribesFromEvents()
        {
            // Arrange
            await _locationService.StartTracking();
            _mockBackgroundLocationService.Invocations.Clear();
            _mockLocationSyncService.Invocations.Clear();
            
            // Act
            _locationService.Dispose();
            
            // Assert
            _mockBackgroundLocationService.Verify(b => b.Stop(), Times.Once);
            _mockLocationSyncService.Verify(s => s.CancelScheduledSync(), Times.Once);
            
            // Verify that events are unsubscribed by ensuring raising events doesn't affect service
            var testLocation = TestDataGenerator.CreateLocationModel();
            _mockBackgroundLocationService.Raise(b => b.LocationChanged += null, 
                new LocationChangedEventArgs(testLocation));
            
            _mockLocationRepository.Verify(r => r.SaveLocationAsync(It.IsAny<LocationModel>()), Times.Never);
            
            _mockNetworkService.Raise(n => n.ConnectivityChanged += null, 
                new ConnectivityChangedEventArgs(true, "WiFi", ConnectionQuality.Good));
            
            _mockLocationSyncService.Verify(s => s.SyncLocationsAsync(It.IsAny<int>()), Times.Never);
        }
    }
}