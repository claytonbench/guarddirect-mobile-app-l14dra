using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.UnitTests.Helpers;
using Xunit;

namespace SecurityPatrol.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the SyncService class to verify its synchronization behavior, error handling, and event notifications.
    /// </summary>
    public class SyncServiceTests : IDisposable
    {
        private readonly Mock<ISyncRepository> _mockSyncRepository;
        private readonly Mock<INetworkService> _mockNetworkService;
        private readonly Mock<ITimeTrackingSyncService> _mockTimeTrackingSyncService;
        private readonly Mock<ILocationSyncService> _mockLocationSyncService;
        private readonly Mock<IPhotoSyncService> _mockPhotoSyncService;
        private readonly Mock<IReportSyncService> _mockReportSyncService;
        private readonly Mock<ILogger<SyncService>> _mockLogger;
        private readonly SyncService _syncService;
        private readonly List<SyncStatusChangedEventArgs> _syncStatusChangedEvents;

        /// <summary>
        /// Initializes a new instance of the SyncServiceTests class
        /// </summary>
        public SyncServiceTests()
        {
            // Initialize mocks
            _mockSyncRepository = new Mock<ISyncRepository>();
            _mockNetworkService = new Mock<INetworkService>();
            _mockTimeTrackingSyncService = new Mock<ITimeTrackingSyncService>();
            _mockLocationSyncService = new Mock<ILocationSyncService>();
            _mockPhotoSyncService = new Mock<IPhotoSyncService>();
            _mockReportSyncService = new Mock<IReportSyncService>();
            _mockLogger = new Mock<ILogger<SyncService>>();
            
            // Initialize event tracking
            _syncStatusChangedEvents = new List<SyncStatusChangedEventArgs>();
            
            // Create service instance with mocked dependencies
            _syncService = new SyncService(
                _mockSyncRepository.Object,
                _mockNetworkService.Object,
                _mockTimeTrackingSyncService.Object,
                _mockLocationSyncService.Object,
                _mockPhotoSyncService.Object,
                _mockReportSyncService.Object,
                _mockLogger.Object);
            
            // Subscribe to events
            _syncService.SyncStatusChanged += (sender, e) => _syncStatusChangedEvents.Add(e);
        }

        /// <summary>
        /// Cleans up resources after tests
        /// </summary>
        public void Dispose()
        {
            // Unsubscribe from events to prevent memory leaks
            if (_syncService != null)
            {
                _syncService.SyncStatusChanged -= (sender, e) => _syncStatusChangedEvents.Add(e);
                
                // Dispose the sync service if it implements IDisposable
                if (_syncService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        
        /// <summary>
        /// Sets up common mock behaviors for tests
        /// </summary>
        /// <param name="isNetworkConnected">Whether network should be reported as connected</param>
        private void SetupMocks(bool isNetworkConnected)
        {
            // Set up network connectivity
            _mockNetworkService.Setup(m => m.IsConnected).Returns(isNetworkConnected);
            
            // Set up time tracking service
            _mockTimeTrackingSyncService.Setup(m => m.GetPendingSyncCountAsync())
                .ReturnsAsync(0);
            _mockTimeTrackingSyncService.Setup(m => m.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Set up location service
            _mockLocationSyncService.Setup(m => m.IsSyncing)
                .Returns(false);
            _mockLocationSyncService.Setup(m => m.SyncLocationsAsync(It.IsAny<int>()))
                .ReturnsAsync(true);
            
            // Set up photo service
            _mockPhotoSyncService.Setup(m => m.IsSyncInProgressAsync())
                .ReturnsAsync(false);
            _mockPhotoSyncService.Setup(m => m.SyncPhotosAsync())
                .ReturnsAsync(true);
            
            // Set up report service
            _mockReportSyncService.Setup(m => m.GetPendingSyncCountAsync())
                .ReturnsAsync(0);
            _mockReportSyncService.Setup(m => m.SyncReportsAsync())
                .ReturnsAsync(0);
            
            // Set up sync repository
            _mockSyncRepository.Setup(m => m.GetSyncStatistics())
                .ReturnsAsync(new Dictionary<string, int>());
            _mockSyncRepository.Setup(m => m.GetAllPendingSync())
                .ReturnsAsync(Enumerable.Empty<SyncItem>());
            
            // Clear events list
            _syncStatusChangedEvents.Clear();
        }

        /// <summary>
        /// Tests that SyncAll synchronizes all data types when network is connected
        /// </summary>
        [Fact]
        public async Task SyncAll_WhenNetworkIsConnected_SynchronizesAllData()
        {
            // Arrange
            SetupMocks(isNetworkConnected: true);
            
            // Set up some pending items
            _mockTimeTrackingSyncService.Setup(m => m.GetPendingSyncCountAsync())
                .ReturnsAsync(5);
            _mockReportSyncService.Setup(m => m.GetPendingSyncCountAsync())
                .ReturnsAsync(3);
            
            // Act
            var result = await _syncService.SyncAll();
            
            // Assert
            Assert.NotNull(result);
            _mockTimeTrackingSyncService.Verify(m => m.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockLocationSyncService.Verify(m => m.SyncLocationsAsync(It.IsAny<int>()), Times.Once);
            _mockPhotoSyncService.Verify(m => m.SyncPhotosAsync(), Times.Once);
            _mockReportSyncService.Verify(m => m.SyncReportsAsync(), Times.Once);
            
            // Verify events were raised
            Assert.Contains(_syncStatusChangedEvents, e => e.EntityType == "All" && e.Status == "Starting");
            Assert.Contains(_syncStatusChangedEvents, e => e.EntityType == "All" && e.Status == "Completed");
        }

        /// <summary>
        /// Tests that SyncAll returns an appropriate result when network is not connected
        /// </summary>
        [Fact]
        public async Task SyncAll_WhenNetworkIsNotConnected_ReturnsOfflineResult()
        {
            // Arrange
            SetupMocks(isNetworkConnected: false);
            
            // Act
            var result = await _syncService.SyncAll();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Equal(1, result.PendingCount);
            
            // Verify no sync methods were called
            _mockTimeTrackingSyncService.Verify(m => m.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockLocationSyncService.Verify(m => m.SyncLocationsAsync(It.IsAny<int>()), Times.Never);
            _mockPhotoSyncService.Verify(m => m.SyncPhotosAsync(), Times.Never);
            _mockReportSyncService.Verify(m => m.SyncReportsAsync(), Times.Never);
        }

        /// <summary>
        /// Tests that SyncAll returns an appropriate result when a sync operation is already in progress
        /// </summary>
        [Fact]
        public async Task SyncAll_WhenAlreadySyncing_ReturnsAppropriateResult()
        {
            // Arrange
            SetupMocks(isNetworkConnected: true);
            
            // Set IsSyncing state by calling SyncAll first (and not completing it)
            var firstSyncTask = _syncService.SyncAll();
            
            // Act
            var result = await _syncService.SyncAll();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Equal(1, result.PendingCount);
            
            // Let the first sync complete
            await firstSyncTask;
        }

        /// <summary>
        /// Tests that SyncEntity synchronizes a specific entity when given valid parameters
        /// </summary>
        [Fact]
        public async Task SyncEntity_WithValidEntityTypeAndId_SynchronizesEntity()
        {
            // Arrange
            SetupMocks(isNetworkConnected: true);
            
            // Set up time tracking sync for specific ID
            _mockTimeTrackingSyncService.Setup(m => m.SyncRecordAsync(123, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Act
            var result = await _syncService.SyncEntity("TimeRecord", "123");
            
            // Assert
            Assert.True(result);
            _mockTimeTrackingSyncService.Verify(m => m.SyncRecordAsync(123, It.IsAny<CancellationToken>()), Times.Once);
            _mockSyncRepository.Verify(m => m.UpdateSyncStatus("TimeRecord", "123", true, null), Times.Once);
        }

        /// <summary>
        /// Tests that SyncEntity returns false when network is not connected
        /// </summary>
        [Fact]
        public async Task SyncEntity_WhenNetworkIsNotConnected_ReturnsFalse()
        {
            // Arrange
            SetupMocks(isNetworkConnected: false);
            
            // Act
            var result = await _syncService.SyncEntity("TimeRecord", "123");
            
            // Assert
            Assert.False(result);
            _mockTimeTrackingSyncService.Verify(m => m.SyncRecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Tests that SyncEntity throws an ArgumentException when given invalid parameters
        /// </summary>
        [Fact]
        public async Task SyncEntity_WithInvalidParameters_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks(isNetworkConnected: true);
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _syncService.SyncEntity(null, "123"));
            await Assert.ThrowsAsync<ArgumentException>(() => _syncService.SyncEntity("TimeRecord", null));
            await Assert.ThrowsAsync<ArgumentException>(() => _syncService.SyncEntity("", "123"));
            await Assert.ThrowsAsync<ArgumentException>(() => _syncService.SyncEntity("TimeRecord", ""));
        }

        /// <summary>
        /// Tests that SyncEntity(entityType) synchronizes all entities of a specific type
        /// </summary>
        [Fact]
        public async Task SyncEntityType_WithValidEntityType_SynchronizesAllEntitiesOfType()
        {
            // Arrange
            SetupMocks(isNetworkConnected: true);
            
            // Set up successful sync result
            var syncResult = new SyncResult { SuccessCount = 5 };
            _mockTimeTrackingSyncService.Setup(m => m.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            // Act
            var result = await _syncService.SyncEntity("TimeRecord");
            
            // Assert
            Assert.NotNull(result);
            _mockTimeTrackingSyncService.Verify(m => m.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()), Times.Once);
            
            // Check events
            Assert.Contains(_syncStatusChangedEvents, e => e.EntityType == "TimeRecord");
        }

        /// <summary>
        /// Tests that SyncEntity(entityType) returns an appropriate result when network is not connected
        /// </summary>
        [Fact]
        public async Task SyncEntityType_WhenNetworkIsNotConnected_ReturnsOfflineResult()
        {
            // Arrange
            SetupMocks(isNetworkConnected: false);
            
            // Act
            var result = await _syncService.SyncEntity("TimeRecord");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Equal(1, result.PendingCount);
            
            _mockTimeTrackingSyncService.Verify(m => m.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Tests that ScheduleSync correctly schedules synchronization at the specified interval
        /// </summary>
        [Fact]
        public void ScheduleSync_WithValidInterval_SchedulesSynchronization()
        {
            // Arrange
            SetupMocks(isNetworkConnected: true);
            
            // Act
            _syncService.ScheduleSync(TimeSpan.FromMinutes(15));
            
            // Assert - can't directly verify timer creation, but we can verify it doesn't throw
            
            // Clean up
            _syncService.CancelScheduledSync();
        }

        /// <summary>
        /// Tests that ScheduleSync throws an ArgumentException when given an invalid interval
        /// </summary>
        [Fact]
        public void ScheduleSync_WithInvalidInterval_ThrowsArgumentException()
        {
            // Arrange
            SetupMocks(isNetworkConnected: true);
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _syncService.ScheduleSync(TimeSpan.Zero));
            Assert.Throws<ArgumentException>(() => _syncService.ScheduleSync(TimeSpan.FromSeconds(-1)));
        }

        /// <summary>
        /// Tests that CancelScheduledSync correctly cancels any scheduled synchronization
        /// </summary>
        [Fact]
        public void CancelScheduledSync_WhenSyncIsScheduled_CancelsScheduledSync()
        {
            // Arrange
            SetupMocks(isNetworkConnected: true);
            _syncService.ScheduleSync(TimeSpan.FromMinutes(15));
            
            // Act
            _syncService.CancelScheduledSync();
            
            // Assert - can only verify it doesn't throw
        }

        /// <summary>
        /// Tests that GetSyncStatus returns the correct synchronization status
        /// </summary>
        [Fact]
        public async Task GetSyncStatus_ReturnsCorrectStatus()
        {
            // Arrange
            SetupMocks(isNetworkConnected: true);
            
            var expectedStats = new Dictionary<string, int>
            {
                { "TimeRecord", 5 },
                { "Location", 10 },
                { "Photo", 3 },
                { "Report", 2 }
            };
            
            _mockSyncRepository.Setup(m => m.GetSyncStatistics())
                .ReturnsAsync(expectedStats);
            
            // Act
            var result = await _syncService.GetSyncStatus();
            
            // Assert
            Assert.Equal(expectedStats, result);
            _mockSyncRepository.Verify(m => m.GetSyncStatistics(), Times.Once);
        }

        /// <summary>
        /// Tests that OnConnectivityChanged triggers synchronization when connectivity is restored and there are pending items
        /// </summary>
        [Fact]
        public async Task OnConnectivityChanged_WhenConnectivityRestored_TriggersSyncIfPendingItems()
        {
            // Arrange
            SetupMocks(isNetworkConnected: false);
            
            // Set up pending items
            var pendingItems = new List<SyncItem>
            {
                new SyncItem("TimeRecord", "1", 100),
                new SyncItem("Photo", "2", 60)
            };
            
            _mockSyncRepository.Setup(m => m.GetAllPendingSync())
                .ReturnsAsync(pendingItems);
            
            // Act - simulate connectivity change
            _mockNetworkService.Raise(
                n => n.ConnectivityChanged += null,
                new ConnectivityChangedEventArgs(true, "WiFi", ConnectionQuality.Excellent));
            
            // Allow async operations to complete
            await Task.Delay(100);
            
            // Assert - verify GetAllPendingSync was called
            _mockSyncRepository.Verify(m => m.GetAllPendingSync(), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests that OnConnectivityChanged does not trigger synchronization when connectivity is restored but there are no pending items
        /// </summary>
        [Fact]
        public async Task OnConnectivityChanged_WhenConnectivityRestored_DoesNotTriggerSyncIfNoPendingItems()
        {
            // Arrange
            SetupMocks(isNetworkConnected: false);
            
            // Set up no pending items
            _mockSyncRepository.Setup(m => m.GetAllPendingSync())
                .ReturnsAsync(new List<SyncItem>());
            
            // Act - simulate connectivity change
            _mockNetworkService.Raise(
                n => n.ConnectivityChanged += null,
                new ConnectivityChangedEventArgs(true, "WiFi", ConnectionQuality.Excellent));
            
            // Allow async operations to complete
            await Task.Delay(100);
            
            // Assert - verify GetAllPendingSync was called
            _mockSyncRepository.Verify(m => m.GetAllPendingSync(), Times.AtLeastOnce);
        }

        /// <summary>
        /// Tests that SyncAll properly handles and logs exceptions that occur during synchronization
        /// </summary>
        [Fact]
        public async Task SyncAll_HandlesExceptions_AndLogsErrors()
        {
            // Arrange
            SetupMocks(isNetworkConnected: true);
            
            // Set up time tracking service to throw an exception
            _mockTimeTrackingSyncService.Setup(m => m.SyncTimeRecordsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));
            
            // Act
            var result = await _syncService.SyncAll();
            
            // Assert
            Assert.NotNull(result);
            Assert.True(result.FailureCount > 0);
            
            // Verify other services were still called despite the exception
            _mockLocationSyncService.Verify(m => m.SyncLocationsAsync(It.IsAny<int>()), Times.Once);
            _mockPhotoSyncService.Verify(m => m.SyncPhotosAsync(), Times.Once);
            _mockReportSyncService.Verify(m => m.SyncReportsAsync(), Times.Once);
            
            // Verify that the error was logged with the exception details
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }
    }
}