using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Microsoft.Extensions.Logging; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.Models;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.MAUI.IntegrationTests.Services
{
    /// <summary>
    /// Integration tests for the LocationService implementation in the Security Patrol application.
    /// These tests verify that the location tracking functionality works correctly with real service implementations in a controlled test environment.
    /// </summary>
    public class LocationTrackingIntegrationTests : IntegrationTestBase
    {
        private List<LocationModel> _receivedLocations;
        private bool _locationEventReceived;

        /// <summary>
        /// Initializes a new instance of the LocationTrackingIntegrationTests class.
        /// </summary>
        public LocationTrackingIntegrationTests()
        {
            // Call base constructor
            
            // Initialize _receivedLocations as new List<LocationModel>()
            _receivedLocations = new List<LocationModel>();
            
            // Initialize _locationEventReceived as false
            _locationEventReceived = false;
        }

        /// <summary>
        /// Sets up successful location tracking API responses for testing.
        /// </summary>
        [Private]
        private void SetupLocationTrackingSuccessResponse()
        {
            // Create LocationSyncResponse with success status (processed: 10, failed: 0)
            var locationResponse = new LocationSyncResponse
            {
                Processed = 10,
                Failed = 0
            };

            // Call ApiServer.SetupSuccessResponse for /location/batch endpoint with the response
            ApiServer.SetupSuccessResponse("/location/batch", locationResponse);

            // Call ApiServer.SetupSuccessResponse for /location/current endpoint with a sample location
            ApiServer.SetupSuccessResponse("/location/current", new
            {
                Timestamp = DateTime.UtcNow.ToString("o"),
                Latitude = 34.0522,
                Longitude = -118.2437
            });
        }

        /// <summary>
        /// Event handler for location changed events during testing.
        /// </summary>
        [Private]
        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            // Add e.Location to _receivedLocations list
            _receivedLocations.Add(e.Location);

            // Set _locationEventReceived to true
            _locationEventReceived = true;
        }

        /// <summary>
        /// Tests that location tracking can be started successfully.
        /// </summary>
        [Public]
        [Async]
        [Fact]
        public async Task StartTrackingTest()
        {
            // Setup location tracking success responses
            SetupLocationTrackingSuccessResponse();

            // Authenticate the user with AuthenticateAsync()
            await AuthenticateAsync();

            // Clock in the user with ClockInAsync()
            await ClockInAsync();

            // Subscribe to LocationService.LocationChanged event with OnLocationChanged handler
            LocationService.LocationChanged += OnLocationChanged;

            // Call LocationService.StartTracking()
            await LocationService.StartTracking();

            // Assert that LocationService.IsTracking is true
            Assert.True(LocationService.IsTracking);

            // Wait for a short period to allow location updates (Task.Delay)
            await Task.Delay(2000);

            // Assert that at least one location update was received (_locationEventReceived should be true)
            Assert.True(_locationEventReceived);

            // Unsubscribe from LocationChanged event
            LocationService.LocationChanged -= OnLocationChanged;

            // Call LocationService.StopTracking() to clean up
            await LocationService.StopTracking();
        }

        /// <summary>
        /// Tests that location tracking can be stopped successfully.
        /// </summary>
        [Public]
        [Async]
        [Fact]
        public async Task StopTrackingTest()
        {
            // Setup location tracking success responses
            SetupLocationTrackingSuccessResponse();

            // Authenticate the user with AuthenticateAsync()
            await AuthenticateAsync();

            // Clock in the user with ClockInAsync()
            await ClockInAsync();

            // Call LocationService.StartTracking()
            await LocationService.StartTracking();

            // Assert that LocationService.IsTracking is true
            Assert.True(LocationService.IsTracking);

            // Call LocationService.StopTracking()
            await LocationService.StopTracking();

            // Assert that LocationService.IsTracking is false
            Assert.False(LocationService.IsTracking);
        }

        /// <summary>
        /// Tests that the current location can be retrieved successfully.
        /// </summary>
        [Public]
        [Async]
        [Fact]
        public async Task GetCurrentLocationTest()
        {
            // Setup location tracking success responses
            SetupLocationTrackingSuccessResponse();

            // Authenticate the user with AuthenticateAsync()
            await AuthenticateAsync();

            // Call LocationService.GetCurrentLocation()
            LocationModel location = await LocationService.GetCurrentLocation();

            // Assert that the returned location is not null
            Assert.NotNull(location);

            // Assert that the location has valid coordinates (latitude and longitude are not 0)
            Assert.NotEqual(0, location.Latitude);
            Assert.NotEqual(0, location.Longitude);
        }

        /// <summary>
        /// Tests that location tracking starts with clock in and stops with clock out.
        /// </summary>
        [Public]
        [Async]
        [Fact]
        public async Task LocationTrackingWithClockInOutTest()
        {
            // Setup location tracking success responses
            SetupLocationTrackingSuccessResponse();

            // Authenticate the user with AuthenticateAsync()
            await AuthenticateAsync();

            // Assert that LocationService.IsTracking is false initially
            Assert.False(LocationService.IsTracking);

            // Clock in the user with ClockInAsync()
            await ClockInAsync();

            // Assert that LocationService.IsTracking is true after clock in
            Assert.True(LocationService.IsTracking);

            // Clock out the user with ClockOutAsync()
            await ClockOutAsync();

            // Assert that LocationService.IsTracking is false after clock out
            Assert.False(LocationService.IsTracking);
        }

        /// <summary>
        /// Tests that battery optimization settings can be changed.
        /// </summary>
        [Public]
        [Async]
        [Fact]
        public async Task BatteryOptimizationTest()
        {
            // Setup location tracking success responses
            SetupLocationTrackingSuccessResponse();

            // Authenticate the user with AuthenticateAsync()
            await AuthenticateAsync();

            // Clock in the user with ClockInAsync()
            await ClockInAsync();

            // Call LocationService.SetBatteryOptimization(false) to disable optimization
            await LocationService.SetBatteryOptimization(false);

            // Wait for a short period to allow settings to apply
            await Task.Delay(100);

            // Call LocationService.SetBatteryOptimization(true) to enable optimization
            await LocationService.SetBatteryOptimization(true);

            // Assert that no exceptions were thrown during the operation
            // (This test primarily checks that the method executes without errors)

            // Call LocationService.StopTracking() to clean up
            await LocationService.StopTracking();
        }

        /// <summary>
        /// Tests that recent location history can be retrieved.
        /// </summary>
        [Public]
        [Async]
        [Fact]
        public async Task GetRecentLocationsTest()
        {
            // Setup location tracking success responses
            SetupLocationTrackingSuccessResponse();

            // Authenticate the user with AuthenticateAsync()
            await AuthenticateAsync();

            // Clock in the user with ClockInAsync()
            await ClockInAsync();

            // Subscribe to LocationService.LocationChanged event with OnLocationChanged handler
            LocationService.LocationChanged += OnLocationChanged;

            // Call LocationService.StartTracking()
            await LocationService.StartTracking();

            // Wait for a short period to allow location updates (Task.Delay)
            await Task.Delay(2000);

            // Call LocationService.GetRecentLocations(10)
            IEnumerable<LocationModel> recentLocations = await LocationService.GetRecentLocations(10);

            // Assert that the returned collection is not null
            Assert.NotNull(recentLocations);

            // Assert that the collection contains at least one location
            Assert.NotEmpty(recentLocations);

            // Unsubscribe from LocationChanged event
            LocationService.LocationChanged -= OnLocationChanged;

            // Call LocationService.StopTracking() to clean up
            await LocationService.StopTracking();
        }

        /// <summary>
        /// Tests that location data is synchronized with the backend.
        /// </summary>
        [Public]
        [Async]
        [Fact]
        public async Task LocationSyncTest()
        {
            // Setup location tracking success responses
            SetupLocationTrackingSuccessResponse();

            // Authenticate the user with AuthenticateAsync()
            await AuthenticateAsync();

            // Clock in the user with ClockInAsync()
            await ClockInAsync();

            // Call LocationService.StartTracking()
            await LocationService.StartTracking();

            // Wait for a longer period to allow multiple location updates and sync (Task.Delay)
            await Task.Delay(5000);

            // Verify that API server received at least one batch request to /location/batch endpoint
            int requestCount = ApiServer.GetRequestCount("/location/batch");
            Assert.True(requestCount > 0, "Expected at least one request to /location/batch");

            // Call LocationService.StopTracking() to clean up
            await LocationService.StopTracking();
        }

        /// <summary>
        /// Tests that location tracking handles API errors gracefully.
        /// </summary>
        [Public]
        [Async]
        [Fact]
        public async Task LocationTrackingErrorHandlingTest()
        {
            // Setup error response for /location/batch endpoint with 500 status code
            ApiServer.SetupErrorResponse("/location/batch", 500, "Internal Server Error");

            // Authenticate the user with AuthenticateAsync()
            await AuthenticateAsync();

            // Clock in the user with ClockInAsync()
            await ClockInAsync();

            // Call LocationService.StartTracking()
            await LocationService.StartTracking();

            // Wait for a period to allow location updates and sync attempts
            await Task.Delay(3000);

            // Assert that LocationService.IsTracking is still true despite API errors
            Assert.True(LocationService.IsTracking);

            // Call LocationService.StopTracking() to clean up
            await LocationService.StopTracking();
        }
    }
}