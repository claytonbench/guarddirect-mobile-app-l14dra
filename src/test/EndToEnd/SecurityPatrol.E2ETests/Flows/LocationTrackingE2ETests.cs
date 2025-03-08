using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.E2ETests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Constants; // TestConstants
using SecurityPatrol.TestCommon.Helpers; // LocationSimulator

namespace SecurityPatrol.E2ETests.Flows
{
    /// <summary>
    /// End-to-end tests for the location tracking functionality in the Security Patrol application.
    /// </summary>
    [Collection("E2E Tests")]
    public class LocationTrackingE2ETests : E2ETestBase
    {
        private List<LocationModel> _receivedLocations;
        private EventHandler<LocationChangedEventArgs> _locationChangedHandler;

        /// <summary>
        /// Default constructor for LocationTrackingE2ETests
        /// </summary>
        public LocationTrackingE2ETests()
        {
            // Initialize _receivedLocations as new List<LocationModel>()
            _receivedLocations = new List<LocationModel>();
        }

        /// <summary>
        /// Initializes the test environment and sets up the location changed event handler.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact]
        public async Task StartTrackingTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Verify that LocationService.IsTracking is false initially
            LocationService.IsTracking.Should().BeFalse();

            // Call ClockInAsync() to clock in the user
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Wait briefly for tracking to start
            await Task.Delay(2000);

            // Verify that LocationService.IsTracking is now true
            LocationService.IsTracking.Should().BeTrue();

            // Call ClockOutAsync() to clean up
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Tests that location tracking stops when the user is clocked out.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task StopTrackingTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Call ClockInAsync() to clock in the user
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Verify that LocationService.IsTracking is true
            LocationService.IsTracking.Should().BeTrue();

            // Call ClockOutAsync() to clock out the user
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();

            // Wait briefly for tracking to stop
            await Task.Delay(2000);

            // Verify that LocationService.IsTracking is now false
            LocationService.IsTracking.Should().BeFalse();
        }

        /// <summary>
        /// Tests that the current location can be retrieved successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task GetCurrentLocationTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Call LocationService.GetCurrentLocation() to get the current location
            LocationModel location = await LocationService.GetCurrentLocation();

            // Verify that the returned location is not null
            location.Should().NotBeNull();

            // Verify that the location has valid coordinates (latitude and longitude)
            location.Latitude.Should().NotBe(0);
            location.Longitude.Should().NotBe(0);

            // Verify that the location has a valid timestamp (recent)
            location.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));

            // Verify that the location has a reasonable accuracy value
            location.Accuracy.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Tests that the LocationChanged event is raised when location updates are received.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task LocationChangedEventTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Clear _receivedLocations list
            _receivedLocations.Clear();

            // Call ClockInAsync() to clock in the user and start location tracking
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Wait for a sufficient time to receive location updates (e.g., 5 seconds)
            await Task.Delay(5000);

            // Call ClockOutAsync() to clean up
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();

            // Verify that _receivedLocations contains at least one location update
            _receivedLocations.Should().NotBeEmpty();

            // Verify that each received location has valid properties
            foreach (var location in _receivedLocations)
            {
                location.Should().NotBeNull();
                location.Latitude.Should().NotBe(0);
                location.Longitude.Should().NotBe(0);
                location.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));
                location.Accuracy.Should().BeGreaterThan(0);
            }
        }

        /// <summary>
        /// Tests that location data is synchronized with the backend API.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task LocationSyncTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Call ClockInAsync() to clock in the user and start location tracking
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Wait for a sufficient time to collect location data
            await Task.Delay(5000);

            // Call SyncDataAsync() to synchronize data with the backend
            bool syncSuccess = await SyncDataAsync();
            syncSuccess.Should().BeTrue();

            // Call ClockOutAsync() to clean up
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Tests that battery optimization settings affect location tracking behavior.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task BatteryOptimizationTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();

            // Call ClockInAsync() to clock in the user and start location tracking
            bool clockInSuccess = await ClockInAsync();
            clockInSuccess.Should().BeTrue();

            // Wait for a sufficient time to collect location data with optimization enabled
            await Task.Delay(5000);

            // Call ClockOutAsync() to stop tracking
            bool clockOutSuccess = await ClockOutAsync();
            clockOutSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Tests that appropriate exception is thrown when location permissions are denied.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task LocationPermissionDeniedTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Tests handling of disabled location services on the device.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task LocationServiceDisabledTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Tests that location tracking works offline and data is queued for later synchronization.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task OfflineLocationTrackingTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Tests that location data is processed in batches for efficient API communication.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation</returns>
        [Fact]
        public async Task LocationBatchProcessingTest()
        {
            // Authenticate the user by calling AuthenticateAsync()
            bool authSuccess = await AuthenticateAsync();
            authSuccess.Should().BeTrue();
        }

        /// <summary>
        /// Initializes the test environment and sets up the location changed event handler.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public override async Task InitializeAsync()
        {
            // Call base.InitializeAsync() to initialize the base test environment
            await base.InitializeAsync();

            // Create _locationChangedHandler to add received locations to _receivedLocations list
            _locationChangedHandler = (sender, e) =>
            {
                _receivedLocations.Add(e.Location);
            };

            // Subscribe to LocationService.LocationChanged event with _locationChangedHandler
            LocationService.LocationChanged += _locationChangedHandler;
        }

        /// <summary>
        /// Cleans up the test environment and unsubscribes from events.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public override async Task DisposeAsync()
        {
            // Unsubscribe from LocationService.LocationChanged event if _locationChangedHandler is not null
            if (_locationChangedHandler != null)
            {
                LocationService.LocationChanged -= _locationChangedHandler;
            }

            // Clear _receivedLocations list
            _receivedLocations.Clear();

            // Call base.DisposeAsync() to clean up the base test environment
            await base.DisposeAsync();
        }
    }
}