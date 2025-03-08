using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SecurityPatrol.Application.Services;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Application.Services
{
    /// <summary>
    /// Contains unit tests for the LocationService class to verify its functionality for processing location data,
    /// retrieving location history, and managing location data lifecycle.
    /// </summary>
    public class LocationServiceTests : TestBase
    {
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IDateTime> _mockDateTime;
        private readonly LocationService _locationService;
        private readonly string _testUserId;
        private readonly DateTime _testDateTime;

        /// <summary>
        /// Initializes a new instance of the LocationServiceTests class with test dependencies.
        /// </summary>
        public LocationServiceTests()
        {
            // Set up test data
            _testUserId = "user1";
            _testDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            // Set up mocks
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(m => m.GetUserId()).Returns(_testUserId);
            _mockCurrentUserService.Setup(m => m.IsAuthenticated()).Returns(true);

            _mockDateTime = new Mock<IDateTime>();
            _mockDateTime.Setup(m => m.UtcNow()).Returns(_testDateTime);

            // Create service instance with mocks
            _locationService = new LocationService(
                MockLocationRecordRepository.Object,
                _mockCurrentUserService.Object,
                _mockDateTime.Object);
        }

        [Fact]
        public async Task ProcessLocationBatchAsync_WithValidRequest_ShouldSucceed()
        {
            // Arrange
            var locations = new List<LocationModel>
            {
                new LocationModel
                {
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    Accuracy = 10.5,
                    Timestamp = _testDateTime.AddMinutes(-30)
                },
                new LocationModel
                {
                    Latitude = 40.7135,
                    Longitude = -74.0065,
                    Accuracy = 8.3,
                    Timestamp = _testDateTime.AddMinutes(-15)
                }
            };

            var request = new LocationBatchRequest
            {
                UserId = _testUserId,
                Locations = locations
            };

            MockLocationRecordRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<LocationRecord>>()))
                .ReturnsAsync(new List<int> { 5, 6 });

            // Act
            var result = await _locationService.ProcessLocationBatchAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.SyncedIds.Should().HaveCount(2);
            result.FailedIds.Should().BeEmpty();
            MockLocationRecordRepository.Verify(r => r.AddRangeAsync(It.Is<IEnumerable<LocationRecord>>(
                records => records.Count() == 2 &&
                          records.All(lr => lr.UserId == _testUserId && lr.IsSynced)
            )), Times.Once);
        }

        [Fact]
        public async Task ProcessLocationBatchAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = await AssertExceptionAsync<ArgumentNullException>(
                () => _locationService.ProcessLocationBatchAsync(null));
            
            exception.ParamName.Should().NotBeNull();
        }

        [Fact]
        public async Task ProcessLocationBatchAsync_WithEmptyLocations_ShouldThrowValidationException()
        {
            // Arrange
            var request = new LocationBatchRequest
            {
                UserId = _testUserId,
                Locations = new List<LocationModel>()
            };

            // Act & Assert
            var exception = await AssertExceptionAsync<FluentValidation.ValidationException>(
                () => _locationService.ProcessLocationBatchAsync(request));
            
            exception.Message.Should().Contain("Validation failed");
        }

        [Fact]
        public async Task GetLocationHistoryAsync_WithValidParameters_ShouldReturnLocations()
        {
            // Arrange
            var startTime = _testDateTime.AddDays(-1);
            var endTime = _testDateTime;
            var locationRecords = TestData.GetTestLocationRecords()
                .Where(lr => lr.UserId == _testUserId)
                .ToList();

            MockLocationRecordRepository.Setup(r => r.GetByUserIdAndTimeRangeAsync(
                _testUserId, startTime, endTime))
                .ReturnsAsync(locationRecords);

            // Act
            var result = await _locationService.GetLocationHistoryAsync(_testUserId, startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(locationRecords.Count);
            MockLocationRecordRepository.Verify(r => r.GetByUserIdAndTimeRangeAsync(
                _testUserId, startTime, endTime), Times.Once);
        }

        [Fact]
        public async Task GetLocationHistoryAsync_WithInvalidTimeRange_ShouldThrowArgumentException()
        {
            // Arrange
            var startTime = _testDateTime;
            var endTime = _testDateTime.AddDays(-1); // End time earlier than start time

            // Act & Assert
            var exception = await AssertExceptionAsync<ArgumentException>(
                () => _locationService.GetLocationHistoryAsync(_testUserId, startTime, endTime));
            
            exception.Message.Should().Contain("Start time must be earlier than end time");
        }

        [Fact]
        public async Task GetLatestLocationAsync_WithValidUserId_ShouldReturnLocation()
        {
            // Arrange
            var latestLocation = TestData.GetTestLocationRecords()
                .Where(lr => lr.UserId == _testUserId)
                .OrderByDescending(lr => lr.Timestamp)
                .FirstOrDefault();

            MockLocationRecordRepository.Setup(r => r.GetLatestLocationAsync(_testUserId))
                .ReturnsAsync(latestLocation);

            // Act
            var result = await _locationService.GetLatestLocationAsync(_testUserId);

            // Assert
            result.Should().NotBeNull();
            result.Latitude.Should().Be(latestLocation.Latitude);
            result.Longitude.Should().Be(latestLocation.Longitude);
            result.Accuracy.Should().Be(latestLocation.Accuracy);
            MockLocationRecordRepository.Verify(r => r.GetLatestLocationAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetLatestLocationAsync_WithNoLocationFound_ShouldReturnNull()
        {
            // Arrange
            MockLocationRecordRepository.Setup(r => r.GetLatestLocationAsync(_testUserId))
                .ReturnsAsync((LocationRecord)null);

            // Act
            var result = await _locationService.GetLatestLocationAsync(_testUserId);

            // Assert
            result.Should().BeNull();
            MockLocationRecordRepository.Verify(r => r.GetLatestLocationAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task GetLocationsByUserIdAsync_WithValidParameters_ShouldReturnLocations()
        {
            // Arrange
            int limit = 5;
            var locationRecords = TestData.GetTestLocationRecords()
                .Where(lr => lr.UserId == _testUserId)
                .Take(limit)
                .ToList();

            MockLocationRecordRepository.Setup(r => r.GetByUserIdAsync(_testUserId, limit))
                .ReturnsAsync(locationRecords);

            // Act
            var result = await _locationService.GetLocationsByUserIdAsync(_testUserId, limit);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(locationRecords.Count);
            MockLocationRecordRepository.Verify(r => r.GetByUserIdAsync(_testUserId, limit), Times.Once);
        }

        [Fact]
        public async Task CleanupLocationDataAsync_WithValidParameters_ShouldDeleteRecords()
        {
            // Arrange
            var cutoffDate = _testDateTime.AddDays(-30);
            bool onlySynced = true;
            int deletedCount = 10;

            MockLocationRecordRepository.Setup(r => r.DeleteOlderThanAsync(cutoffDate, onlySynced))
                .ReturnsAsync(deletedCount);

            // Act
            var result = await _locationService.CleanupLocationDataAsync(cutoffDate, onlySynced);

            // Assert
            result.Should().Be(deletedCount);
            MockLocationRecordRepository.Verify(r => r.DeleteOlderThanAsync(cutoffDate, onlySynced), Times.Once);
        }

        [Fact]
        public async Task SyncPendingLocationsAsync_WithUnsyncedRecords_ShouldSyncRecords()
        {
            // Arrange
            int batchSize = 50;
            var unsyncedRecords = TestData.GetTestLocationRecords()
                .Where(lr => !lr.IsSynced)
                .Take(batchSize)
                .ToList();

            MockLocationRecordRepository.Setup(r => r.GetUnsyncedRecordsAsync(batchSize))
                .ReturnsAsync(unsyncedRecords);

            MockLocationRecordRepository.Setup(r => r.UpdateSyncStatusAsync(It.IsAny<IEnumerable<int>>(), true))
                .ReturnsAsync(true);

            // Act
            var result = await _locationService.SyncPendingLocationsAsync(batchSize);

            // Assert
            result.Should().NotBeNull();
            result.SyncedIds.Should().HaveCount(unsyncedRecords.Count);
            result.FailedIds.Should().BeEmpty();
            MockLocationRecordRepository.Verify(r => r.GetUnsyncedRecordsAsync(batchSize), Times.Once);
            MockLocationRecordRepository.Verify(r => r.UpdateSyncStatusAsync(
                It.Is<IEnumerable<int>>(ids => ids.Count() == unsyncedRecords.Count), true), Times.Once);
        }

        [Fact]
        public async Task SyncPendingLocationsAsync_WithNoUnsyncedRecords_ShouldReturnEmptyResponse()
        {
            // Arrange
            int batchSize = 50;
            MockLocationRecordRepository.Setup(r => r.GetUnsyncedRecordsAsync(batchSize))
                .ReturnsAsync(new List<LocationRecord>());

            // Act
            var result = await _locationService.SyncPendingLocationsAsync(batchSize);

            // Assert
            result.Should().NotBeNull();
            result.SyncedIds.Should().BeEmpty();
            result.FailedIds.Should().BeEmpty();
            MockLocationRecordRepository.Verify(r => r.GetUnsyncedRecordsAsync(batchSize), Times.Once);
            MockLocationRecordRepository.Verify(r => r.UpdateSyncStatusAsync(It.IsAny<IEnumerable<int>>(), true), Times.Never);
        }
    }
}