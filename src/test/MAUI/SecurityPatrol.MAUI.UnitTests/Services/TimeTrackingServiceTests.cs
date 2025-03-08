using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.MAUI.UnitTests.Setup;
using System.Threading;

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the TimeTrackingService class to verify its functionality for clock in/out operations,
    /// status tracking, and history retrieval.
    /// </summary>
    public class TimeTrackingServiceTests
    {
        private readonly Mock<ITimeRecordRepository> _mockTimeRecordRepository;
        private readonly Mock<ILocationService> _mockLocationService;
        private readonly Mock<IAuthenticationStateProvider> _mockAuthStateProvider;
        private readonly Mock<ITimeTrackingSyncService> _mockSyncService;
        private readonly Mock<ILogger<TimeTrackingService>> _mockLogger;
        private readonly TimeTrackingService _timeTrackingService;

        /// <summary>
        /// Initializes a new instance of the TimeTrackingServiceTests class with mocked dependencies
        /// </summary>
        public TimeTrackingServiceTests()
        {
            _mockTimeRecordRepository = new Mock<ITimeRecordRepository>();
            _mockLocationService = new Mock<ILocationService>();
            _mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            _mockSyncService = new Mock<ITimeTrackingSyncService>();
            _mockLogger = new Mock<ILogger<TimeTrackingService>>();

            // Setup default mock behaviors
            SetupMocks();

            // Create the service with mocked dependencies
            _timeTrackingService = new TimeTrackingService(
                _mockTimeRecordRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);
        }

        /// <summary>
        /// Sets up the default behaviors for the mock dependencies
        /// </summary>
        private void SetupMocks()
        {
            // Setup authentication state
            _mockAuthStateProvider.Setup(x => x.IsAuthenticated()).ReturnsAsync(true);
            _mockAuthStateProvider.Setup(x => x.GetCurrentState()).ReturnsAsync(new AuthState { IsAuthenticated = true, PhoneNumber = TestConstants.TestUserId });

            // Setup location service
            _mockLocationService.Setup(x => x.GetCurrentLocation()).ReturnsAsync(new LocationModel { Latitude = 37.7749, Longitude = -122.4194 });

            // Setup repository
            _mockTimeRecordRepository.Setup(x => x.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>())).ReturnsAsync(1);
            _mockTimeRecordRepository.Setup(x => x.GetTimeRecordsAsync(It.IsAny<int>())).ReturnsAsync(new List<TimeRecordModel>());
            
            // Default to not clocked in
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockInEventAsync()).ReturnsAsync((TimeRecordModel)null);
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);

            // Setup sync service
            _mockSyncService.Setup(x => x.SyncRecordAsync(It.IsAny<TimeRecordModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        /// <summary>
        /// Tests that ClockIn succeeds when the user is authenticated
        /// </summary>
        [Fact]
        public async Task ClockIn_WhenUserIsAuthenticated_ShouldSucceed()
        {
            // Arrange - already done in constructor

            // Act
            var result = await _timeTrackingService.ClockIn();

            // Assert
            result.Should().NotBeNull();
            result.IsClockIn().Should().BeTrue();
            _mockTimeRecordRepository.Verify(x => x.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Once);
            _mockLocationService.Verify(x => x.StartTracking(), Times.Once);
            _mockSyncService.Verify(x => x.SyncRecordAsync(It.IsAny<TimeRecordModel>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that ClockIn throws UnauthorizedException when the user is not authenticated
        /// </summary>
        [Fact]
        public async Task ClockIn_WhenUserIsNotAuthenticated_ShouldThrowUnauthorizedException()
        {
            // Arrange
            _mockAuthStateProvider.Setup(x => x.GetCurrentState()).ReturnsAsync(new AuthState { IsAuthenticated = false });

            // Act
            Func<Task> action = async () => await _timeTrackingService.ClockIn();

            // Assert
            await action.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockTimeRecordRepository.Verify(x => x.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Never);
            _mockLocationService.Verify(x => x.StartTracking(), Times.Never);
        }

        /// <summary>
        /// Tests that ClockIn throws InvalidOperationException when the user is already clocked in
        /// </summary>
        [Fact]
        public async Task ClockIn_WhenAlreadyClockedIn_ShouldThrowInvalidOperationException()
        {
            // Arrange - make it look like we're already clocked in
            var clockInRecord = new TimeRecordModel { Id = 1, Type = "ClockIn", Timestamp = DateTime.Now.AddHours(-1) };
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);

            // Create a new instance to pick up the "clocked in" state
            var service = new TimeTrackingService(
                _mockTimeRecordRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);

            // Act
            Func<Task> action = async () => await service.ClockIn();

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
            _mockTimeRecordRepository.Verify(x => x.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Never);
            _mockLocationService.Verify(x => x.StartTracking(), Times.Never);
        }

        /// <summary>
        /// Tests that ClockOut succeeds when the user is authenticated and clocked in
        /// </summary>
        [Fact]
        public async Task ClockOut_WhenUserIsAuthenticated_ShouldSucceed()
        {
            // Arrange - make it look like we're clocked in
            var clockInRecord = new TimeRecordModel { Id = 1, Type = "ClockIn", Timestamp = DateTime.Now.AddHours(-1) };
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);

            // Create a new instance to pick up the "clocked in" state
            var service = new TimeTrackingService(
                _mockTimeRecordRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);

            // Act
            var result = await service.ClockOut();

            // Assert
            result.Should().NotBeNull();
            result.IsClockOut().Should().BeTrue();
            _mockTimeRecordRepository.Verify(x => x.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Once);
            _mockLocationService.Verify(x => x.StopTracking(), Times.Once);
            _mockSyncService.Verify(x => x.SyncRecordAsync(It.IsAny<TimeRecordModel>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that ClockOut throws UnauthorizedException when the user is not authenticated
        /// </summary>
        [Fact]
        public async Task ClockOut_WhenUserIsNotAuthenticated_ShouldThrowUnauthorizedException()
        {
            // Arrange
            _mockAuthStateProvider.Setup(x => x.GetCurrentState()).ReturnsAsync(new AuthState { IsAuthenticated = false });

            // Act
            Func<Task> action = async () => await _timeTrackingService.ClockOut();

            // Assert
            await action.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockTimeRecordRepository.Verify(x => x.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Never);
            _mockLocationService.Verify(x => x.StopTracking(), Times.Never);
        }

        /// <summary>
        /// Tests that ClockOut throws InvalidOperationException when the user is not clocked in
        /// </summary>
        [Fact]
        public async Task ClockOut_WhenNotClockedIn_ShouldThrowInvalidOperationException()
        {
            // Arrange - make it look like we're not clocked in
            var clockOutRecord = new TimeRecordModel { Id = 1, Type = "ClockOut", Timestamp = DateTime.Now.AddHours(-1) };
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockInEventAsync()).ReturnsAsync((TimeRecordModel)null);
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockOutEventAsync()).ReturnsAsync(clockOutRecord);

            // Create a new instance to pick up the "not clocked in" state
            var service = new TimeTrackingService(
                _mockTimeRecordRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);

            // Act
            Func<Task> action = async () => await service.ClockOut();

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
            _mockTimeRecordRepository.Verify(x => x.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Never);
            _mockLocationService.Verify(x => x.StopTracking(), Times.Never);
        }

        /// <summary>
        /// Tests that GetCurrentStatus returns the correct clock status
        /// </summary>
        [Fact]
        public async Task GetCurrentStatus_ShouldReturnCorrectStatus()
        {
            // Arrange
            var clockInTime = DateTime.Now.AddHours(-8);
            var clockOutTime = DateTime.Now.AddHours(-1);
            
            var clockInRecord = new TimeRecordModel { Id = 1, Type = "ClockIn", Timestamp = clockInTime };
            var clockOutRecord = new TimeRecordModel { Id = 2, Type = "ClockOut", Timestamp = clockOutTime };
            
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockOutEventAsync()).ReturnsAsync(clockOutRecord);

            // Create a new instance to pick up the setup state
            var service = new TimeTrackingService(
                _mockTimeRecordRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);

            // Act
            var result = await service.GetCurrentStatus();

            // Assert
            result.Should().NotBeNull();
            result.IsClocked.Should().BeFalse(); // Not clocked in because clockOutTime > clockInTime
            result.LastClockInTime.Should().Be(clockInTime);
            result.LastClockOutTime.Should().Be(clockOutTime);
        }

        /// <summary>
        /// Tests that GetHistory returns the correct number of time records
        /// </summary>
        [Fact]
        public async Task GetHistory_ShouldReturnCorrectRecords()
        {
            // Arrange
            var records = new List<TimeRecordModel>
            {
                new TimeRecordModel { Id = 1, Type = "ClockIn", Timestamp = DateTime.Now.AddHours(-9) },
                new TimeRecordModel { Id = 2, Type = "ClockOut", Timestamp = DateTime.Now.AddHours(-8) },
                new TimeRecordModel { Id = 3, Type = "ClockIn", Timestamp = DateTime.Now.AddHours(-4) },
                new TimeRecordModel { Id = 4, Type = "ClockOut", Timestamp = DateTime.Now.AddHours(-3) }
            };
            
            _mockTimeRecordRepository.Setup(x => x.GetTimeRecordsAsync(5)).ReturnsAsync(records);

            // Act
            var result = await _timeTrackingService.GetHistory(5);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4);
            _mockTimeRecordRepository.Verify(x => x.GetTimeRecordsAsync(5), Times.Once);
        }

        /// <summary>
        /// Tests that GetHistory throws ArgumentException when count is invalid
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetHistory_WithInvalidCount_ShouldThrowArgumentException(int count)
        {
            // Act
            Func<Task> action = async () => await _timeTrackingService.GetHistory(count);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>();
            _mockTimeRecordRepository.Verify(x => x.GetTimeRecordsAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Tests that StatusChanged event is raised when clocking in
        /// </summary>
        [Fact]
        public async Task StatusChanged_ShouldRaiseEventOnClockIn()
        {
            // Arrange
            bool eventRaised = false;
            ClockStatus capturedStatus = null;
            
            _timeTrackingService.StatusChanged += (sender, e) => 
            {
                eventRaised = true;
                capturedStatus = e.Status;
            };

            // Act
            await _timeTrackingService.ClockIn();

            // Assert
            eventRaised.Should().BeTrue();
            capturedStatus.Should().NotBeNull();
            capturedStatus.IsClocked.Should().BeTrue();
        }

        /// <summary>
        /// Tests that StatusChanged event is raised when clocking out
        /// </summary>
        [Fact]
        public async Task StatusChanged_ShouldRaiseEventOnClockOut()
        {
            // Arrange
            bool eventRaised = false;
            ClockStatus capturedStatus = null;
            
            var clockInRecord = new TimeRecordModel { Id = 1, Type = "ClockIn", Timestamp = DateTime.Now.AddHours(-1) };
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockTimeRecordRepository.Setup(x => x.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);

            // Create a new service instance to pick up clocked-in state
            var service = new TimeTrackingService(
                _mockTimeRecordRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);
            
            service.StatusChanged += (sender, e) => 
            {
                eventRaised = true;
                capturedStatus = e.Status;
            };

            // Act
            await service.ClockOut();

            // Assert
            eventRaised.Should().BeTrue();
            capturedStatus.Should().NotBeNull();
            capturedStatus.IsClocked.Should().BeFalse();
        }

        /// <summary>
        /// Tests that a sync failure does not prevent a successful clock operation
        /// </summary>
        [Fact]
        public async Task SyncFailure_ShouldNotPreventClockOperation()
        {
            // Arrange
            _mockSyncService.Setup(x => x.SyncRecordAsync(It.IsAny<TimeRecordModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            var result = await _timeTrackingService.ClockIn();

            // Assert
            result.Should().NotBeNull();
            result.IsClockIn().Should().BeTrue();
            _mockTimeRecordRepository.Verify(x => x.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Once);
            _mockLocationService.Verify(x => x.StartTracking(), Times.Once);
            _mockSyncService.Verify(x => x.SyncRecordAsync(It.IsAny<TimeRecordModel>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}