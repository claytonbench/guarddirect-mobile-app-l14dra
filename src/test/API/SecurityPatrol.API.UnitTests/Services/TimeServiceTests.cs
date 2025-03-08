using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Exceptions;

namespace SecurityPatrol.API.UnitTests.Services
{
    /// <summary>
    /// Contains unit tests for the TimeRecordService class to verify its behavior and functionality.
    /// </summary>
    public class TimeServiceTests
    {
        private readonly Mock<ITimeRecordRepository> _mockTimeRecordRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IDateTime> _mockDateTime;
        private readonly Mock<ILogger<TimeRecordService>> _mockLogger;
        private readonly TimeRecordService _timeRecordService;
        private readonly DateTime _fixedDateTime;
        private readonly string _testUserId;

        public TimeServiceTests()
        {
            // Initialize test values
            _testUserId = "test-user-123";
            _fixedDateTime = new DateTime(2023, 6, 15, 10, 30, 0, DateTimeKind.Utc);

            // Initialize mocks
            _mockTimeRecordRepository = new Mock<ITimeRecordRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockDateTime = new Mock<IDateTime>();
            _mockLogger = new Mock<ILogger<TimeRecordService>>();

            // Configure mocks
            _mockDateTime.Setup(dt => dt.UtcNow()).Returns(_fixedDateTime);
            _mockCurrentUserService.Setup(ucs => ucs.GetUserId()).Returns(_testUserId);

            // Create service instance
            _timeRecordService = new TimeRecordService(
                _mockTimeRecordRepository.Object,
                _mockCurrentUserService.Object,
                _mockDateTime.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateTimeRecord_WithValidRequest_ReturnsSuccessResult()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = _fixedDateTime,
                Location = new LocationModel { Latitude = 40.7128, Longitude = -74.0060 }
            };

            _mockTimeRecordRepository.Setup(r => r.GetCurrentStatusAsync(_testUserId))
                .ReturnsAsync("out");

            _mockTimeRecordRepository.Setup(r => r.AddAsync(It.IsAny<TimeRecord>()))
                .ReturnsAsync(new TimeRecord 
                { 
                    Id = 1, 
                    UserId = _testUserId, 
                    Type = request.Type, 
                    Timestamp = request.Timestamp,
                    Latitude = request.Location.Latitude,
                    Longitude = request.Location.Longitude
                });

            // Act
            var result = await _timeRecordService.CreateTimeRecordAsync(request, _testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be("1");
            result.Data.Status.Should().Be("success");
            
            _mockTimeRecordRepository.Verify(r => r.GetCurrentStatusAsync(_testUserId), Times.Once);
            _mockTimeRecordRepository.Verify(r => r.AddAsync(It.Is<TimeRecord>(tr =>
                tr.UserId == _testUserId &&
                tr.Type == request.Type.ToLower() &&
                tr.Timestamp == request.Timestamp &&
                tr.Latitude == request.Location.Latitude &&
                tr.Longitude == request.Location.Longitude &&
                !tr.IsSynced)), Times.Once);
        }

        [Fact]
        public async Task CreateTimeRecord_WithNullRequest_ReturnsFailureResult()
        {
            // Arrange
            TimeRecordRequest request = null;

            // Act
            var result = await _timeRecordService.CreateTimeRecordAsync(request, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("cannot be null");
            
            _mockTimeRecordRepository.Verify(r => r.GetCurrentStatusAsync(It.IsAny<string>()), Times.Never);
            _mockTimeRecordRepository.Verify(r => r.AddAsync(It.IsAny<TimeRecord>()), Times.Never);
        }

        [Fact]
        public async Task CreateTimeRecord_WithNullUserId_ReturnsFailureResult()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = _fixedDateTime,
                Location = new LocationModel { Latitude = 40.7128, Longitude = -74.0060 }
            };

            // Act
            var result = await _timeRecordService.CreateTimeRecordAsync(request, null);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("User ID cannot be null or empty");
            
            _mockTimeRecordRepository.Verify(r => r.GetCurrentStatusAsync(It.IsAny<string>()), Times.Never);
            _mockTimeRecordRepository.Verify(r => r.AddAsync(It.IsAny<TimeRecord>()), Times.Never);
        }

        [Fact]
        public async Task CreateTimeRecord_WithInvalidType_ReturnsFailureResult()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "invalid",
                Timestamp = _fixedDateTime,
                Location = new LocationModel { Latitude = 40.7128, Longitude = -74.0060 }
            };

            // Act
            var result = await _timeRecordService.CreateTimeRecordAsync(request, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("must be 'in' or 'out'");
            
            _mockTimeRecordRepository.Verify(r => r.GetCurrentStatusAsync(It.IsAny<string>()), Times.Never);
            _mockTimeRecordRepository.Verify(r => r.AddAsync(It.IsAny<TimeRecord>()), Times.Never);
        }

        [Fact]
        public async Task CreateTimeRecord_WhenAlreadyClockedIn_ReturnsFailureResult()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = _fixedDateTime,
                Location = new LocationModel { Latitude = 40.7128, Longitude = -74.0060 }
            };

            _mockTimeRecordRepository.Setup(r => r.GetCurrentStatusAsync(_testUserId))
                .ReturnsAsync("in");

            // Act
            var result = await _timeRecordService.CreateTimeRecordAsync(request, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("already clocked in");
            
            _mockTimeRecordRepository.Verify(r => r.GetCurrentStatusAsync(_testUserId), Times.Once);
            _mockTimeRecordRepository.Verify(r => r.AddAsync(It.IsAny<TimeRecord>()), Times.Never);
        }

        [Fact]
        public async Task CreateTimeRecord_WhenAlreadyClockedOut_ReturnsFailureResult()
        {
            // Arrange
            var request = new TimeRecordRequest
            {
                Type = "out",
                Timestamp = _fixedDateTime,
                Location = new LocationModel { Latitude = 40.7128, Longitude = -74.0060 }
            };

            _mockTimeRecordRepository.Setup(r => r.GetCurrentStatusAsync(_testUserId))
                .ReturnsAsync("out");

            // Act
            var result = await _timeRecordService.CreateTimeRecordAsync(request, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("already clocked out");
            
            _mockTimeRecordRepository.Verify(r => r.GetCurrentStatusAsync(_testUserId), Times.Once);
            _mockTimeRecordRepository.Verify(r => r.AddAsync(It.IsAny<TimeRecord>()), Times.Never);
        }

        [Fact]
        public async Task GetTimeRecordById_WithValidId_ReturnsSuccessResult()
        {
            // Arrange
            int testId = 1;
            var testTimeRecord = new TimeRecord
            {
                Id = testId,
                UserId = _testUserId,
                Type = "in",
                Timestamp = _fixedDateTime,
                Latitude = 40.7128,
                Longitude = -74.0060
            };

            _mockTimeRecordRepository.Setup(r => r.GetByIdAsync(testId))
                .ReturnsAsync(testTimeRecord);

            // Act
            var result = await _timeRecordService.GetTimeRecordByIdAsync(testId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testTimeRecord);
            
            _mockTimeRecordRepository.Verify(r => r.GetByIdAsync(testId), Times.Once);
        }

        [Fact]
        public async Task GetTimeRecordById_WithInvalidId_ReturnsFailureResult()
        {
            // Arrange
            int invalidId = 999;
            _mockTimeRecordRepository.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((TimeRecord)null);

            // Act
            var result = await _timeRecordService.GetTimeRecordByIdAsync(invalidId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
            
            _mockTimeRecordRepository.Verify(r => r.GetByIdAsync(invalidId), Times.Once);
        }

        [Fact]
        public async Task GetTimeRecordHistory_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange
            int pageNumber = 1;
            int pageSize = 10;
            var testRecords = new List<TimeRecord>
            {
                new TimeRecord { Id = 1, UserId = _testUserId, Type = "in", Timestamp = _fixedDateTime, Latitude = 40.7128, Longitude = -74.0060 },
                new TimeRecord { Id = 2, UserId = _testUserId, Type = "out", Timestamp = _fixedDateTime.AddHours(8), Latitude = 40.7128, Longitude = -74.0060 }
            };
            var paginatedList = new PaginatedList<TimeRecord>(testRecords, 2, pageNumber, pageSize);

            _mockTimeRecordRepository.Setup(r => r.GetPaginatedByUserIdAsync(_testUserId, pageNumber, pageSize))
                .ReturnsAsync(paginatedList);

            // Act
            var result = await _timeRecordService.GetTimeRecordHistoryAsync(_testUserId, pageNumber, pageSize);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(paginatedList);
            
            _mockTimeRecordRepository.Verify(r => r.GetPaginatedByUserIdAsync(_testUserId, pageNumber, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetTimeRecordHistory_WithInvalidUserId_ReturnsFailureResult()
        {
            // Arrange
            string invalidUserId = null;
            int pageNumber = 1;
            int pageSize = 10;

            // Act
            var result = await _timeRecordService.GetTimeRecordHistoryAsync(invalidUserId, pageNumber, pageSize);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("User ID cannot be null or empty");
            
            _mockTimeRecordRepository.Verify(r => r.GetPaginatedByUserIdAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetTimeRecordHistory_WithInvalidPagination_ReturnsFailureResult()
        {
            // Arrange
            int invalidPageNumber = 0;
            int pageSize = 10;

            // Act
            var result = await _timeRecordService.GetTimeRecordHistoryAsync(_testUserId, invalidPageNumber, pageSize);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Page number must be greater than 0");
            
            _mockTimeRecordRepository.Verify(r => r.GetPaginatedByUserIdAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetTimeRecordsByDateRange_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange
            var startDate = _fixedDateTime.Date;
            var endDate = startDate.AddDays(1);
            var testRecords = new List<TimeRecord>
            {
                new TimeRecord { Id = 1, UserId = _testUserId, Type = "in", Timestamp = startDate.AddHours(9), Latitude = 40.7128, Longitude = -74.0060 },
                new TimeRecord { Id = 2, UserId = _testUserId, Type = "out", Timestamp = startDate.AddHours(17), Latitude = 40.7128, Longitude = -74.0060 }
            };

            _mockTimeRecordRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(_testUserId, startDate, endDate))
                .ReturnsAsync(testRecords);

            // Act
            var result = await _timeRecordService.GetTimeRecordsByDateRangeAsync(_testUserId, startDate, endDate);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testRecords);
            
            _mockTimeRecordRepository.Verify(r => r.GetByUserIdAndDateRangeAsync(_testUserId, startDate, endDate), Times.Once);
        }

        [Fact]
        public async Task GetTimeRecordsByDateRange_WithInvalidDateRange_ReturnsFailureResult()
        {
            // Arrange
            var endDate = _fixedDateTime.Date;
            var startDate = endDate.AddDays(1); // Invalid: start date after end date

            // Act
            var result = await _timeRecordService.GetTimeRecordsByDateRangeAsync(_testUserId, startDate, endDate);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("Start date must be before or equal to end date");
            
            _mockTimeRecordRepository.Verify(r => r.GetByUserIdAndDateRangeAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentStatus_WithValidUserId_ReturnsSuccessResult()
        {
            // Arrange
            string expectedStatus = "in";
            _mockTimeRecordRepository.Setup(r => r.GetCurrentStatusAsync(_testUserId))
                .ReturnsAsync(expectedStatus);

            // Act
            var result = await _timeRecordService.GetCurrentStatusAsync(_testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().Be(expectedStatus);
            
            _mockTimeRecordRepository.Verify(r => r.GetCurrentStatusAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentStatus_WithInvalidUserId_ReturnsFailureResult()
        {
            // Arrange
            string invalidUserId = null;

            // Act
            var result = await _timeRecordService.GetCurrentStatusAsync(invalidUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("User ID cannot be null or empty");
            
            _mockTimeRecordRepository.Verify(r => r.GetCurrentStatusAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetLatestTimeRecord_WithValidUserId_ReturnsSuccessResult()
        {
            // Arrange
            var testTimeRecord = new TimeRecord
            {
                Id = 1,
                UserId = _testUserId,
                Type = "in",
                Timestamp = _fixedDateTime,
                Latitude = 40.7128,
                Longitude = -74.0060
            };

            _mockTimeRecordRepository.Setup(r => r.GetLatestByUserIdAsync(_testUserId))
                .ReturnsAsync(testTimeRecord);

            // Act
            var result = await _timeRecordService.GetLatestTimeRecordAsync(_testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(testTimeRecord);
            
            _mockTimeRecordRepository.Verify(r => r.GetLatestByUserIdAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetLatestTimeRecord_WithInvalidUserId_ReturnsFailureResult()
        {
            // Arrange
            string invalidUserId = null;

            // Act
            var result = await _timeRecordService.GetLatestTimeRecordAsync(invalidUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("User ID cannot be null or empty");
            
            _mockTimeRecordRepository.Verify(r => r.GetLatestByUserIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTimeRecord_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange
            int testId = 1;
            var testTimeRecord = new TimeRecord
            {
                Id = testId,
                UserId = _testUserId,
                Type = "in",
                Timestamp = _fixedDateTime
            };

            _mockTimeRecordRepository.Setup(r => r.GetByIdAsync(testId))
                .ReturnsAsync(testTimeRecord);

            _mockTimeRecordRepository.Setup(r => r.DeleteAsync(testId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _timeRecordService.DeleteTimeRecordAsync(testId, _testUserId);

            // Assert
            result.Succeeded.Should().BeTrue();
            
            _mockTimeRecordRepository.Verify(r => r.GetByIdAsync(testId), Times.Once);
            _mockTimeRecordRepository.Verify(r => r.DeleteAsync(testId), Times.Once);
        }

        [Fact]
        public async Task DeleteTimeRecord_WithNonexistentRecord_ReturnsFailureResult()
        {
            // Arrange
            int nonExistentId = 999;

            _mockTimeRecordRepository.Setup(r => r.GetByIdAsync(nonExistentId))
                .ReturnsAsync((TimeRecord)null);

            // Act
            var result = await _timeRecordService.DeleteTimeRecordAsync(nonExistentId, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
            
            _mockTimeRecordRepository.Verify(r => r.GetByIdAsync(nonExistentId), Times.Once);
            _mockTimeRecordRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTimeRecord_WithUnauthorizedUser_ReturnsFailureResult()
        {
            // Arrange
            int testId = 1;
            string differentUserId = "different-user";
            var testTimeRecord = new TimeRecord
            {
                Id = testId,
                UserId = differentUserId, // Different from _testUserId
                Type = "in",
                Timestamp = _fixedDateTime
            };

            _mockTimeRecordRepository.Setup(r => r.GetByIdAsync(testId))
                .ReturnsAsync(testTimeRecord);

            // Act
            var result = await _timeRecordService.DeleteTimeRecordAsync(testId, _testUserId);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not authorized");
            
            _mockTimeRecordRepository.Verify(r => r.GetByIdAsync(testId), Times.Once);
            _mockTimeRecordRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CleanupOldRecords_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange
            var cutoffDate = _fixedDateTime.AddDays(-30);
            bool onlySynced = true;
            int deletedCount = 15;

            _mockTimeRecordRepository.Setup(r => r.DeleteOlderThanAsync(cutoffDate, onlySynced))
                .ReturnsAsync(deletedCount);

            // Act
            var result = await _timeRecordService.CleanupOldRecordsAsync(cutoffDate, onlySynced);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().Be(deletedCount);
            
            _mockTimeRecordRepository.Verify(r => r.DeleteOlderThanAsync(cutoffDate, onlySynced), Times.Once);
        }

        [Fact]
        public async Task UpdateSyncStatus_WithValidParameters_ReturnsSuccessResult()
        {
            // Arrange
            int testId = 1;
            bool syncStatus = true;

            _mockTimeRecordRepository.Setup(r => r.UpdateSyncStatusAsync(testId, syncStatus))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _timeRecordService.UpdateSyncStatusAsync(testId, syncStatus);

            // Assert
            result.Succeeded.Should().BeTrue();
            
            _mockTimeRecordRepository.Verify(r => r.UpdateSyncStatusAsync(testId, syncStatus), Times.Once);
        }

        [Fact]
        public async Task GetUnsyncedRecords_ReturnsSuccessResult()
        {
            // Arrange
            var unsyncedRecords = new List<TimeRecord>
            {
                new TimeRecord { Id = 1, UserId = _testUserId, Type = "in", Timestamp = _fixedDateTime, IsSynced = false },
                new TimeRecord { Id = 2, UserId = _testUserId, Type = "out", Timestamp = _fixedDateTime.AddHours(8), IsSynced = false }
            };

            _mockTimeRecordRepository.Setup(r => r.GetUnsyncedAsync())
                .ReturnsAsync(unsyncedRecords);

            // Act
            var result = await _timeRecordService.GetUnsyncedRecordsAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(unsyncedRecords);
            
            _mockTimeRecordRepository.Verify(r => r.GetUnsyncedAsync(), Times.Once);
        }
    }
}