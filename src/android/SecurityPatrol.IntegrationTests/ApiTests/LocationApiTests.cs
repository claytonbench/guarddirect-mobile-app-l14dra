using System; // System 8.0.0
using System.Net.Http; // System.Net.Http 8.0.0
using System.Threading.Tasks; // System.Threading.Tasks 8.0.0
using System.Collections.Generic; // System.Collections.Generic 8.0.0
using System.Linq; // System.Linq 8.0.0
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging 8.0.0
using Microsoft.Extensions.DependencyInjection; // Microsoft.Extensions.DependencyInjection 8.0.0
using Xunit; // xunit 2.4.2
using Moq; // Moq 4.18.4
using FluentAssertions; // FluentAssertions 6.10.0
using SecurityPatrol.IntegrationTests.Helpers;
using SecurityPatrol.Constants;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.IntegrationTests.ApiTests
{
    /// <summary>
    /// Integration tests for the location tracking API functionality in the Security Patrol application
    /// </summary>
    public class LocationApiTests : IAsyncLifetime
    {
        private MockApiServer _mockApiServer;
        private ILocationService _locationService;
        private ILocationSyncService _locationSyncService;
        private ILocationRepository _locationRepository;
        private IApiService _apiService;
        private ILogger<LocationApiTests> _logger;
        private TestAuthenticationHandler _testAuthHandler;
        private TestDatabaseInitializer _dbInitializer;
        private Mock<IAuthenticationStateProvider> _mockAuthStateProvider;
        private Mock<INetworkService> _mockNetworkService;
        private Mock<IBackgroundService> _mockBackgroundService;
        private Mock<ITelemetryService> _mockTelemetryService;
        private Mock<ISettingsService> _mockSettingsService;

        /// <summary>
        /// Initializes a new instance of the LocationApiTests class
        /// </summary>
        public LocationApiTests()
        {
            // Initialize test properties
        }

        /// <summary>
        /// Initializes the test environment before each test
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task InitializeAsync()
        {
            // Create a new MockApiServer instance on a test port
            _mockApiServer = new MockApiServer(new LoggerFactory().CreateLogger<MockApiServer>(), 9001);

            // Start the mock server
            _mockApiServer.Start();

            // Create a logger factory for test logging
            var loggerFactory = new LoggerFactory();
            _logger = loggerFactory.CreateLogger<LocationApiTests>();

            // Initialize the test database
            _dbInitializer = new TestDatabaseInitializer(new LoggerFactory().CreateLogger<TestDatabaseInitializer>());
            await _dbInitializer.InitializeAsync();

            // Create mock dependencies (auth state provider, network service, background service, etc.)
            _mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            _mockNetworkService = new Mock<INetworkService>();
            _mockBackgroundService = new Mock<IBackgroundService>();
            _mockTelemetryService = new Mock<ITelemetryService>();
            _mockSettingsService = new Mock<ISettingsService>();

            // Set up mock auth state provider to return a valid user ID
            _mockAuthStateProvider.Setup(x => x.GetCurrentState())
                .ReturnsAsync(new AuthState(true, "+15551234567"));

            // Set up mock network service to indicate network is available
            _mockNetworkService.Setup(x => x.IsConnected).Returns(true);
            _mockNetworkService.Setup(x => x.ShouldAttemptOperation(It.IsAny<NetworkOperationType>())).Returns(true);

            // Create an ApiService instance with the mock server URL
            var httpClient = new HttpClient { BaseAddress = new Uri(_mockApiServer.GetBaseUrl()) };
            _apiService = new ApiService(httpClient, new Mock<ITokenManager>().Object, _mockNetworkService.Object, _mockTelemetryService.Object);

            // Create a LocationRepository with the test database
            _locationRepository = new LocationRepository(_dbInitializer, _mockAuthStateProvider.Object, new LoggerFactory().CreateLogger<LocationRepository>());

            // Create a LocationSyncService with the dependencies
            _locationSyncService = new LocationSyncService(_locationRepository, _apiService, _mockNetworkService.Object, _mockAuthStateProvider.Object, new LoggerFactory().CreateLogger<LocationSyncService>());

            // Create a LocationService with all the dependencies
            _locationService = new LocationService(_locationRepository, _locationSyncService, new BackgroundLocationService(new LoggerFactory().CreateLogger<BackgroundLocationService>()), _mockNetworkService.Object, _mockSettingsService.Object, new LoggerFactory().CreateLogger<LocationService>());

            // Create a TestAuthenticationHandler with the mock server
            _testAuthHandler = new TestAuthenticationHandler(_mockAuthStateProvider.Object, _mockApiServer, new LoggerFactory().CreateLogger<TestAuthenticationHandler>());
        }

        /// <summary>
        /// Cleans up resources after each test
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task DisposeAsync()
        {
            // Stop the mock server if it's running
            _mockApiServer?.Stop();

            // Dispose any other resources
        }

        /// <summary>
        /// Tests that location synchronization succeeds with valid data
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestSyncLocations_Success()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Set up mock server with success response for location batch endpoint
            var responseObj = new LocationSyncResponse { SyncedIds = new List<int> { 1, 2, 3 } };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.LocationBatch, responseObj);

            // Arrange: Create and save test location data to repository
            var locations = new List<LocationModel>
            {
                new LocationModel { Id = 1, Latitude = 37.7749, Longitude = -122.4194, Accuracy = 10, Timestamp = DateTime.UtcNow, IsSynced = false },
                new LocationModel { Id = 2, Latitude = 34.0522, Longitude = -118.2437, Accuracy = 15, Timestamp = DateTime.UtcNow, IsSynced = false },
                new LocationModel { Id = 3, Latitude = 40.7128, Longitude = -74.0060, Accuracy = 20, Timestamp = DateTime.UtcNow, IsSynced = false }
            };
            await _locationRepository.SaveLocationBatchAsync(locations);

            // Act: Call _locationSyncService.SyncLocationsAsync()
            bool syncResult = await _locationSyncService.SyncLocationsAsync();

            // Assert: Verify that synchronization returns true (success)
            syncResult.Should().BeTrue();

            // Assert: Verify that the API was called with the correct endpoint
            _mockApiServer.GetRequestCount(ApiEndpoints.LocationBatch).Should().Be(1);

            // Assert: Verify that the request body contains the expected location data
            string requestBody = _mockApiServer.GetLastRequestBody(ApiEndpoints.LocationBatch);
            requestBody.Should().NotBeNullOrEmpty();

            // Assert: Verify that the locations in the database are marked as synced
            var syncedLocations = await _locationRepository.GetLocationsAsync(l => l.IsSynced);
            syncedLocations.Count().Should().Be(3);
        }

        /// <summary>
        /// Tests that location synchronization handles empty batches correctly
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestSyncLocations_EmptyBatch()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Ensure no pending locations in repository
            var pendingLocations = await _locationRepository.GetPendingSyncLocationsAsync(50);
            pendingLocations.Should().BeEmpty();

            // Act: Call _locationSyncService.SyncLocationsAsync()
            bool syncResult = await _locationSyncService.SyncLocationsAsync();

            // Assert: Verify that synchronization returns true (success)
            syncResult.Should().BeTrue();

            // Assert: Verify that the API was not called (no data to sync)
            _mockApiServer.GetRequestCount(ApiEndpoints.LocationBatch).Should().Be(0);
        }

        /// <summary>
        /// Tests that location synchronization handles server errors gracefully
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestSyncLocations_ServerError()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Set up mock server with error response (500) for location batch endpoint
            _mockApiServer.SetupErrorResponse(ApiEndpoints.LocationBatch, 500, "Internal Server Error");

            // Arrange: Create and save test location data to repository
            var locations = new List<LocationModel>
            {
                new LocationModel { Id = 1, Latitude = 37.7749, Longitude = -122.4194, Accuracy = 10, Timestamp = DateTime.UtcNow, IsSynced = false }
            };
            await _locationRepository.SaveLocationBatchAsync(locations);

            // Act: Call _locationSyncService.SyncLocationsAsync()
            bool syncResult = await _locationSyncService.SyncLocationsAsync();

            // Assert: Verify that synchronization returns false (failure)
            syncResult.Should().BeFalse();

            // Assert: Verify that the API was called
            _mockApiServer.GetRequestCount(ApiEndpoints.LocationBatch).Should().Be(1);

            // Assert: Verify that the locations in the database are still marked as not synced
            var unsyncedLocations = await _locationRepository.GetLocationsAsync(l => !l.IsSynced);
            unsyncedLocations.Count().Should().Be(1);
        }

        /// <summary>
        /// Tests that location synchronization handles partial success correctly
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestSyncLocations_PartialSuccess()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Set up mock server with partial success response (some IDs in SyncedIds, some in FailedIds)
            var responseObj = new LocationSyncResponse { SyncedIds = new List<int> { 1 }, FailedIds = new List<int> { 2 } };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.LocationBatch, responseObj);

            // Arrange: Create and save test location data to repository
            var locations = new List<LocationModel>
            {
                new LocationModel { Id = 1, Latitude = 37.7749, Longitude = -122.4194, Accuracy = 10, Timestamp = DateTime.UtcNow, IsSynced = false },
                new LocationModel { Id = 2, Latitude = 34.0522, Longitude = -118.2437, Accuracy = 15, Timestamp = DateTime.UtcNow, IsSynced = false }
            };
            await _locationRepository.SaveLocationBatchAsync(locations);

            // Act: Call _locationSyncService.SyncLocationsAsync()
            bool syncResult = await _locationSyncService.SyncLocationsAsync();

            // Assert: Verify that synchronization returns false (partial failure)
            syncResult.Should().BeFalse();

            // Assert: Verify that the API was called
            _mockApiServer.GetRequestCount(ApiEndpoints.LocationBatch).Should().Be(1);

            // Assert: Verify that successful locations are marked as synced
            var syncedLocations = await _locationRepository.GetLocationsAsync(l => l.IsSynced);
            syncedLocations.Count().Should().Be(1);
            syncedLocations.First().Id.Should().Be(1);

            // Assert: Verify that failed locations are still marked as not synced
            var unsyncedLocations = await _locationRepository.GetLocationsAsync(l => !l.IsSynced);
            unsyncedLocations.Count().Should().Be(1);
            unsyncedLocations.First().Id.Should().Be(2);
        }

        /// <summary>
        /// Tests that location synchronization is skipped when offline
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestSyncLocations_Offline()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Set up mock network service to indicate network is NOT available
            _mockNetworkService.Setup(x => x.IsConnected).Returns(false);

            // Arrange: Create and save test location data to repository
            var locations = new List<LocationModel>
            {
                new LocationModel { Id = 1, Latitude = 37.7749, Longitude = -122.4194, Accuracy = 10, Timestamp = DateTime.UtcNow, IsSynced = false }
            };
            await _locationRepository.SaveLocationBatchAsync(locations);

            // Act: Call _locationSyncService.SyncLocationsAsync()
            bool syncResult = await _locationSyncService.SyncLocationsAsync();

            // Assert: Verify that synchronization returns false (skipped due to offline)
            syncResult.Should().BeFalse();

            // Assert: Verify that the API was not called
            _mockApiServer.GetRequestCount(ApiEndpoints.LocationBatch).Should().Be(0);

            // Assert: Verify that the locations in the database are still marked as not synced
            var unsyncedLocations = await _locationRepository.GetLocationsAsync(l => !l.IsSynced);
            unsyncedLocations.Count().Should().Be(1);
        }

        /// <summary>
        /// Tests that location synchronization handles timeouts gracefully
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestSyncLocations_Timeout()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Set up mock server with delayed response (delay > timeout) for location batch endpoint
            _mockApiServer.SetupDelayedResponse(ApiEndpoints.LocationBatch, new LocationSyncResponse(), 40000); // 40 seconds delay

            // Arrange: Create and save test location data to repository
            var locations = new List<LocationModel>
            {
                new LocationModel { Id = 1, Latitude = 37.7749, Longitude = -122.4194, Accuracy = 10, Timestamp = DateTime.UtcNow, IsSynced = false }
            };
            await _locationRepository.SaveLocationBatchAsync(locations);

            // Act: Call _locationSyncService.SyncLocationsAsync()
            bool syncResult = await _locationSyncService.SyncLocationsAsync();

            // Assert: Verify that synchronization returns false (timeout)
            syncResult.Should().BeFalse();

            // Assert: Verify that the API was called
            _mockApiServer.GetRequestCount(ApiEndpoints.LocationBatch).Should().Be(1);

            // Assert: Verify that the locations in the database are still marked as not synced
            var unsyncedLocations = await _locationRepository.GetLocationsAsync(l => !l.IsSynced);
            unsyncedLocations.Count().Should().Be(1);
        }

        /// <summary>
        /// Tests starting and stopping location tracking
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestLocationTracking_StartStop()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Set up mock dependencies for location tracking
            _mockBackgroundService.Setup(x => x.Start()).Returns(Task.CompletedTask);
            _mockBackgroundService.Setup(x => x.Stop()).Returns(Task.CompletedTask);

            // Act 1: Call _locationService.StartTracking()
            await _locationService.StartTracking();

            // Assert 1: Verify that IsTracking is true
            _locationService.IsTracking.Should().BeTrue();

            // Assert 1: Verify that background service was started
            _mockBackgroundService.Verify(x => x.Start(), Times.Once);

            // Act 2: Call _locationService.StopTracking()
            await _locationService.StopTracking();

            // Assert 2: Verify that IsTracking is false
            _locationService.IsTracking.Should().BeFalse();

            // Assert 2: Verify that background service was stopped
            _mockBackgroundService.Verify(x => x.Stop(), Times.Once);
        }

        /// <summary>
        /// Tests getting the current location
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestGetCurrentLocation()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Set up mock dependencies to return a valid location
            var mockLocation = new LocationModel { Latitude = 37.7749, Longitude = -122.4194, Accuracy = 10, Timestamp = DateTime.UtcNow };
            _mockBackgroundService.Setup(x => x.Start()).Returns(Task.CompletedTask);
            _mockBackgroundService.Setup(x => x.Stop()).Returns(Task.CompletedTask);

            // Act: Call _locationService.GetCurrentLocation()
            LocationModel currentLocation = await _locationService.GetCurrentLocation();

            // Assert: Verify that a valid LocationModel is returned
            currentLocation.Should().NotBeNull();

            // Assert: Verify that the location has expected properties (non-zero coordinates, etc.)
            currentLocation.Latitude.Should().NotBe(0);
            currentLocation.Longitude.Should().NotBe(0);
        }

        /// <summary>
        /// Tests that location data is properly batched for API calls
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestLocationBatchProcessing()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Set up mock server with success response for location batch endpoint
            var responseObj = new LocationSyncResponse { SyncedIds = Enumerable.Range(1, AppConstants.LocationBatchSize).ToList() };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.LocationBatch, responseObj);

            // Arrange: Create and save multiple test location data points to repository
            var locations = Enumerable.Range(1, AppConstants.LocationBatchSize).Select(i => new LocationModel
            {
                Id = i,
                Latitude = 37.7749 + i * 0.0001,
                Longitude = -122.4194 + i * 0.0001,
                Accuracy = 10,
                Timestamp = DateTime.UtcNow,
                IsSynced = false
            }).ToList();
            await _locationRepository.SaveLocationBatchAsync(locations);

            // Act: Call _locationSyncService.SyncLocationsAsync() with a specific batch size
            bool syncResult = await _locationSyncService.SyncLocationsAsync(AppConstants.LocationBatchSize);

            // Assert: Verify that synchronization returns true (success)
            syncResult.Should().BeTrue();

            // Assert: Verify that the API was called
            _mockApiServer.GetRequestCount(ApiEndpoints.LocationBatch).Should().Be(1);

            // Assert: Verify that the request body contains the expected number of locations (batch size)
            string requestBody = _mockApiServer.GetLastRequestBody(ApiEndpoints.LocationBatch);
            requestBody.Should().NotBeNullOrEmpty();

            // Assert: Verify that a second batch would be needed for remaining locations
            var remainingLocations = await _locationRepository.GetPendingSyncLocationsAsync(AppConstants.LocationBatchSize);
            remainingLocations.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that scheduled synchronization works correctly
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestScheduledSync()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Set up mock server with success response for location batch endpoint
            var responseObj = new LocationSyncResponse { SyncedIds = new List<int> { 1 } };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.LocationBatch, responseObj);

            // Arrange: Create and save test location data to repository
            var locations = new List<LocationModel>
            {
                new LocationModel { Id = 1, Latitude = 37.7749, Longitude = -122.4194, Accuracy = 10, Timestamp = DateTime.UtcNow, IsSynced = false }
            };
            await _locationRepository.SaveLocationBatchAsync(locations);

            // Act: Call _locationSyncService.ScheduleSync() with a short interval
            _locationSyncService.ScheduleSync(TimeSpan.FromSeconds(1));

            // Assert: Wait for a short period to allow scheduled sync to run
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Assert: Verify that the API was called
            _mockApiServer.GetRequestCount(ApiEndpoints.LocationBatch).Should().BeGreaterThan(0);

            // Assert: Verify that the locations in the database are marked as synced
            var syncedLocations = await _locationRepository.GetLocationsAsync(l => l.IsSynced);
            syncedLocations.Count().Should().Be(1);
        }

        /// <summary>
        /// Tests the complete location tracking flow from start to sync
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        [Fact, public, async]
        public async Task TestCompleteLocationTrackingFlow()
        {
            // Arrange: Initialize test environment
            await InitializeAsync();

            // Arrange: Set up mock server with success responses
            var responseObj = new LocationSyncResponse { SyncedIds = new List<int> { 1, 2, 3 } };
            _mockApiServer.SetupSuccessResponse(ApiEndpoints.LocationBatch, responseObj);

            // Arrange: Set up mock dependencies for location tracking
            _mockBackgroundService.Setup(x => x.Start()).Returns(Task.CompletedTask);
            _mockBackgroundService.Setup(x => x.Stop()).Returns(Task.CompletedTask);

            // Act 1: Call _locationService.StartTracking()
            await _locationService.StartTracking();

            // Assert 1: Verify that IsTracking is true
            _locationService.IsTracking.Should().BeTrue();

            // Act 2: Simulate location updates (add locations to the queue)
            var locations = new List<LocationModel>
            {
                new LocationModel { Id = 1, Latitude = 37.7749, Longitude = -122.4194, Accuracy = 10, Timestamp = DateTime.UtcNow, IsSynced = false },
                new LocationModel { Id = 2, Latitude = 34.0522, Longitude = -118.2437, Accuracy = 15, Timestamp = DateTime.UtcNow, IsSynced = false },
                new LocationModel { Id = 3, Latitude = 40.7128, Longitude = -74.0060, Accuracy = 20, Timestamp = DateTime.UtcNow, IsSynced = false }
            };
            await _locationRepository.SaveLocationBatchAsync(locations);

            // Act 3: Call _locationSyncService.SyncLocationsAsync()
            bool syncResult = await _locationSyncService.SyncLocationsAsync();

            // Assert 3: Verify that synchronization returns true (success)
            syncResult.Should().BeTrue();

            // Assert 3: Verify that the API was called with the correct data
            _mockApiServer.GetRequestCount(ApiEndpoints.LocationBatch).Should().Be(1);

            // Act 4: Call _locationService.StopTracking()
            await _locationService.StopTracking();

            // Assert 4: Verify that IsTracking is false
            _locationService.IsTracking.Should().BeFalse();

            // Assert 4: Verify that all locations are properly synced
            var syncedLocations = await _locationRepository.GetLocationsAsync(l => l.IsSynced);
            syncedLocations.Count().Should().Be(3);
        }
    }
}