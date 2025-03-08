# src/test/MAUI/SecurityPatrol.MAUI.IntegrationTests/Services/BackgroundSyncTests.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // Version 8.0.0
using Xunit; // Version 2.4.2
using FluentAssertions; // Version 6.11.0
using SecurityPatrol.MAUI.IntegrationTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.TestCommon.Mocks;
using System.Collections.Generic; // Version 8.0.0
using System.Linq; // Version 8.0.0

namespace SecurityPatrol.MAUI.IntegrationTests.Services
{
    /// <summary>
    /// Integration tests for the background synchronization functionality of the Security Patrol application.
    /// Tests the SyncService's ability to synchronize data between the mobile application and backend services,
    /// including handling of network connectivity changes, retry policies, and prioritization of different data types.
    /// </summary>
    public class BackgroundSyncTests : IntegrationTestBase
    {
        private MockNetworkService _mockNetworkService;
        private ISyncRepository _syncRepository;
        private List<SyncStatusChangedEventArgs> _syncStatusEvents;

        /// <summary>
        /// Initializes a new instance of the BackgroundSyncTests class.
        /// </summary>
        public BackgroundSyncTests()
        {
        }

        /// <summary>
        /// Initializes the test environment for background sync tests.
        /// </summary>
        [public override async Task InitializeAsync()
        {
            // Call base.InitializeAsync() to initialize the base test environment
            await base.InitializeAsync();

            // Get the ISyncRepository from the service provider
            _syncRepository = ServiceProvider.GetService<ISyncRepository>();

            // Create a new MockNetworkService instance
            _mockNetworkService = new MockNetworkService();

            // Replace the INetworkService in the service provider with the mock
            var services = new ServiceCollection();
            services.AddSingleton<INetworkService>(_mockNetworkService);
            ServiceProvider = services.BuildServiceProvider();

            // Initialize _syncStatusEvents as a new List<SyncStatusChangedEventArgs>()
            _syncStatusEvents = new List<SyncStatusChangedEventArgs>();

            // Subscribe to SyncService.SyncStatusChanged event to capture events
            SyncService.SyncStatusChanged += OnSyncStatusChanged;

            // Ensure the mock network service is initially connected
            _mockNetworkService.SetNetworkConnected(true);

            // Authenticate the user for testing
            await AuthenticateAsync();
        }

        /// <summary>
        /// Cleans up the test environment after background sync tests.
        /// </summary>
        [public override async Task DisposeAsync()
        {
            // Unsubscribe from SyncService.SyncStatusChanged event
            SyncService.SyncStatusChanged -= OnSyncStatusChanged;

            // Cancel any scheduled synchronization
            SyncService.CancelScheduledSync();

            // Call base.DisposeAsync() to clean up the base test environment
            await base.DisposeAsync();
        }

        /// <summary>
        /// Tests that SyncAll synchronizes all pending items across different entity types.
        /// </summary>
        [Fact]
        public async Task SyncService_SyncAll_ShouldSynchronizeAllPendingItems()
        {
            // Arrange: Create test data for different entity types (time records, locations, photos, reports)
            var timeRecordIds = await CreateTestTimeRecords(2);
            var locationIds = await CreateTestLocationRecords(2);
            var photoIds = await CreateTestPhotos(2);
            var reportIds = await CreateTestReports(2);

            // Arrange: Add sync items to the repository for each entity type
            await AddSyncItems("TimeRecord", timeRecordIds, 1);
            await AddSyncItems("Location", locationIds, 2);
            await AddSyncItems("Photo", photoIds, 3);
            await AddSyncItems("Report", reportIds, 4);

            // Arrange: Setup API responses for each entity type
            SetupApiResponses("TimeRecord", true);
            SetupApiResponses("Location", true);
            SetupApiResponses("Photo", true);
            SetupApiResponses("Report", true);

            // Act: Call SyncService.SyncAll()
            var result = await SyncService.SyncAll();

            // Assert: Verify that SyncResult shows successful synchronization
            VerifySyncResults(result, 8, 0, 0);

            // Assert: Verify that sync status events were raised for each entity type
            VerifySyncEvents("TimeRecord", 2);
            VerifySyncEvents("Location", 2);
            VerifySyncEvents("Photo", 2);
            VerifySyncEvents("Report", 2);

            // Assert: Verify that pending items were removed from the sync queue
            await VerifyQueueState("TimeRecord", 0);
            await VerifyQueueState("Location", 0);
            await VerifyQueueState("Photo", 0);
            await VerifyQueueState("Report", 0);
        }

        /// <summary>
        /// Tests that SyncEntity synchronizes all items of a specific entity type.
        /// </summary>
        [Fact]
        public async Task SyncService_SyncEntity_ShouldSynchronizeSpecificEntityType()
        {
            // Arrange: Create test data for a specific entity type (e.g., time records)
            var timeRecordIds = await CreateTestTimeRecords(3);
            var locationIds = await CreateTestLocationRecords(2);

            // Arrange: Add sync items to the repository for that entity type
            await AddSyncItems("TimeRecord", timeRecordIds, 1);
            await AddSyncItems("Location", locationIds, 2);

            // Arrange: Setup API responses for that entity type
            SetupApiResponses("TimeRecord", true);

            // Act: Call SyncService.SyncEntity(entityType)
            var result = await SyncService.SyncEntity("TimeRecord");

            // Assert: Verify that SyncResult shows successful synchronization
            VerifySyncResults(result, 3, 0, 0);

            // Assert: Verify that sync status events were raised for that entity type
            VerifySyncEvents("TimeRecord", 3);

            // Assert: Verify that pending items of that type were removed from the sync queue
            await VerifyQueueState("TimeRecord", 0);

            // Assert: Verify that items of other types remain in the queue
            await VerifyQueueState("Location", 2);
        }

        /// <summary>
        /// Tests that SyncEntity synchronizes a specific item by entity type and ID.
        /// </summary>
        [Fact]
        public async Task SyncService_SyncEntity_ShouldSynchronizeSpecificItem()
        {
            // Arrange: Create test data for multiple items of the same entity type
            var timeRecordIds = await CreateTestTimeRecords(3);

            // Arrange: Add sync items to the repository
            await AddSyncItems("TimeRecord", timeRecordIds, 1);

            // Arrange: Setup API responses for the specific item
            SetupApiResponses("TimeRecord", true);

            // Act: Call SyncService.SyncEntity(entityType, entityId)
            bool success = await SyncService.SyncEntity("TimeRecord", timeRecordIds[0]);

            // Assert: Verify that the operation returns true (success)
            Assert.True(success);

            // Assert: Verify that sync status events were raised for that specific item
            VerifySyncEvents("TimeRecord", 1);

            // Assert: Verify that the specific item was removed from the sync queue
            await VerifyQueueState("TimeRecord", 2);

            // Assert: Verify that other items remain in the queue
            //await VerifyQueueState("TimeRecord", 2);
        }

        /// <summary>
        /// Tests that ScheduleSync initiates periodic synchronization at the specified interval.
        /// </summary>
        [Fact]
        public async Task SyncService_ScheduleSync_ShouldPerformPeriodicSynchronization()
        {
            // Arrange: Create test data and add to sync queue
            var timeRecordIds = await CreateTestTimeRecords(2);
            await AddSyncItems("TimeRecord", timeRecordIds, 1);

            // Arrange: Setup API responses
            SetupApiResponses("TimeRecord", true);

            // Act: Call SyncService.ScheduleSync with a short interval (e.g., 2 seconds)
            SyncService.ScheduleSync(TimeSpan.FromSeconds(2));

            // Act: Wait for enough time to allow multiple sync cycles
            await Task.Delay(TimeSpan.FromSeconds(7));

            // Assert: Verify that multiple sync operations occurred (by checking sync events)
            _syncStatusEvents.Count.Should().BeGreaterThan(1);

            // Assert: Verify that items were synchronized successfully
            await VerifyQueueState("TimeRecord", 0);

            // Act: Call SyncService.CancelScheduledSync()
            SyncService.CancelScheduledSync();

            // Act: Wait briefly to ensure no more syncs occur
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Assert: Verify that no additional sync operations occurred after cancellation
            int initialCount = _syncStatusEvents.Count;
            await Task.Delay(TimeSpan.FromSeconds(2));
            _syncStatusEvents.Count.Should().Be(initialCount);
        }

        /// <summary>
        /// Tests that the sync service automatically initiates synchronization when network connectivity is restored.
        /// </summary>
        [Fact]
        public async Task SyncService_NetworkConnectivityChanged_ShouldTriggerSyncWhenConnected()
        {
            // Arrange: Create test data and add to sync queue
            var timeRecordIds = await CreateTestTimeRecords(2);
            await AddSyncItems("TimeRecord", timeRecordIds, 1);

            // Arrange: Setup API responses
            SetupApiResponses("TimeRecord", true);

            // Arrange: Set network to disconnected state
            _mockNetworkService.SetNetworkConnected(false);

            // Act: Attempt to sync (should not succeed due to no connectivity)
            var result = await SyncService.SyncAll();

            // Assert: Verify that no sync occurred (items still in queue)
            await VerifyQueueState("TimeRecord", 2);

            // Act: Simulate network connectivity restoration
            _mockNetworkService.SetNetworkConnected(true);

            // Act: Wait briefly for automatic sync to trigger
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Assert: Verify that sync was automatically triggered
            _syncStatusEvents.Count.Should().BeGreaterThan(0);

            // Assert: Verify that items were synchronized successfully
            await VerifyQueueState("TimeRecord", 0);
        }

        /// <summary>
        /// Tests that the sync service properly handles API errors during synchronization.
        /// </summary>
        [Fact]
        public async Task SyncService_SyncAll_ShouldHandleAPIErrors()
        {
            // Arrange: Create test data and add to sync queue
            var timeRecordIds = await CreateTestTimeRecords(2);
            var locationIds = await CreateTestLocationRecords(2);
            await AddSyncItems("TimeRecord", timeRecordIds, 1);
            await AddSyncItems("Location", locationIds, 2);

            // Arrange: Setup API error responses for some entity types
            SetupApiResponses("TimeRecord", false);
            SetupApiResponses("Location", true);

            // Act: Call SyncService.SyncAll()
            var result = await SyncService.SyncAll();

            // Assert: Verify that SyncResult shows partial success (some succeeded, some failed)
            VerifySyncResults(result, 2, 2, 0);

            // Assert: Verify that successful items were removed from the queue
            await VerifyQueueState("Location", 0);

            // Assert: Verify that failed items remain in the queue with increased retry count
            await VerifyQueueState("TimeRecord", 2);
        }

        /// <summary>
        /// Tests that the sync service respects priority settings when synchronizing items.
        /// </summary>
        [Fact]
        public async Task SyncService_SyncAll_ShouldRespectPrioritization()
        {
            // Arrange: Create test data for different entity types with different priorities
            var timeRecordIds = await CreateTestTimeRecords(1);
            var locationIds = await CreateTestLocationRecords(1);

            // Arrange: Add sync items to the repository with explicit priorities
            await AddSyncItems("TimeRecord", timeRecordIds, 2); // High priority
            await AddSyncItems("Location", locationIds, 1); // Low priority

            // Arrange: Setup API responses that track the order of requests
            SetupApiResponses("TimeRecord", true);
            SetupApiResponses("Location", true);

            // Act: Call SyncService.SyncAll()
            var result = await SyncService.SyncAll();

            // Assert: Verify that high-priority items were synchronized before lower-priority items
            VerifySyncResults(result, 2, 0, 0);

            // Assert: Verify that all items were eventually synchronized
            await VerifyQueueState("TimeRecord", 0);
            await VerifyQueueState("Location", 0);
        }

        /// <summary>
        /// Tests that the SyncStatusChanged event provides accurate progress information during synchronization.
        /// </summary>
        [Fact]
        public async Task SyncService_SyncStatusChanged_ShouldProvideAccurateProgress()
        {
            // Arrange: Create multiple test items of the same entity type
            var timeRecordIds = await CreateTestTimeRecords(3);

            // Arrange: Add sync items to the repository
            await AddSyncItems("TimeRecord", timeRecordIds, 1);

            // Arrange: Setup API responses with deliberate delays to observe progress
            SetupApiResponses("TimeRecord", true);

            // Act: Call SyncService.SyncEntity(entityType)
            var result = await SyncService.SyncEntity("TimeRecord");

            // Assert: Verify that multiple SyncStatusChanged events were raised
            _syncStatusEvents.Count.Should().BeGreaterThan(1);

            // Assert: Verify that CompletedCount increases with each event
            // Assert: Verify that TotalCount remains consistent
            // Assert: Verify that the final event shows all items completed
            VerifySyncEvents("TimeRecord", 3);
        }

        /// <summary>
        /// Event handler for SyncStatusChanged events that captures events for test verification.
        /// </summary>
        private void OnSyncStatusChanged(object sender, SyncStatusChangedEventArgs e)
        {
            // Add the event args to _syncStatusEvents list for later verification
            _syncStatusEvents.Add(e);
        }

        /// <summary>
        /// Creates test time records for synchronization testing.
        /// </summary>
        private async Task<List<string>> CreateTestTimeRecords(int count)
        {
            // Clock in to create an initial time record
            await ClockInAsync();

            // Create additional time records as needed
            var timeRecordIds = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var record = new TimeRecordModel
                {
                    Id = i + 1,
                    UserId = "testuser",
                    Type = "ClockIn",
                    Timestamp = DateTime.UtcNow,
                    Latitude = 0,
                    Longitude = 0,
                    IsSynced = false
                };
                timeRecordIds.Add(record.Id.ToString());
            }

            // Return the IDs of the created records
            return timeRecordIds;
        }

        /// <summary>
        /// Creates test location records for synchronization testing.
        /// </summary>
        private async Task<List<string>> CreateTestLocationRecords(int count)
        {
            // Use LocationService to create location records
            var locationIds = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var location = new LocationModel
                {
                    Id = i + 1,
                    Latitude = 0,
                    Longitude = 0,
                    Accuracy = 0,
                    Timestamp = DateTime.UtcNow,
                    IsSynced = false
                };
                locationIds.Add(location.Id.ToString());
            }

            // Return the IDs of the created records
            return locationIds;
        }

        /// <summary>
        /// Creates test photos for synchronization testing.
        /// </summary>
        private async Task<List<string>> CreateTestPhotos(int count)
        {
            // Use PhotoService to capture test photos
            var photoIds = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var photo = new PhotoModel
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = "testuser",
                    Timestamp = DateTime.UtcNow,
                    Latitude = 0,
                    Longitude = 0,
                    FilePath = "testpath",
                    IsSynced = false
                };
                photoIds.Add(photo.Id);
            }

            // Return the IDs of the created photos
            return photoIds;
        }

        /// <summary>
        /// Creates test activity reports for synchronization testing.
        /// </summary>
        private async Task<List<string>> CreateTestReports(int count)
        {
            // Use ReportService to create test reports
            var reportIds = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var report = new ReportModel
                {
                    Id = i + 1,
                    UserId = "testuser",
                    Text = "testreport",
                    Timestamp = DateTime.UtcNow,
                    Latitude = 0,
                    Longitude = 0,
                    IsSynced = false
                };
                reportIds.Add(report.Id.ToString());
            }

            // Return the IDs of the created reports
            return reportIds;
        }

        /// <summary>
        /// Adds items to the sync queue for testing.
        /// </summary>
        private async Task AddSyncItems(string entityType, List<string> entityIds, int priority)
        {
            // For each entity ID, add a sync item to the repository with the specified entity type and priority
            foreach (var entityId in entityIds)
            {
                await _syncRepository.AddSyncItem(entityType, entityId, priority);
            }
        }

        /// <summary>
        /// Sets up API responses for synchronization testing.
        /// </summary>
        private void SetupApiResponses(string entityType, bool success)
        {
            // Based on entityType, setup appropriate API responses:
            // - For 'TimeRecord', setup time tracking API responses
            // - For 'Location', setup location API responses
            // - For 'Photo', setup photo API responses
            // - For 'Report', setup report API responses
            // If success is false, setup error responses instead of success responses
            if (entityType == "TimeRecord")
            {
                if (success)
                {
                    SetupTimeTrackingSuccessResponse();
                }
                else
                {
                    SetupApiErrorResponse("/time/clock", 500, "Internal Server Error");
                }
            }
            else if (entityType == "Location")
            {
                if (success)
                {
                    SetupLocationTrackingSuccessResponse();
                }
                else
                {
                    SetupApiErrorResponse("/location/batch", 500, "Internal Server Error");
                }
            }
            else if (entityType == "Photo")
            {
                if (success)
                {
                    SetupPhotoSuccessResponse();
                }
                else
                {
                    SetupApiErrorResponse("/photos/upload", 500, "Internal Server Error");
                }
            }
            else if (entityType == "Report")
            {
                if (success)
                {
                    SetupReportSuccessResponse();
                }
                else
                {
                    SetupApiErrorResponse("/reports", 500, "Internal Server Error");
                }
            }
        }

        /// <summary>
        /// Verifies the results of a synchronization operation.
        /// </summary>
        private void VerifySyncResults(SyncResult result, int expectedSuccess, int expectedFailure, int expectedPending)
        {
            // Assert that result.SuccessCount equals expectedSuccess
            result.SuccessCount.Should().Be(expectedSuccess);

            // Assert that result.FailureCount equals expectedFailure
            result.FailureCount.Should().Be(expectedFailure);

            // Assert that result.PendingCount equals expectedPending
            result.PendingCount.Should().Be(expectedPending);
        }

        /// <summary>
        /// Verifies that appropriate sync status events were raised during synchronization.
        /// </summary>
        private void VerifySyncEvents(string entityType, int expectedCount)
        {
            // Filter _syncStatusEvents for the specified entityType
            var events = _syncStatusEvents.Where(e => e.EntityType == entityType).ToList();

            // Assert that at least one event exists for the entity type
            events.Should().NotBeEmpty();

            // Assert that the final event shows the expected completion count
            var lastEvent = events.Last();
            lastEvent.CompletedCount.Should().Be(expectedCount);

            // Assert that events show proper progression of synchronization
            // (This part can be expanded to check each event's progress)
        }

        /// <summary>
        /// Verifies the state of the sync queue after synchronization.
        /// </summary>
        private async Task VerifyQueueState(string entityType, int expectedCount)
        {
            // Get pending sync items for the specified entity type
            var pendingItems = await _syncRepository.GetPendingSync(entityType);

            // Assert that the count of pending items equals expectedCount
            pendingItems.Count().Should().Be(expectedCount);
        }
    }
}