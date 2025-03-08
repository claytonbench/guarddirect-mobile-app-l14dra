using System; // Version 8.0.0
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using System.Net.Http; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.IntegrationTests.Setup; // Internal import
using SecurityPatrol.Models; // Internal import
using SecurityPatrol.Constants; // Internal import
using SecurityPatrol.TestCommon.Constants; // Internal import

namespace SecurityPatrol.MAUI.IntegrationTests.ApiIntegration
{
    /// <summary>
    /// Integration tests for the location tracking API functionality in the Security Patrol application.
    /// These tests verify that the LocationService and LocationSyncService correctly interact with the backend API
    /// for location data synchronization, using a controlled test environment with mock API responses.
    /// </summary>
    [public]
    public class LocationApiTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the LocationApiTests class.
        /// </summary>
        public LocationApiTests()
        {
            // Call base constructor to initialize the test environment
        }

        /// <summary>
        /// Tests that GetCurrentLocation method correctly retrieves location data from the API.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task GetCurrentLocationShouldReturnLocationFromApi()
        {
            // Set up a mock response for the current location endpoint
            // Create a sample location model with test coordinates
            var expectedLocation = new LocationModel
            {
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                Accuracy = 5.0,
                Timestamp = DateTime.UtcNow
            };

            // Configure ApiServer to return the sample location for LocationCurrent endpoint
            ApiServer.SetupSuccessResponse(ApiEndpoints.LocationCurrent, new
            {
                Timestamp = expectedLocation.Timestamp.ToString("o"),
                Latitude = expectedLocation.Latitude,
                Longitude = expectedLocation.Longitude,
                Accuracy = expectedLocation.Accuracy
            });

            // Call LocationService.GetCurrentLocation()
            var actualLocation = await LocationService.GetCurrentLocation();

            // Assert that the returned location matches the expected values
            actualLocation.Should().NotBeNull();

            // Verify latitude, longitude, and accuracy match the sample data
            actualLocation.Latitude.Should().Be(TestConstants.TestLatitude);
            actualLocation.Longitude.Should().Be(TestConstants.TestLongitude);
        }

        /// <summary>
        /// Tests that StartTracking method correctly initiates location tracking.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task StartTrackingShouldInitiateLocationTracking()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Call LocationService.StartTracking()
            await LocationService.StartTracking();

            // Assert that LocationService.IsTracking is true
            LocationService.IsTracking.Should().BeTrue();

            // Call LocationService.StopTracking() to clean up
            await LocationService.StopTracking();
        }

        /// <summary>
        /// Tests that StopTracking method correctly ends location tracking.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task StopTrackingShouldEndLocationTracking()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Call LocationService.StartTracking()
            await LocationService.StartTracking();

            // Verify tracking is active
            LocationService.IsTracking.Should().BeTrue();

            // Call LocationService.StopTracking()
            await LocationService.StopTracking();

            // Assert that LocationService.IsTracking is false
            LocationService.IsTracking.Should().BeFalse();
        }

        /// <summary>
        /// Tests that location synchronization correctly sends batched location data to the API.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task SyncLocationsShouldSendBatchToApi()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Create a list of test location models
            var locations = new List<LocationModel>
            {
                new LocationModel { Latitude = TestConstants.TestLatitude, Longitude = TestConstants.TestLongitude, Accuracy = 5.0, Timestamp = DateTime.UtcNow },
                new LocationModel { Latitude = TestConstants.TestLatitude + 0.01, Longitude = TestConstants.TestLongitude + 0.01, Accuracy = 5.0, Timestamp = DateTime.UtcNow }
            };

            // Set up a mock response for the location batch endpoint
            // Create a sample LocationSyncResponse with successful IDs
            var syncResponse = new LocationSyncResponse
            {
                SyncedIds = new[] { 1, 2 },
                FailedIds = new int[] { }
            };

            // Configure ApiServer to return the sample response for LocationBatch endpoint
            ApiServer.SetupSuccessResponse(ApiEndpoints.LocationBatch, syncResponse);

            // Seed the database with test location records
            await SeedLocationRecordsAsync(locations);

            // Get the LocationSyncService from the service provider
            var locationSyncService = ServiceProvider.GetService<ILocationSyncService>();

            // Call LocationSyncService.SyncLocationsAsync() with a batch size
            bool syncResult = await locationSyncService.SyncLocationsAsync(locations.Count);

            // Assert that the sync operation returns true (success)
            syncResult.Should().BeTrue();

            // Verify that the database records are marked as synced
            // TODO: Implement database verification logic
        }

        /// <summary>
        /// Tests that location synchronization correctly handles API errors.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task SyncLocationsShouldHandleApiErrors()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Set up a mock error response for the location batch endpoint
            // Configure ApiServer to return a 500 error for LocationBatch endpoint
            ApiServer.SetupErrorResponse(ApiEndpoints.LocationBatch, 500, "Internal Server Error");

            // Seed the database with test location records
            var locations = new List<LocationModel>
            {
                new LocationModel { Latitude = TestConstants.TestLatitude, Longitude = TestConstants.TestLongitude, Accuracy = 5.0, Timestamp = DateTime.UtcNow },
                new LocationModel { Latitude = TestConstants.TestLatitude + 0.01, Longitude = TestConstants.TestLongitude + 0.01, Accuracy = 5.0, Timestamp = DateTime.UtcNow }
            };
            await SeedLocationRecordsAsync(locations);

            // Get the LocationSyncService from the service provider
            var locationSyncService = ServiceProvider.GetService<ILocationSyncService>();

            // Call LocationSyncService.SyncLocationsAsync() with a batch size
            bool syncResult = await locationSyncService.SyncLocationsAsync(locations.Count);

            // Assert that the sync operation returns false (failure)
            syncResult.Should().BeFalse();

            // Verify that the database records are still marked as not synced
            // TODO: Implement database verification logic
        }

        /// <summary>
        /// Tests that location synchronization correctly handles partial success responses.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task SyncLocationsShouldHandlePartialSuccess()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Create a list of test location models
            var locations = new List<LocationModel>
            {
                new LocationModel { Id = 1, Latitude = TestConstants.TestLatitude, Longitude = TestConstants.TestLongitude, Accuracy = 5.0, Timestamp = DateTime.UtcNow },
                new LocationModel { Id = 2, Latitude = TestConstants.TestLatitude + 0.01, Longitude = TestConstants.TestLongitude + 0.01, Accuracy = 5.0, Timestamp = DateTime.UtcNow },
                new LocationModel { Id = 3, Latitude = TestConstants.TestLatitude + 0.02, Longitude = TestConstants.TestLongitude + 0.02, Accuracy = 5.0, Timestamp = DateTime.UtcNow }
            };

            // Set up a mock response for the location batch endpoint
            // Create a sample LocationSyncResponse with some successful IDs and some failed IDs
            var syncResponse = new LocationSyncResponse
            {
                SyncedIds = new[] { 1, 3 },
                FailedIds = new[] { 2 }
            };

            // Configure ApiServer to return the sample response for LocationBatch endpoint
            ApiServer.SetupSuccessResponse(ApiEndpoints.LocationBatch, syncResponse);

            // Seed the database with test location records
            await SeedLocationRecordsAsync(locations);

            // Get the LocationSyncService from the service provider
            var locationSyncService = ServiceProvider.GetService<ILocationSyncService>();

            // Call LocationSyncService.SyncLocationsAsync() with a batch size
            bool syncResult = await locationSyncService.SyncLocationsAsync(locations.Count);

            // Assert that the sync operation returns false (partial failure)
            syncResult.Should().BeFalse();

            // Verify that successful records are marked as synced
            // Verify that failed records are still marked as not synced
            // TODO: Implement database verification logic
        }

        /// <summary>
        /// Tests that the LocationChanged event fires when location updates are received.
        /// </summary>
        [Fact]
        [public]
        [async]
        public async Task LocationChangedEventShouldFireWhenLocationUpdates()
        {
            // Authenticate the user using AuthenticateAsync()
            await AuthenticateAsync();

            // Create a TaskCompletionSource to wait for the event
            var tcs = new TaskCompletionSource<LocationModel>();

            // Subscribe to the LocationChanged event
            EventHandler<LocationChangedEventArgs> handler = (sender, e) =>
            {
                tcs.SetResult(e.Location);
            };
            LocationService.LocationChanged += handler;

            // Call LocationService.StartTracking()
            await LocationService.StartTracking();

            // Simulate a location update through the test framework
            var expectedLocation = new LocationModel
            {
                Latitude = TestConstants.TestLatitude + 0.01,
                Longitude = TestConstants.TestLongitude + 0.01,
                Accuracy = 5.0,
                Timestamp = DateTime.UtcNow
            };

            // Wait for the event to fire with a timeout
            var task = tcs.Task;
            LocationService.SimulateLocationUpdate(expectedLocation);

            // Wait for the event to fire with a timeout
            var actualLocation = await task.TimeoutAfter(TimeSpan.FromSeconds(5));

            // Assert that the event fired with the expected location data
            actualLocation.Should().NotBeNull();
            actualLocation.Latitude.Should().Be(expectedLocation.Latitude);
            actualLocation.Longitude.Should().Be(expectedLocation.Longitude);

            // Unsubscribe from the event
            LocationService.LocationChanged -= handler;

            // Call LocationService.StopTracking() to clean up
            await LocationService.StopTracking();
        }

        /// <summary>
        /// Helper method to create test location data.
        /// </summary>
        /// <param name="count">The number of location models to create.</param>
        /// <returns>A list of test location models</returns>
        [private]
        private List<LocationModel> SetupTestLocationData(int count)
        {
            // Create a new List<LocationModel>
            var locations = new List<LocationModel>();

            // Generate 'count' number of location models with test data
            for (int i = 0; i < count; i++)
            {
                // Set latitude, longitude, accuracy, and timestamp for each model
                var location = new LocationModel
                {
                    Latitude = TestConstants.TestLatitude + i * 0.001,
                    Longitude = TestConstants.TestLongitude + i * 0.001,
                    Accuracy = 5.0,
                    Timestamp = DateTime.UtcNow
                };
                locations.Add(location);
            }

            // Return the list of test location models
            return locations;
        }

        /// <summary>
        /// Helper method to seed the database with test location records.
        /// </summary>
        /// <param name="locations">The list of location models to seed.</param>
        [private]
        [async]
        private async Task SeedLocationRecordsAsync(List<LocationModel> locations)
        {
            // Get the ILocationRepository from the service provider
            var repository = ServiceProvider.GetService<ILocationRepository>();

            // Call repository.SaveLocationBatchAsync() with the provided locations
            await repository.SaveLocationBatchAsync(locations);

            // Return when the operation completes
        }
    }

    // Extension method for simulating location updates
    public static class LocationServiceExtensions
    {
        public static void SimulateLocationUpdate(this ILocationService locationService, LocationModel location)
        {
            locationService.LocationChanged?.Invoke(locationService, new LocationChangedEventArgs(location));
        }

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }
    }
}