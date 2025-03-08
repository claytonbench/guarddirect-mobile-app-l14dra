using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Application.Services
{
    public class TimeRecordServiceTests : TestBase, IDisposable
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IDateTime> _mockDateTime;
        private readonly Mock<ILogger<TimeRecordService>> _mockLogger;
        private readonly TimeRecordService _timeRecordService;
        private readonly string _testUserId;

        public TimeRecordServiceTests()
        {
            // Initialize test user ID
            _testUserId = "user1";
            
            // Create and setup mock for ICurrentUserService
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(m => m.GetUserId()).Returns(_testUserId);
            _mockCurrentUserService.Setup(m => m.IsAuthenticated()).Returns(true);
            
            // Create and setup mock for IDateTime
            _mockDateTime = new Mock<IDateTime>();
            _mockDateTime.Setup(m => m.UtcNow()).Returns(DateTime.UtcNow);
            
            // Create mock logger
            _mockLogger = CreateMockLogger<TimeRecordService>();
            
            // Create the service with mocked dependencies
            _timeRecordService = new TimeRecordService(
                MockTimeRecordRepository,
                _mockCurrentUserService.Object,
                _mockDateTime.Object,
                _mockLogger.Object);
        }

        public void Dispose()
        {
            // Reset all mocks to their initial state
            ResetMocks();
        }

        [Fact]
        public async Task CreateTimeRecordAsync_WithValidRequest_ReturnsSuccess()
        {
            // Arrange: Create a valid TimeRecordRequest with type 'in'
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel { Latitude = 40.7128, Longitude = -74.0060 }
            };
            
            // Arrange: Setup repository to return "out" as current status
            MockTimeRecordRepository.Setup(r => r.GetCurrentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync("out");
                
            // Arrange: Setup repository to successfully add the time record
            var timeRecord = new TimeRecord
            {
                Id = 5,
                UserId = _testUserId,
                Type = "in",
                Timestamp = request.Timestamp,
                Latitude = request.Location.Latitude,
                Longitude = request.Location.Longitude,
                IsSynced = false
            };
            
            MockTimeRecordRepository.Setup(r => r.AddAsync(It.IsAny<TimeRecord>()))
                .ReturnsAsync(timeRecord);
            
            // Act: Call _timeRecordService.CreateTimeRecordAsync with the request
            var result = await _timeRecordService.CreateTimeRecordAsync(request, _testUserId);
            
            // Assert: Verify result is successful
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be("5");
            result.Data.Status.Should().Be("success");
            
            // Assert: Verify repository's AddAsync was called with correct parameters
            MockTimeRecordRepository.Verify(r => r.AddAsync(It.Is<TimeRecord>(
                tr => tr.UserId == _testUserId && 
                      tr.Type == "in" && 
                      tr.Latitude == request.Location.Latitude && 
                      tr.Longitude == request.Location.Longitude)), 
                Times.Once);
        }

        [Fact]
        public async Task CreateTimeRecordAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert: Call AssertExceptionAsync<ArgumentNullException> with a lambda that calls CreateTimeRecordAsync with null request
            var exception = await AssertExceptionAsync<ArgumentNullException>(
                () => _timeRecordService.CreateTimeRecordAsync(null, _testUserId));
            
            // Assert: Verify the exception message contains parameter name
            exception.ParamName.Should().Be("request");
        }

        [Fact]
        public async Task CreateTimeRecordAsync_WithInvalidType_ReturnsFailure()
        {
            // Arrange: Create a TimeRecordRequest with invalid type (not 'in' or 'out')
            var request = new TimeRecordRequest
            {
                Type = "invalid",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel { Latitude = 40.7128, Longitude = -74.0060 }
            };
            
            // Act: Call _timeRecordService.CreateTimeRecordAsync with the request
            var result = await _timeRecordService.CreateTimeRecordAsync(request, _testUserId);
            
            // Assert: Verify result is failure
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("type must be 'in' or 'out'");
            
            // Assert: Verify repository's AddAsync was not called
            MockTimeRecordRepository.Verify(r => r.AddAsync(It.IsAny<TimeRecord>()), Times.Never);
        }

        [Fact]
        public async Task CreateTimeRecordAsync_WhenAlreadyClockedIn_ReturnsFailure()
        {
            // Arrange: Create a TimeRecordRequest with type 'in'
            var request = new TimeRecordRequest
            {
                Type = "in",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel { Latitude = 40.7128, Longitude = -74.0060 }
            };
            
            // Arrange: Setup repository to return "in" as current status
            MockTimeRecordRepository.Setup(r => r.GetCurrentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync("in");
            
            // Act: Call _timeRecordService.CreateTimeRecordAsync with the request
            var result = await _timeRecordService.CreateTimeRecordAsync(request, _testUserId);
            
            // Assert: Verify result is failure
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("already clocked in");
            
            // Assert: Verify repository's AddAsync was not called
            MockTimeRecordRepository.Verify(r => r.AddAsync(It.IsAny<TimeRecord>()), Times.Never);
        }

        [Fact]
        public async Task CreateTimeRecordAsync_WhenAlreadyClockedOut_ReturnsFailure()
        {
            // Arrange: Create a TimeRecordRequest with type 'out'
            var request = new TimeRecordRequest
            {
                Type = "out",
                Timestamp = DateTime.UtcNow,
                Location = new LocationModel { Latitude = 40.7128, Longitude = -74.0060 }
            };
            
            // Arrange: Setup repository to return "out" as current status
            MockTimeRecordRepository.Setup(r => r.GetCurrentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync("out");
            
            // Act: Call _timeRecordService.CreateTimeRecordAsync with the request
            var result = await _timeRecordService.CreateTimeRecordAsync(request, _testUserId);
            
            // Assert: Verify result is failure
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("already clocked out");
            
            // Assert: Verify repository's AddAsync was not called
            MockTimeRecordRepository.Verify(r => r.AddAsync(It.IsAny<TimeRecord>()), Times.Never);
        }

        [Fact]
        public async Task GetTimeRecordByIdAsync_WithValidId_ReturnsTimeRecord()
        {
            // Arrange: Get a test time record from TestData
            var timeRecord = TestData.GetTestTimeRecordById(1);
            
            // Arrange: Setup repository to return the test time record for its ID
            MockTimeRecordRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(timeRecord);
            
            // Act: Call _timeRecordService.GetTimeRecordByIdAsync with the ID
            var result = await _timeRecordService.GetTimeRecordByIdAsync(1);
            
            // Assert: Verify result is successful
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(timeRecord.Id);
            
            // Assert: Verify repository's GetByIdAsync was called with correct ID
            MockTimeRecordRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetTimeRecordByIdAsync_WithInvalidId_ReturnsFailure()
        {
            // Arrange: Setup repository to return null for any ID
            MockTimeRecordRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((TimeRecord)null);
            
            // Act: Call _timeRecordService.GetTimeRecordByIdAsync with an invalid ID
            var result = await _timeRecordService.GetTimeRecordByIdAsync(999);
            
            // Assert: Verify result is failure
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
            
            // Assert: Verify repository's GetByIdAsync was called with the invalid ID
            MockTimeRecordRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
        }

        [Fact]
        public async Task GetTimeRecordHistoryAsync_ReturnsPagedTimeRecords()
        {
            // Arrange: Setup repository to return test time records for the test user
            var timeRecords = TestData.GetTestTimeRecords()
                .Where(tr => tr.UserId == _testUserId)
                .ToList();
            
            var paginatedList = PaginatedList<TimeRecord>.Create(timeRecords, 1, 10);
            
            MockTimeRecordRepository.Setup(r => r.GetPaginatedByUserIdAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(paginatedList);
            
            // Act: Call _timeRecordService.GetTimeRecordHistoryAsync with user ID and pagination parameters
            var result = await _timeRecordService.GetTimeRecordHistoryAsync(_testUserId, 1, 10);
            
            // Assert: Verify result is successful
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(timeRecords.Count);
            
            // Assert: Verify repository's GetPaginatedByUserIdAsync was called with correct parameters
            MockTimeRecordRepository.Verify(r => r.GetPaginatedByUserIdAsync(_testUserId, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetTimeRecordsByDateRangeAsync_ReturnsTimeRecordsInRange()
        {
            // Arrange: Define start and end dates for the range
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            
            // Arrange: Setup repository to return test time records within the date range
            var timeRecords = TestData.GetTestTimeRecords()
                .Where(tr => tr.UserId == _testUserId && 
                            tr.Timestamp >= startDate && 
                            tr.Timestamp <= endDate)
                .ToList();
            
            MockTimeRecordRepository.Setup(r => r.GetByUserIdAndDateRangeAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(timeRecords);
            
            // Act: Call _timeRecordService.GetTimeRecordsByDateRangeAsync with user ID and date range
            var result = await _timeRecordService.GetTimeRecordsByDateRangeAsync(_testUserId, startDate, endDate);
            
            // Assert: Verify result is successful
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(timeRecords.Count);
            
            // Assert: Verify repository's GetByUserIdAndDateRangeAsync was called with correct parameters
            MockTimeRecordRepository.Verify(r => r.GetByUserIdAndDateRangeAsync(_testUserId, startDate, endDate), Times.Once);
        }

        [Fact]
        public async Task GetCurrentStatusAsync_ReturnsCurrentStatus()
        {
            // Arrange: Setup repository to return a specific status ('in' or 'out')
            MockTimeRecordRepository.Setup(r => r.GetCurrentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync("in");
            
            // Act: Call _timeRecordService.GetCurrentStatusAsync with user ID
            var result = await _timeRecordService.GetCurrentStatusAsync(_testUserId);
            
            // Assert: Verify result is successful
            result.Succeeded.Should().BeTrue();
            result.Data.Should().Be("in");
            
            // Assert: Verify repository's GetCurrentStatusAsync was called with correct user ID
            MockTimeRecordRepository.Verify(r => r.GetCurrentStatusAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetLatestTimeRecordAsync_ReturnsLatestRecord()
        {
            // Arrange: Get a test time record from TestData
            var timeRecord = TestData.GetTestTimeRecordById(1);
            
            // Arrange: Setup repository to return the test time record as the latest
            MockTimeRecordRepository.Setup(r => r.GetLatestByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync(timeRecord);
            
            // Act: Call _timeRecordService.GetLatestTimeRecordAsync with user ID
            var result = await _timeRecordService.GetLatestTimeRecordAsync(_testUserId);
            
            // Assert: Verify result is successful
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(timeRecord.Id);
            
            // Assert: Verify repository's GetLatestByUserIdAsync was called with correct user ID
            MockTimeRecordRepository.Verify(r => r.GetLatestByUserIdAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task DeleteTimeRecordAsync_WithValidIdAndOwner_ReturnsSuccess()
        {
            // Arrange: Get a test time record from TestData with matching user ID
            var timeRecord = TestData.GetTestTimeRecordById(1);
            timeRecord.UserId = _testUserId; // Ensure user is the owner
            
            // Arrange: Setup repository to return the test time record for its ID
            MockTimeRecordRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(timeRecord);
            
            // Arrange: Setup repository to successfully delete the record
            MockTimeRecordRepository.Setup(r => r.DeleteAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            
            // Act: Call _timeRecordService.DeleteTimeRecordAsync with the record ID and user ID
            var result = await _timeRecordService.DeleteTimeRecordAsync(1, _testUserId);
            
            // Assert: Verify result is successful
            result.Succeeded.Should().BeTrue();
            
            // Assert: Verify repository's DeleteAsync was called with correct ID
            MockTimeRecordRepository.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteTimeRecordAsync_WithInvalidId_ReturnsFailure()
        {
            // Arrange: Setup repository to return null for any ID
            MockTimeRecordRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((TimeRecord)null);
            
            // Act: Call _timeRecordService.DeleteTimeRecordAsync with an invalid ID and user ID
            var result = await _timeRecordService.DeleteTimeRecordAsync(999, _testUserId);
            
            // Assert: Verify result is failure
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not found");
            
            // Assert: Verify repository's DeleteAsync was not called
            MockTimeRecordRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTimeRecordAsync_WithDifferentOwner_ReturnsFailure()
        {
            // Arrange: Get a test time record from TestData with different user ID
            var timeRecord = TestData.GetTestTimeRecordById(1);
            timeRecord.UserId = "differentUser"; // Different user is the owner
            
            // Arrange: Setup repository to return the test time record for its ID
            MockTimeRecordRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(timeRecord);
            
            // Act: Call _timeRecordService.DeleteTimeRecordAsync with the record ID and current user ID
            var result = await _timeRecordService.DeleteTimeRecordAsync(1, _testUserId);
            
            // Assert: Verify result is failure
            result.Succeeded.Should().BeFalse();
            result.Message.Should().Contain("not authorized");
            
            // Assert: Verify repository's DeleteAsync was not called
            MockTimeRecordRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CleanupOldRecordsAsync_DeletesOldRecords_ReturnsCount()
        {
            // Arrange: Define a cutoff date for old records
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            
            // Arrange: Setup repository to return a specific count of deleted records
            MockTimeRecordRepository.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<bool>()))
                .ReturnsAsync(5); // 5 records deleted
            
            // Act: Call _timeRecordService.CleanupOldRecordsAsync with the cutoff date
            var result = await _timeRecordService.CleanupOldRecordsAsync(cutoffDate, true);
            
            // Assert: Verify result is successful
            result.Succeeded.Should().BeTrue();
            result.Data.Should().Be(5);
            
            // Assert: Verify repository's DeleteOlderThanAsync was called with correct parameters
            MockTimeRecordRepository.Verify(r => r.DeleteOlderThanAsync(cutoffDate, true), Times.Once);
        }

        [Fact]
        public async Task UpdateSyncStatusAsync_WithValidId_ReturnsSuccess()
        {
            // Arrange: Setup repository to successfully update sync status
            MockTimeRecordRepository.Setup(r => r.UpdateSyncStatusAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);
            
            // Act: Call _timeRecordService.UpdateSyncStatusAsync with a valid ID and sync status
            var result = await _timeRecordService.UpdateSyncStatusAsync(1, true);
            
            // Assert: Verify result is successful
            result.Succeeded.Should().BeTrue();
            
            // Assert: Verify repository's UpdateSyncStatusAsync was called with correct parameters
            MockTimeRecordRepository.Verify(r => r.UpdateSyncStatusAsync(1, true), Times.Once);
        }

        [Fact]
        public async Task GetUnsyncedRecordsAsync_ReturnsUnsyncedRecords()
        {
            // Arrange: Setup repository to return test unsynced time records
            var unsyncedRecords = TestData.GetTestTimeRecords()
                .Where(tr => !tr.IsSynced)
                .ToList();
            
            MockTimeRecordRepository.Setup(r => r.GetUnsyncedAsync())
                .ReturnsAsync(unsyncedRecords);
            
            // Act: Call _timeRecordService.GetUnsyncedRecordsAsync
            var result = await _timeRecordService.GetUnsyncedRecordsAsync();
            
            // Assert: Verify result is successful
            result.Succeeded.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(unsyncedRecords.Count);
            
            // Assert: Verify repository's GetUnsyncedAsync was called
            MockTimeRecordRepository.Verify(r => r.GetUnsyncedAsync(), Times.Once);
        }
    }
}