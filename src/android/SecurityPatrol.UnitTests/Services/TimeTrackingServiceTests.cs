using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SecurityPatrol.Services;
using SecurityPatrol.Models;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Services
{
    public class TimeTrackingServiceTests
    {
        private readonly Mock<ITimeRecordRepository> _mockRepository;
        private readonly Mock<ILocationService> _mockLocationService;
        private readonly Mock<IAuthenticationStateProvider> _mockAuthStateProvider;
        private readonly Mock<ITimeTrackingSyncService> _mockSyncService;
        private readonly Mock<ILogger<TimeTrackingService>> _mockLogger;
        private readonly TimeTrackingService _service;
        private readonly string _testUserId = "+15551234567";

        public TimeTrackingServiceTests()
        {
            // Initialize mocks
            _mockRepository = new Mock<ITimeRecordRepository>();
            _mockLocationService = new Mock<ILocationService>();
            _mockAuthStateProvider = new Mock<IAuthenticationStateProvider>();
            _mockSyncService = new Mock<ITimeTrackingSyncService>();
            _mockLogger = new Mock<ILogger<TimeTrackingService>>();

            // Setup default behaviors
            _mockAuthStateProvider.Setup(a => a.GetCurrentState())
                .ReturnsAsync(TestDataGenerator.CreateAuthState(true, _testUserId));
            
            _mockLocationService.Setup(l => l.GetCurrentLocation())
                .ReturnsAsync(TestDataGenerator.CreateLocationModel());

            // Create service with mocked dependencies
            _service = new TimeTrackingService(
                _mockRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Initialize_ShouldSetupCorrectInitialState()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetLatestClockInEventAsync()).ReturnsAsync((TimeRecordModel)null);
            _mockRepository.Setup(r => r.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);

            // Act
            var service = new TimeTrackingService(
                _mockRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);
            
            var status = await service.GetCurrentStatus();

            // Assert
            Assert.NotNull(status);
            Assert.False(status.IsClocked);
            Assert.Null(status.LastClockInTime);
            Assert.Null(status.LastClockOutTime);
            
            _mockRepository.Verify(r => r.GetLatestClockInEventAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetLatestClockOutEventAsync(), Times.Once);
        }

        [Fact]
        public async Task Initialize_WithExistingClockInEvent_ShouldSetClockedInState()
        {
            // Arrange
            var clockInRecord = TestDataGenerator.CreateClockInRecord(_testUserId);
            
            _mockRepository.Setup(r => r.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockRepository.Setup(r => r.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);

            // Act
            var service = new TimeTrackingService(
                _mockRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);
            
            var status = await service.GetCurrentStatus();

            // Assert
            Assert.NotNull(status);
            Assert.True(status.IsClocked);
            Assert.Equal(clockInRecord.Timestamp, status.LastClockInTime);
            Assert.Null(status.LastClockOutTime);
        }

        [Fact]
        public async Task Initialize_WithExistingClockOutEvent_ShouldSetClockedOutState()
        {
            // Arrange
            var clockInTime = DateTime.UtcNow.AddHours(-2);
            var clockOutTime = DateTime.UtcNow.AddHours(-1);
            
            var clockInRecord = TestDataGenerator.CreateTimeRecord(
                1, _testUserId, "ClockIn", clockInTime);
            var clockOutRecord = TestDataGenerator.CreateTimeRecord(
                2, _testUserId, "ClockOut", clockOutTime);
            
            _mockRepository.Setup(r => r.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockRepository.Setup(r => r.GetLatestClockOutEventAsync()).ReturnsAsync(clockOutRecord);

            // Act
            var service = new TimeTrackingService(
                _mockRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);
            
            var status = await service.GetCurrentStatus();

            // Assert
            Assert.NotNull(status);
            Assert.False(status.IsClocked);
            Assert.Equal(clockInTime, status.LastClockInTime);
            Assert.Equal(clockOutTime, status.LastClockOutTime);
        }

        [Fact]
        public async Task ClockIn_WhenNotAuthenticated_ShouldThrowUnauthorizedException()
        {
            // Arrange
            _mockAuthStateProvider.Setup(a => a.GetCurrentState())
                .ReturnsAsync(TestDataGenerator.CreateAuthState(false));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ClockIn());
        }

        [Fact]
        public async Task ClockIn_WhenAlreadyClockedIn_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var clockInRecord = TestDataGenerator.CreateClockInRecord(_testUserId);
            
            _mockRepository.Setup(r => r.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockRepository.Setup(r => r.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);

            var service = new TimeTrackingService(
                _mockRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClockIn());
        }

        [Fact]
        public async Task ClockIn_WhenAuthenticated_ShouldCreateClockInRecord()
        {
            // Arrange
            _mockRepository.Setup(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()))
                .ReturnsAsync(1); // Return ID of 1

            // Act
            var result = await _service.ClockIn();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ClockIn", result.Type);
            Assert.Equal(_testUserId, result.UserId);
            
            _mockRepository.Verify(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Once);
            _mockLocationService.Verify(l => l.StartTracking(), Times.Once);
            _mockSyncService.Verify(s => s.SyncRecordAsync(It.IsAny<TimeRecordModel>(), default), Times.Once);
        }

        [Fact]
        public async Task ClockIn_ShouldRaiseStatusChangedEvent()
        {
            // Arrange
            _mockRepository.Setup(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()))
                .ReturnsAsync(1);
            
            bool eventRaised = false;
            ClockStatus eventStatus = null;
            
            _service.StatusChanged += (sender, args) => {
                eventRaised = true;
                eventStatus = args.Status;
            };

            // Act
            await _service.ClockIn();

            // Assert
            Assert.True(eventRaised);
            Assert.True(eventStatus.IsClocked);
        }

        [Fact]
        public async Task ClockOut_WhenNotAuthenticated_ShouldThrowUnauthorizedException()
        {
            // Arrange
            _mockAuthStateProvider.Setup(a => a.GetCurrentState())
                .ReturnsAsync(TestDataGenerator.CreateAuthState(false));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ClockOut());
        }

        [Fact]
        public async Task ClockOut_WhenNotClockedIn_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var clockInTime = DateTime.UtcNow.AddHours(-2);
            var clockOutTime = DateTime.UtcNow.AddHours(-1);
            
            var clockInRecord = TestDataGenerator.CreateTimeRecord(
                1, _testUserId, "ClockIn", clockInTime);
            var clockOutRecord = TestDataGenerator.CreateTimeRecord(
                2, _testUserId, "ClockOut", clockOutTime);
            
            _mockRepository.Setup(r => r.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockRepository.Setup(r => r.GetLatestClockOutEventAsync()).ReturnsAsync(clockOutRecord);

            var service = new TimeTrackingService(
                _mockRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClockOut());
        }

        [Fact]
        public async Task ClockOut_WhenClockedIn_ShouldCreateClockOutRecord()
        {
            // Arrange
            var clockInRecord = TestDataGenerator.CreateClockInRecord(_testUserId);
            
            _mockRepository.Setup(r => r.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockRepository.Setup(r => r.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);
            _mockRepository.Setup(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()))
                .ReturnsAsync(2); // Return ID of 2

            var service = new TimeTrackingService(
                _mockRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);

            // Act
            var result = await service.ClockOut();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ClockOut", result.Type);
            Assert.Equal(_testUserId, result.UserId);
            
            _mockRepository.Verify(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Once);
            _mockLocationService.Verify(l => l.StopTracking(), Times.Once);
            _mockSyncService.Verify(s => s.SyncRecordAsync(It.IsAny<TimeRecordModel>(), default), Times.Once);
        }

        [Fact]
        public async Task ClockOut_ShouldRaiseStatusChangedEvent()
        {
            // Arrange
            var clockInRecord = TestDataGenerator.CreateClockInRecord(_testUserId);
            
            _mockRepository.Setup(r => r.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockRepository.Setup(r => r.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);
            _mockRepository.Setup(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()))
                .ReturnsAsync(2);

            var service = new TimeTrackingService(
                _mockRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);
            
            bool eventRaised = false;
            ClockStatus eventStatus = null;
            
            service.StatusChanged += (sender, args) => {
                eventRaised = true;
                eventStatus = args.Status;
            };

            // Act
            await service.ClockOut();

            // Assert
            Assert.True(eventRaised);
            Assert.False(eventStatus.IsClocked);
        }

        [Fact]
        public async Task GetCurrentStatus_ShouldReturnCurrentStatus()
        {
            // Act
            var status = await _service.GetCurrentStatus();

            // Assert
            Assert.NotNull(status);
            // We don't test specific values here as they depend on the initial state,
            // which is already tested in the Initialize_* tests
        }

        [Fact]
        public async Task GetHistory_WithInvalidCount_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetHistory(0));
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetHistory(-1));
        }

        [Fact]
        public async Task GetHistory_WithValidCount_ShouldReturnRecords()
        {
            // Arrange
            var records = new List<TimeRecordModel>
            {
                TestDataGenerator.CreateTimeRecord(1, _testUserId, "ClockIn", DateTime.UtcNow.AddHours(-2)),
                TestDataGenerator.CreateTimeRecord(2, _testUserId, "ClockOut", DateTime.UtcNow.AddHours(-1))
            };
            
            _mockRepository.Setup(r => r.GetTimeRecordsAsync(It.IsAny<int>()))
                .ReturnsAsync(records);

            // Act
            var result = await _service.GetHistory(5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(records.Count, result.Count());
            _mockRepository.Verify(r => r.GetTimeRecordsAsync(5), Times.Once);
        }

        [Fact]
        public async Task ClockIn_WhenRepositoryFails_ShouldThrowException()
        {
            // Arrange
            _mockRepository.Setup(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.ClockIn());
        }

        [Fact]
        public async Task ClockOut_WhenRepositoryFails_ShouldThrowException()
        {
            // Arrange
            var clockInRecord = TestDataGenerator.CreateClockInRecord(_testUserId);
            
            _mockRepository.Setup(r => r.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockRepository.Setup(r => r.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);
            _mockRepository.Setup(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()))
                .ThrowsAsync(new Exception("Database error"));

            var service = new TimeTrackingService(
                _mockRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.ClockOut());
        }

        [Fact]
        public async Task ClockIn_WhenSyncFails_ShouldNotThrowException()
        {
            // Arrange
            _mockRepository.Setup(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()))
                .ReturnsAsync(1);
            
            _mockSyncService.Setup(s => s.SyncRecordAsync(It.IsAny<TimeRecordModel>(), default))
                .ThrowsAsync(new Exception("Sync error"));

            // Act
            var result = await _service.ClockIn(); // This should not throw

            // Assert
            Assert.NotNull(result);
            _mockRepository.Verify(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Once);
            _mockLocationService.Verify(l => l.StartTracking(), Times.Once);
            // Despite sync error, the operation should complete
        }

        [Fact]
        public async Task ClockOut_WhenSyncFails_ShouldNotThrowException()
        {
            // Arrange
            var clockInRecord = TestDataGenerator.CreateClockInRecord(_testUserId);
            
            _mockRepository.Setup(r => r.GetLatestClockInEventAsync()).ReturnsAsync(clockInRecord);
            _mockRepository.Setup(r => r.GetLatestClockOutEventAsync()).ReturnsAsync((TimeRecordModel)null);
            _mockRepository.Setup(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()))
                .ReturnsAsync(2);
            
            _mockSyncService.Setup(s => s.SyncRecordAsync(It.IsAny<TimeRecordModel>(), default))
                .ThrowsAsync(new Exception("Sync error"));

            var service = new TimeTrackingService(
                _mockRepository.Object,
                _mockLocationService.Object,
                _mockAuthStateProvider.Object,
                _mockSyncService.Object,
                _mockLogger.Object);

            // Act
            var result = await service.ClockOut(); // This should not throw

            // Assert
            Assert.NotNull(result);
            _mockRepository.Verify(r => r.SaveTimeRecordAsync(It.IsAny<TimeRecordModel>()), Times.Once);
            _mockLocationService.Verify(l => l.StopTracking(), Times.Once);
            // Despite sync error, the operation should complete
        }
    }
}