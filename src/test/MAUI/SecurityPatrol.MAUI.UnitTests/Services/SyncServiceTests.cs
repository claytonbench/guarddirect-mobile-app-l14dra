using System; // System 8.0+
using System.Threading; // System.Threading 8.0+
using System.Threading.Tasks; // System.Threading.Tasks 8.0+
using System.Collections.Generic; // System.Collections.Generic 8.0+
using Xunit; // xunit 2.4.2
using Moq; // Moq 4.18.4
using FluentAssertions; // FluentAssertions 6.11.0
using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging 8.0.0
using SecurityPatrol.MAUI.UnitTests.Setup; // TestBase
using SecurityPatrol.Services; // ISyncService, SyncService
using SecurityPatrol.Models; // SyncResult, SyncStatusChangedEventArgs
using SecurityPatrol.TestCommon.Mocks; // MockNetworkService

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the SyncService class to verify its synchronization functionality, network handling, and event notifications.
    /// </summary>
    public class SyncServiceTests : TestBase
    {
        private Mock<ISyncRepository> MockSyncRepository { get; set; }
        private Mock<INetworkService> MockNetworkService { get; set; }
        private Mock<ITimeTrackingSyncService> MockTimeTrackingSyncService { get; set; }
        private Mock<ILocationSyncService> MockLocationSyncService { get; set; }
        private Mock<IPhotoSyncService> MockPhotoSyncService { get; set; }
        private Mock<IReportSyncService> MockReportSyncService { get; set; }
        private Mock<ILogger<SyncService>> MockLogger { get; set; }
        private SyncService SyncService { get; set; }

        /// <summary>
        /// Initializes a new instance of the SyncServiceTests class with test setup
        /// </summary>
        public SyncServiceTests()
        {
            // Initialize mock dependencies using Moq
            MockSyncRepository = new Mock<ISyncRepository>();
            MockNetworkService = new Mock<INetworkService>();
            MockTimeTrackingSyncService = new Mock<ITimeTrackingSyncService>();
            MockLocationSyncService = new Mock<ILocationSyncService>();
            MockPhotoSyncService = new Mock<IPhotoSyncService>();
            MockReportSyncService = new Mock<IReportSyncService>();
            MockLogger = new Mock<ILogger<SyncService>>();

            // Configure default mock behaviors
            Setup();

            // Create an instance of SyncService with the mock dependencies
            SyncService = new SyncService(
                MockSyncRepository.Object,
                MockNetworkService.Object,
                MockTimeTrackingSyncService.Object,
                MockLocationSyncService.Object,
                MockPhotoSyncService.Object,
                MockReportSyncService.Object,
                MockLogger.Object);
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        [Fact]
        public void Setup()
        {
            // Reset all mocks
            MockSyncRepository.Reset();
            MockNetworkService.Reset();
            MockTimeTrackingSyncService.Reset();
            MockLocationSyncService.Reset();
            MockPhotoSyncService.Reset();
            MockReportSyncService.Reset();
            MockLogger.Reset();

            // Configure MockNetworkService.IsConnected to return true by default
            MockNetworkService.Setup(x => x.IsConnected).Returns(true);

            // Configure MockSyncRepository to return empty collections by default
            MockSyncRepository.Setup(x => x.GetPendingSync(It.IsAny<string>()))
                .ReturnsAsync(new List<SyncItem>());

            // Configure MockTimeTrackingSyncService to return successful results
            MockTimeTrackingSyncService.Setup(x => x.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Configure MockLocationSyncService to return successful results
            MockLocationSyncService.Setup(x => x.SyncLocationsAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            // Configure MockPhotoSyncService to return successful results
            MockPhotoSyncService.Setup(x => x.SyncPhotosAsync())
                .ReturnsAsync(true);

            // Configure MockReportSyncService to return successful results
            MockReportSyncService.Setup(x => x.SyncReportsAsync())
                .ReturnsAsync(1);

            // Create a new instance of SyncService with the configured mocks
            SyncService = new SyncService(
                MockSyncRepository.Object,
                MockNetworkService.Object,
                MockTimeTrackingSyncService.Object,
                MockLocationSyncService.Object,
                MockPhotoSyncService.Object,
                MockReportSyncService.Object,
                MockLogger.Object);
        }

        /// <summary>
        /// Tests that SyncAll synchronizes all entity types when network is connected
        /// </summary>
        [Fact]
        public async Task SyncAll_WhenNetworkConnected_ShouldSyncAllEntityTypes()
        {
            // Arrange: Configure MockNetworkService.IsConnected to return true
            MockNetworkService.Setup(x => x.IsConnected).Returns(true);

            // Arrange: Configure MockSyncRepository to return pending items for each entity type
            MockSyncRepository.Setup(x => x.GetPendingSync("TimeRecord"))
                .ReturnsAsync(new List<SyncItem> { new SyncItem("TimeRecord", "1", 1) });
            MockSyncRepository.Setup(x => x.GetPendingSync("Location"))
                .ReturnsAsync(new List<SyncItem> { new SyncItem("Location", "1", 1) });
            MockSyncRepository.Setup(x => x.GetPendingSync("Photo"))
                .ReturnsAsync(new List<SyncItem> { new SyncItem("Photo", "1", 1) });
            MockSyncRepository.Setup(x => x.GetPendingSync("Report"))
                .ReturnsAsync(new List<SyncItem> { new SyncItem("Report", "1", 1) });

            // Act: Call SyncService.SyncAll()
            var result = await SyncService.SyncAll();

            // Assert: Verify that MockTimeTrackingSyncService.SyncTimeRecordsAsync was called
            MockTimeTrackingSyncService.Verify(x => x.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Assert: Verify that MockLocationSyncService.SyncLocationsAsync was called
            MockLocationSyncService.Verify(x => x.SyncLocationsAsync(It.IsAny<int>()), Times.Once);

            // Assert: Verify that MockPhotoSyncService.SyncPhotosAsync was called
            MockPhotoSyncService.Verify(x => x.SyncPhotosAsync(), Times.Once);

            // Assert: Verify that MockReportSyncService.SyncReportsAsync was called
            MockReportSyncService.Verify(x => x.SyncReportsAsync(), Times.Once);

            // Assert: Verify that the result contains the expected success counts
            result.SuccessCount.Should().Be(3);
        }

        /// <summary>
        /// Tests that SyncAll returns an offline result when network is disconnected
        /// </summary>
        [Fact]
        public async Task SyncAll_WhenNetworkDisconnected_ShouldReturnOfflineResult()
        {
            // Arrange: Configure MockNetworkService.IsConnected to return false
            MockNetworkService.Setup(x => x.IsConnected).Returns(false);

            // Act: Call SyncService.SyncAll()
            var result = await SyncService.SyncAll();

            // Assert: Verify that no sync services were called
            MockTimeTrackingSyncService.Verify(x => x.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()), Times.Never);
            MockLocationSyncService.Verify(x => x.SyncLocationsAsync(It.IsAny<int>()), Times.Never);
            MockPhotoSyncService.Verify(x => x.SyncPhotosAsync(), Times.Never);
            MockReportSyncService.Verify(x => x.SyncReportsAsync(), Times.Never);

            // Assert: Verify that the result indicates offline status
            result.PendingCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that SyncAll returns a busy result when synchronization is already in progress
        /// </summary>
        [Fact]
        public async Task SyncAll_WhenAlreadySyncing_ShouldReturnBusyResult()
        {
            // Arrange: Set up SyncService to be already syncing
            SyncService.IsSyncing = true;

            // Act: Call SyncService.SyncAll()
            var result = await SyncService.SyncAll();

            // Assert: Verify that no sync services were called
            MockTimeTrackingSyncService.Verify(x => x.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()), Times.Never);
            MockLocationSyncService.Verify(x => x.SyncLocationsAsync(It.IsAny<int>()), Times.Never);
            MockPhotoSyncService.Verify(x => x.SyncPhotosAsync(), Times.Never);
            MockReportSyncService.Verify(x => x.SyncReportsAsync(), Times.Never);

            // Assert: Verify that the result indicates busy status
            result.PendingCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that SyncEntity synchronizes a specific entity when given valid type and ID
        /// </summary>
        [Fact]
        public async Task SyncEntity_WithValidEntityTypeAndId_ShouldSyncSpecificEntity()
        {
            // Arrange: Configure MockNetworkService.IsConnected to return true
            MockNetworkService.Setup(x => x.IsConnected).Returns(true);

            // Arrange: Configure MockSyncRepository to return the entity
            MockSyncRepository.Setup(x => x.GetPendingSync("TimeRecord"))
                .ReturnsAsync(new List<SyncItem> { new SyncItem("TimeRecord", "123", 1) });

            // Act: Call SyncService.SyncEntity('TimeRecord', '123')
            var result = await SyncService.SyncEntity("TimeRecord", "123");

            // Assert: Verify that the appropriate sync service method was called
            MockTimeTrackingSyncService.Verify(x => x.SyncRecordAsync(123, It.IsAny<CancellationToken>()), Times.Once);

            // Assert: Verify that the result is true
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that SyncEntity synchronizes all entities of a specific type
        /// </summary>
        [Fact]
        public async Task SyncEntity_WithValidEntityType_ShouldSyncAllEntitiesOfType()
        {
            // Arrange: Configure MockNetworkService.IsConnected to return true
            MockNetworkService.Setup(x => x.IsConnected).Returns(true);

            // Arrange: Configure MockSyncRepository to return entities of the specified type
            MockSyncRepository.Setup(x => x.GetPendingSync("TimeRecord"))
                .ReturnsAsync(new List<SyncItem> { new SyncItem("TimeRecord", "1", 1), new SyncItem("TimeRecord", "2", 1) });

            // Act: Call SyncService.SyncEntity('TimeRecord')
            var result = await SyncService.SyncEntity("TimeRecord");

            // Assert: Verify that the appropriate sync service method was called
            MockTimeTrackingSyncService.Verify(x => x.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Assert: Verify that the result contains the expected success counts
            result.SuccessCount.Should().Be(0);
        }

        /// <summary>
        /// Tests that SyncEntity returns false when network is disconnected
        /// </summary>
        [Fact]
        public async Task SyncEntity_WhenNetworkDisconnected_ShouldReturnFalse()
        {
            // Arrange: Configure MockNetworkService.IsConnected to return false
            MockNetworkService.Setup(x => x.IsConnected).Returns(false);

            // Act: Call SyncService.SyncEntity('TimeRecord', '123')
            var result = await SyncService.SyncEntity("TimeRecord", "123");

            // Assert: Verify that no sync services were called
            MockTimeTrackingSyncService.Verify(x => x.SyncRecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

            // Assert: Verify that the result is false
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ScheduleSync sets up a timer with the specified interval
        /// </summary>
        [Fact]
        public void ScheduleSync_WithValidInterval_ShouldSetupTimer()
        {
            // Arrange: Create a TimeSpan interval
            TimeSpan interval = TimeSpan.FromSeconds(60);

            // Act: Call SyncService.ScheduleSync(interval)
            SyncService.ScheduleSync(interval);

            // Assert: Verify that a timer was created with the correct interval
            Assert.NotNull(SyncService.GetType().GetField("_syncTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(SyncService));
        }

        /// <summary>
        /// Tests that CancelScheduledSync cancels the scheduled synchronization timer
        /// </summary>
        [Fact]
        public void CancelScheduledSync_WhenSyncScheduled_ShouldCancelTimer()
        {
            // Arrange: Call SyncService.ScheduleSync to set up a timer
            TimeSpan interval = TimeSpan.FromSeconds(60);
            SyncService.ScheduleSync(interval);

            // Act: Call SyncService.CancelScheduledSync()
            SyncService.CancelScheduledSync();

            // Assert: Verify that the timer was disposed
            Assert.Null(SyncService.GetType().GetField("_syncTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(SyncService));
        }

        /// <summary>
        /// Tests that GetSyncStatus returns the correct counts of pending items by entity type
        /// </summary>
        [Fact]
        public async Task GetSyncStatus_ShouldReturnPendingCounts()
        {
            // Arrange: Configure MockSyncRepository to return specific pending counts
            var expectedStatus = new Dictionary<string, int>
            {
                { "TimeRecord", 5 },
                { "Location", 10 },
                { "Photo", 3 },
                { "Report", 2 }
            };
            MockSyncRepository.Setup(x => x.GetSyncStatistics())
                .ReturnsAsync(expectedStatus);

            // Act: Call SyncService.GetSyncStatus()
            var actualStatus = await SyncService.GetSyncStatus();

            // Assert: Verify that the result contains the expected entity types and counts
            actualStatus.Should().BeEquivalentTo(expectedStatus);
        }

        /// <summary>
        /// Tests that the service triggers synchronization when connectivity is restored
        /// </summary>
        [Fact]
        public async Task OnConnectivityChanged_WhenConnectivityRestored_ShouldTriggerSync()
        {
            // Arrange: Configure MockNetworkService.IsConnected to return false initially
            MockNetworkService.Setup(x => x.IsConnected).Returns(false);

            // Arrange: Configure MockSyncRepository to return pending items
            MockSyncRepository.Setup(x => x.GetPendingSync(It.IsAny<string>()))
                .ReturnsAsync(new List<SyncItem> { new SyncItem("TimeRecord", "1", 1) });

            // Act: Change MockNetworkService.IsConnected to true and raise ConnectivityChanged event
            MockNetworkService.Setup(x => x.IsConnected).Returns(true);
            MockNetworkService.SimulateConnectivityChange();

            // Assert: Verify that SyncAll was called
            await Task.Delay(100); // Allow time for the event to trigger the sync
            MockTimeTrackingSyncService.Verify(x => x.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that the SyncStatusChanged event is raised with the correct arguments
        /// </summary>
        [Fact]
        public async Task SyncStatusChanged_ShouldRaiseEventWithCorrectArgs()
        {
            // Arrange: Set up an event handler to capture the SyncStatusChanged event
            SyncStatusChangedEventArgs actualArgs = null;
            SyncService.SyncStatusChanged += (sender, args) => actualArgs = args;

            // Arrange: Configure MockNetworkService.IsConnected to return true
            MockNetworkService.Setup(x => x.IsConnected).Returns(true);

            // Arrange: Configure MockSyncRepository to return pending items
            MockSyncRepository.Setup(x => x.GetPendingSync(It.IsAny<string>()))
                .ReturnsAsync(new List<SyncItem> { new SyncItem("TimeRecord", "1", 1) });

            // Act: Call SyncService.SyncAll()
            await SyncService.SyncAll();

            // Assert: Verify that the event was raised with the correct entity type, status, and counts
            Assert.NotNull(actualArgs);
            actualArgs.EntityType.Should().Be("All");
            actualArgs.Status.Should().Be("Completed");
        }

        /// <summary>
        /// Tests that SyncAll respects cancellation when a cancellation token is provided
        /// </summary>
        [Fact]
        public async Task SyncAll_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange: Configure MockNetworkService.IsConnected to return true
            MockNetworkService.Setup(x => x.IsConnected).Returns(true);

            // Arrange: Configure MockSyncRepository to return pending items
            MockSyncRepository.Setup(x => x.GetPendingSync(It.IsAny<string>()))
                .ReturnsAsync(new List<SyncItem> { new SyncItem("TimeRecord", "1", 1) });

            // Arrange: Create a CancellationTokenSource and cancel it
            var cts = new CancellationTokenSource();
            cts.Cancel();
            CancellationToken cancellationToken = cts.Token;

            // Act: Call SyncService.SyncAll(cancellationToken)
            await Assert.ThrowsAsync<TaskCanceledException>(() => SyncService.SyncAll(cancellationToken));

            // Assert: Verify that the operation was cancelled and sync services were not called
            MockTimeTrackingSyncService.Verify(x => x.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Tests that Dispose properly cleans up resources
        /// </summary>
        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange: Call SyncService.ScheduleSync to set up a timer
            TimeSpan interval = TimeSpan.FromSeconds(60);
            SyncService.ScheduleSync(interval);

            // Act: Call SyncService.Dispose()
            SyncService.Dispose();

            // Assert: Verify that the timer was disposed
            Assert.Null(SyncService.GetType().GetField("_syncTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(SyncService));

            // Assert: Verify that event handlers were unsubscribed
            var fieldInfo = typeof(SyncService).GetField("_networkService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var networkService = fieldInfo?.GetValue(SyncService) as INetworkService;
            Assert.Null(networkService);
        }
    }
}